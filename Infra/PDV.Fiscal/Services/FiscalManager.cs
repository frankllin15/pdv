using Microsoft.Extensions.Logging;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Fiscal.Exceptions;
using PDV.Fiscal.Providers;
using PDV.Fiscal.Utilities;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Fiscal.Services;

/// <summary>
/// Main fiscal manager service that orchestrates NFC-e issuance.
/// Handles normal transmission and offline contingency.
/// </summary>
public class FiscalManager : IFiscalManager
{
    private readonly IFiscalProvider _provider;
    private readonly IFiscalTransactionRepository _transactionRepository;
    private readonly IFiscalConfigurationRepository _configurationRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly XmlBuilderService _xmlBuilder;
    private readonly DanfeService _danfeService;
    private readonly ILogger<FiscalManager> _logger;

    public FiscalManager(
        IFiscalProvider provider,
        IFiscalTransactionRepository transactionRepository,
        IFiscalConfigurationRepository configurationRepository,
        ISaleRepository saleRepository,
        IUnitOfWork unitOfWork,
        XmlBuilderService xmlBuilder,
        DanfeService danfeService,
        ILogger<FiscalManager> logger)
    {
        _provider = provider;
        _transactionRepository = transactionRepository;
        _configurationRepository = configurationRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
        _xmlBuilder = xmlBuilder;
        _danfeService = danfeService;
        _logger = logger;
    }

    public async Task<FiscalResult> IssueNfceAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Issuing NFC-e for sale {SaleId}", sale.Id);

        // Get fiscal configuration
        var config = await _configurationRepository.GetActiveAsync(cancellationToken);
        if (config == null)
        {
            _logger.LogError("No active fiscal configuration found");
            return FiscalResult.Error("Configuração fiscal não encontrada");
        }

        // Validate sale products for fiscal
        if (!ValidateFiscalProducts(sale, out var errors))
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Fiscal validation failed for sale {SaleId}: {Errors}", sale.Id, errorMessage);
            return FiscalResult.Error($"Validação fiscal falhou: {errorMessage}");
        }

        // Get next number
        var number = config.GetNextNumberAndIncrement();
        var series = config.Series;

        // Generate access key
        var numericCode = AccessKeyGenerator.GenerateNumericCode();
        var stateCode = AccessKeyGenerator.GetStateCode(config.State);
        var isContingency = !await _provider.IsAvailableAsync(cancellationToken);

        var accessKey = AccessKeyGenerator.Generate(
            stateCode,
            sale.SaleDate,
            config.TaxId,
            65, // NFC-e model
            series,
            number,
            isContingency ? 9 : 1, // 1=Normal, 9=Contingency
            numericCode);

        _logger.LogDebug("Generated access key: {AccessKey}, Contingency: {IsContingency}", accessKey, isContingency);

        // Build XML
        var xml = _xmlBuilder.BuildNfceXml(sale, config, number, series, accessKey, isContingency);

        // Create transaction record
        var transaction = new FiscalTransaction(sale.Id, accessKey, number, series, isContingency);
        transaction.SetXmlRequest(xml);
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        // Update sale fiscal data
        sale.SetFiscalData(
            isContingency ? FiscalStatus.Contingency : FiscalStatus.Pending,
            accessKey,
            number,
            series);

        try
        {
            if (isContingency)
            {
                // Store for later transmission
                _logger.LogWarning("SEFAZ unavailable, storing NFC-e in contingency: {AccessKey}", accessKey);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return FiscalResult.Contingency(accessKey, number);
            }

            // Transmit to SEFAZ
            _logger.LogInformation("Transmitting NFC-e to SEFAZ: {AccessKey}", accessKey);
            var result = await _provider.TransmitAsync(xml, cancellationToken);

            if (result.Success)
            {
                transaction.SetAuthorized(result.Protocol!, result.StatusCode, result.StatusMessage, result.AuthorizedXml);
                sale.SetFiscalStatus(FiscalStatus.Authorized);
                _logger.LogInformation("NFC-e authorized: {AccessKey}, Protocol: {Protocol}", accessKey, result.Protocol);
            }
            else
            {
                transaction.SetRejected(result.StatusCode, result.StatusMessage);
                sale.SetFiscalStatus(FiscalStatus.Rejected);
                _logger.LogWarning("NFC-e rejected: {AccessKey}, Status: {StatusCode} - {StatusMessage}",
                    accessKey, result.StatusCode, result.StatusMessage);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transmitting NFC-e: {AccessKey}", accessKey);

            // Switch to contingency on error
            transaction.SetContingency();
            sale.SetFiscalStatus(FiscalStatus.Contingency);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return FiscalResult.Contingency(accessKey, number);
        }
    }

    public async Task<FiscalResult> CancelNfceAsync(Sale sale, string justification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling NFC-e for sale {SaleId}", sale.Id);

        if (string.IsNullOrWhiteSpace(justification) || justification.Length < 15)
        {
            return FiscalResult.Error("Justificativa deve ter no mínimo 15 caracteres");
        }

        var transaction = await _transactionRepository.GetBySaleIdAsync(sale.Id, cancellationToken);
        if (transaction == null)
        {
            return FiscalResult.Error("Transação fiscal não encontrada");
        }

        if (transaction.Status != FiscalStatus.Authorized)
        {
            return FiscalResult.Error($"NFC-e não pode ser cancelada. Status atual: {transaction.Status}");
        }

        try
        {
            var result = await _provider.CancelAsync(
                transaction.AccessKey,
                transaction.Protocol!,
                justification,
                cancellationToken);

            if (result.Success)
            {
                transaction.SetCancelled(result.Protocol!, justification);
                sale.SetFiscalStatus(FiscalStatus.Cancelled);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("NFC-e cancelled: {AccessKey}", transaction.AccessKey);
            }
            else
            {
                _logger.LogWarning("NFC-e cancellation rejected: {StatusCode} - {StatusMessage}",
                    result.StatusCode, result.StatusMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling NFC-e: {AccessKey}", transaction.AccessKey);
            return FiscalResult.Error($"Erro ao cancelar NFC-e: {ex.Message}");
        }
    }

    public async Task<FiscalResult> QueryNfceAsync(string accessKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying NFC-e: {AccessKey}", accessKey);

        if (!AccessKeyGenerator.Validate(accessKey))
        {
            return FiscalResult.Error("Chave de acesso inválida");
        }

        try
        {
            return await _provider.QueryAsync(accessKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying NFC-e: {AccessKey}", accessKey);
            return FiscalResult.Error($"Erro ao consultar NFC-e: {ex.Message}");
        }
    }

    public async Task<int> TransmitContingenciesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting contingency transmission");

        if (!await _provider.IsAvailableAsync(cancellationToken))
        {
            _logger.LogWarning("SEFAZ still unavailable, cannot transmit contingencies");
            return 0;
        }

        var pendingTransactions = await _transactionRepository.GetPendingContingencyAsync(cancellationToken);
        var transmittedCount = 0;

        foreach (var transaction in pendingTransactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                _logger.LogInformation("Transmitting contingency NFC-e: {AccessKey}", transaction.AccessKey);

                var result = await _provider.TransmitAsync(transaction.XmlRequest!, cancellationToken);

                if (result.Success)
                {
                    transaction.SetAuthorized(result.Protocol!, result.StatusCode, result.StatusMessage, result.AuthorizedXml);

                    var sale = await _saleRepository.GetByIdAsync(transaction.SaleId, cancellationToken);
                    if (sale != null)
                    {
                        sale.SetFiscalStatus(FiscalStatus.Authorized);
                    }

                    transmittedCount++;
                    _logger.LogInformation("Contingency NFC-e authorized: {AccessKey}", transaction.AccessKey);
                }
                else
                {
                    transaction.SetRejected(result.StatusCode, result.StatusMessage);
                    _logger.LogWarning("Contingency NFC-e rejected: {AccessKey}, {StatusMessage}",
                        transaction.AccessKey, result.StatusMessage);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transmitting contingency NFC-e: {AccessKey}", transaction.AccessKey);
            }
        }

        _logger.LogInformation("Contingency transmission completed. Transmitted: {Count}", transmittedCount);
        return transmittedCount;
    }

    public async Task<string> GenerateDanfeAsync(FiscalTransaction transaction, CancellationToken cancellationToken = default)
    {
        var config = await _configurationRepository.GetActiveAsync(cancellationToken);
        if (config == null)
        {
            throw FiscalException.ConfigurationError("Configuração fiscal não encontrada");
        }

        var sale = await _saleRepository.GetByIdWithItemsAndPaymentsAsync(transaction.SaleId, cancellationToken);
        if (sale == null)
        {
            throw FiscalException.ValidationError("Venda não encontrada");
        }

        return _danfeService.GenerateDanfeText(sale, config, transaction);
    }

    public bool ValidateFiscalProducts(Sale sale, out List<string> errors)
    {
        errors = new List<string>();

        if (!sale.Items.Any())
        {
            errors.Add("Venda sem itens");
            return false;
        }

        // For now, basic validation
        // In a full implementation, would check for required fiscal fields on products

        foreach (var item in sale.Items)
        {
            if (item.Quantity <= 0)
            {
                errors.Add($"Item {item.ProductDescription}: quantidade inválida");
            }

            if (item.UnitPrice <= 0)
            {
                errors.Add($"Item {item.ProductDescription}: preço unitário inválido");
            }
        }

        if (sale.Total <= 0)
        {
            errors.Add("Total da venda inválido");
        }

        return errors.Count == 0;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configurationRepository.GetActiveAsync(cancellationToken);
        return config != null;
    }

    public async Task<bool> IsSefazAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await _provider.IsAvailableAsync(cancellationToken);
    }
}
