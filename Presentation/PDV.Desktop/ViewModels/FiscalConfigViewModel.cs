using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;

namespace PDV.Desktop.ViewModels;

public partial class FiscalConfigViewModel : ViewModelBase
{
    private readonly IFiscalConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalManager _fiscalManager;

    private FiscalConfiguration? _currentConfig;

    [ObservableProperty]
    private string _statusMessage = "Carregando configuração...";

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _isLoaded;

    // Company Data
    [ObservableProperty]
    private string _taxId = string.Empty;

    [ObservableProperty]
    private string _legalName = string.Empty;

    [ObservableProperty]
    private string _tradeName = string.Empty;

    [ObservableProperty]
    private string _stateRegistration = string.Empty;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private string _cityCode = string.Empty;

    // Address
    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _addressNumber = string.Empty;

    [ObservableProperty]
    private string _neighborhood = string.Empty;

    [ObservableProperty]
    private string _zipCode = string.Empty;

    // NFC-e Parameters
    [ObservableProperty]
    private int _taxRegime = 1;

    [ObservableProperty]
    private int _series = 1;

    [ObservableProperty]
    private int _nextNumber = 1;

    [ObservableProperty]
    private string _cscToken = string.Empty;

    [ObservableProperty]
    private string _cscId = string.Empty;

    [ObservableProperty]
    private bool _isProduction;

    [ObservableProperty]
    private string _certificatePath = string.Empty;

    [ObservableProperty]
    private string _certificatePassword = string.Empty;

    // Tax Regime options for combo box
    public List<TaxRegimeOption> TaxRegimeOptions { get; } = new()
    {
        new TaxRegimeOption(1, "Simples Nacional"),
        new TaxRegimeOption(2, "Simples Nacional - Excesso"),
        new TaxRegimeOption(3, "Regime Normal")
    };

    // Brazilian states for combo box
    public List<string> StateOptions { get; } = new()
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO",
        "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    public FiscalConfigViewModel(
        IFiscalConfigurationRepository configRepository,
        IUnitOfWork unitOfWork,
        IFiscalManager fiscalManager)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
        _fiscalManager = fiscalManager;
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        if (IsLoaded) return;

        try
        {
            SetStatus("Carregando configuração...", false);

            _currentConfig = await _configRepository.GetActiveAsync();

            if (_currentConfig != null)
            {
                MapFromEntity(_currentConfig);
                SetStatus("Configuração carregada", false);
            }
            else
            {
                SetStatus("Nenhuma configuração encontrada. Preencha os dados para criar.", false);
            }

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao carregar: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task SaveConfigAsync()
    {
        if (!ValidateFields()) return;

        try
        {
            IsSaving = true;
            SetStatus("Salvando configuração...", false);

            if (_currentConfig == null)
            {
                // Create new
                _currentConfig = new FiscalConfiguration(
                    TaxId,
                    LegalName,
                    TradeName,
                    StateRegistration,
                    State,
                    CityCode,
                    Address,
                    AddressNumber,
                    ZipCode,
                    TaxRegime,
                    Series,
                    NextNumber);

                _currentConfig.SetCsc(CscToken, CscId);
                _currentConfig.SetEnvironment(IsProduction);

                if (!string.IsNullOrEmpty(CertificatePath) && !string.IsNullOrEmpty(CertificatePassword))
                {
                    _currentConfig.SetCertificate(CertificatePath, CertificatePassword);
                }

                await _configRepository.AddAsync(_currentConfig);
            }
            else
            {
                // Update existing
                _currentConfig.SetTaxId(TaxId);
                _currentConfig.SetTradeName(TradeName);
                _currentConfig.SetAddress(Address, AddressNumber, Neighborhood, ZipCode);
                _currentConfig.SetSeries(Series);
                _currentConfig.SetCsc(CscToken, CscId);
                _currentConfig.SetEnvironment(IsProduction);

                if (!string.IsNullOrEmpty(CertificatePath) && !string.IsNullOrEmpty(CertificatePassword))
                {
                    _currentConfig.SetCertificate(CertificatePath, CertificatePassword);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            SetStatus("Configuração salva com sucesso!", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao salvar: {ex.Message}", true);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsTesting = true;
            SetStatus("Testando conexão com SEFAZ...", false);

            var isAvailable = await _fiscalManager.IsSefazAvailableAsync();

            if (isAvailable)
            {
                SetStatus("Conexão com SEFAZ OK!", false);
            }
            else
            {
                SetStatus("SEFAZ indisponível no momento", true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Erro no teste: {ex.Message}", true);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool ValidateFields()
    {
        if (string.IsNullOrWhiteSpace(TaxId))
        {
            SetStatus("CNPJ é obrigatório", true);
            return false;
        }

        if (TaxId.Replace(".", "").Replace("/", "").Replace("-", "").Length != 14)
        {
            SetStatus("CNPJ deve ter 14 dígitos", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(LegalName))
        {
            SetStatus("Razão Social é obrigatória", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(TradeName))
        {
            SetStatus("Nome Fantasia é obrigatório", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(State) || State.Length != 2)
        {
            SetStatus("UF é obrigatória (2 caracteres)", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(CityCode) || CityCode.Length != 7)
        {
            SetStatus("Código IBGE deve ter 7 dígitos", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            SetStatus("Endereço é obrigatório", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(AddressNumber))
        {
            SetStatus("Número é obrigatório", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(ZipCode))
        {
            SetStatus("CEP é obrigatório", true);
            return false;
        }

        if (Series < 1)
        {
            SetStatus("Série deve ser maior que zero", true);
            return false;
        }

        return true;
    }

    private void MapFromEntity(FiscalConfiguration config)
    {
        TaxId = config.TaxId;
        LegalName = config.LegalName;
        TradeName = config.TradeName;
        StateRegistration = config.StateRegistration;
        State = config.State;
        CityCode = config.CityCode;
        Address = config.Address;
        AddressNumber = config.AddressNumber;
        Neighborhood = config.Neighborhood ?? string.Empty;
        ZipCode = config.ZipCode;
        TaxRegime = config.TaxRegime;
        Series = config.Series;
        NextNumber = config.NextNumber;
        CscToken = config.CscToken ?? string.Empty;
        CscId = config.CscId ?? string.Empty;
        IsProduction = config.IsProduction;
        CertificatePath = config.CertificatePath ?? string.Empty;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}

public record TaxRegimeOption(int Value, string Description);
