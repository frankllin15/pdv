using PDV.Core.Entities;
using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Services;

public interface IFiscalManager
{
    /// <summary>
    /// Issues an NFC-e for the given sale.
    /// If SEFAZ is unavailable, the invoice is issued in contingency mode.
    /// </summary>
    Task<FiscalResult> IssueNfceAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a previously authorized NFC-e.
    /// </summary>
    Task<FiscalResult> CancelNfceAsync(Sale sale, string justification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the status of an NFC-e by its access key.
    /// </summary>
    Task<FiscalResult> QueryNfceAsync(string accessKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transmits all pending contingency invoices to SEFAZ.
    /// Returns the number of successfully transmitted invoices.
    /// </summary>
    Task<int> TransmitContingenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates DANFE (receipt) content for printing.
    /// </summary>
    Task<string> GenerateDanfeAsync(FiscalTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all products in the sale have required fiscal data.
    /// Returns true if valid, false otherwise with error details.
    /// </summary>
    bool ValidateFiscalProducts(Sale sale, out List<string> errors);

    /// <summary>
    /// Checks if fiscal module is configured and ready to issue invoices.
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if SEFAZ is currently available.
    /// </summary>
    Task<bool> IsSefazAvailableAsync(CancellationToken cancellationToken = default);
}
