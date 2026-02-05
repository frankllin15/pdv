using PDV.Shared.DTOs;

namespace PDV.Fiscal.Providers;

/// <summary>
/// Interface for fiscal document transmission providers.
/// Implements Strategy pattern for different transmission methods.
/// </summary>
public interface IFiscalProvider
{
    /// <summary>
    /// Transmits an NFC-e XML to SEFAZ.
    /// </summary>
    /// <param name="xml">The signed NFC-e XML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the transmission</returns>
    Task<FiscalResult> TransmitAsync(string xml, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a previously authorized NFC-e.
    /// </summary>
    /// <param name="accessKey">44-digit access key</param>
    /// <param name="protocol">Authorization protocol number</param>
    /// <param name="justification">Cancellation justification (min 15 chars)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the cancellation</returns>
    Task<FiscalResult> CancelAsync(string accessKey, string protocol, string justification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the status of an NFC-e.
    /// </summary>
    /// <param name="accessKey">44-digit access key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current status of the NFC-e</returns>
    Task<FiscalResult> QueryAsync(string accessKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the SEFAZ service is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the provider name for logging purposes.
    /// </summary>
    string ProviderName { get; }
}
