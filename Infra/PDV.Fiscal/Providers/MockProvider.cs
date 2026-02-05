using Microsoft.Extensions.Logging;
using PDV.Fiscal.Utilities;
using PDV.Shared.DTOs;

namespace PDV.Fiscal.Providers;

/// <summary>
/// Mock provider for testing fiscal operations without connecting to SEFAZ.
/// Simulates authorization, cancellation, and query operations.
/// </summary>
public class MockProvider(ILogger<MockProvider> logger, MockProviderOptions? options = null)
    : IFiscalProvider
{
    private readonly MockProviderOptions _options = options ?? new MockProviderOptions();
    private readonly Dictionary<string, MockNfceState> _issuedNfces = new();

    public string ProviderName => "MockProvider";

    public async Task<FiscalResult> TransmitAsync(string xml, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[MockProvider] Transmitting NFC-e...");

        // Simulate network delay
        await Task.Delay(_options.SimulatedDelayMs, cancellationToken);

        // Simulate random failures if configured
        if (_options.SimulateRandomFailures && Random.Shared.NextDouble() < _options.FailureRate)
        {
            logger.LogWarning("[MockProvider] Simulated transmission failure");
            return FiscalResult.Rejected(539, "Rejeição: Duplicidade de NF-e, com diferença na Chave de Acesso [Simulado]");
        }

        // Simulate unavailability if configured
        if (_options.SimulateUnavailability)
        {
            logger.LogWarning("[MockProvider] Simulated service unavailability");
            return FiscalResult.Error("Serviço indisponível [Simulado]");
        }

        // Extract access key from XML (simplified)
        var accessKey = ExtractAccessKeyFromXml(xml);
        if (string.IsNullOrEmpty(accessKey))
        {
            accessKey = GenerateMockAccessKey();
        }

        // Generate mock protocol
        var protocol = GenerateMockProtocol();

        // Store the NFC-e state
        _issuedNfces[accessKey] = new MockNfceState
        {
            AccessKey = accessKey,
            Protocol = protocol,
            Status = "Autorizado",
            AuthorizationDate = DateTime.UtcNow
        };

        logger.LogInformation("[MockProvider] NFC-e authorized: {AccessKey}, Protocol: {Protocol}", accessKey, protocol);

        return FiscalResult.Authorized(accessKey, protocol, GenerateMockAuthorizedXml(accessKey, protocol));
    }

    public async Task<FiscalResult> CancelAsync(string accessKey, string protocol, string justification, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[MockProvider] Cancelling NFC-e: {AccessKey}", accessKey);

        // Simulate network delay
        await Task.Delay(_options.SimulatedDelayMs, cancellationToken);

        // Validate justification length
        if (string.IsNullOrWhiteSpace(justification) || justification.Length < 15)
        {
            return FiscalResult.Rejected(236, "Rejeição: Justificativa menor que 15 caracteres");
        }

        // Check if NFC-e exists
        if (!_issuedNfces.TryGetValue(accessKey, out var nfceState))
        {
            return FiscalResult.Rejected(218, "Rejeição: NFC-e não encontrada");
        }

        // Check if already cancelled
        if (nfceState.Status == "Cancelado")
        {
            return FiscalResult.Rejected(220, "Rejeição: NFC-e já cancelada");
        }

        // Generate cancellation protocol
        var cancellationProtocol = GenerateMockProtocol();

        // Update state
        nfceState.Status = "Cancelado";
        nfceState.CancellationProtocol = cancellationProtocol;
        nfceState.CancellationDate = DateTime.UtcNow;

        logger.LogInformation("[MockProvider] NFC-e cancelled: {AccessKey}, Protocol: {Protocol}", accessKey, cancellationProtocol);

        return FiscalResult.Authorized(accessKey, cancellationProtocol);
    }

    public async Task<FiscalResult> QueryAsync(string accessKey, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[MockProvider] Querying NFC-e: {AccessKey}", accessKey);

        // Simulate network delay
        await Task.Delay(_options.SimulatedDelayMs / 2, cancellationToken);

        if (!_issuedNfces.TryGetValue(accessKey, out var nfceState))
        {
            return FiscalResult.Rejected(217, "Rejeição: NFC-e não encontrada na base de dados");
        }

        var statusCode = nfceState.Status == "Autorizado" ? 100 : 101;
        return new FiscalResult(
            true,
            accessKey,
            nfceState.Protocol,
            statusCode,
            nfceState.Status,
            null,
            false);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return !_options.SimulateUnavailability;
    }

    private static string ExtractAccessKeyFromXml(string xml)
    {
        // Simple extraction - in real implementation would parse XML
        var startTag = "<chNFe>";
        var endTag = "</chNFe>";
        var startIndex = xml.IndexOf(startTag, StringComparison.Ordinal);
        if (startIndex < 0) return string.Empty;

        startIndex += startTag.Length;
        var endIndex = xml.IndexOf(endTag, startIndex, StringComparison.Ordinal);
        if (endIndex < 0) return string.Empty;

        return xml[startIndex..endIndex];
    }

    private static string GenerateMockAccessKey()
    {
        return AccessKeyGenerator.Generate(
            "35", // SP
            DateTime.Now,
            "00000000000000",
            65,
            1,
            Random.Shared.Next(1, 999999999),
            1,
            AccessKeyGenerator.GenerateNumericCode());
    }

    private static string GenerateMockProtocol()
    {
        return $"{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(100000, 999999):D6}";
    }

    private static string GenerateMockAuthorizedXml(string accessKey, string protocol)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<nfeProc xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
  <NFe>
    <infNFe Id=""NFe{accessKey}"" versao=""4.00"">
      <!-- NFC-e content -->
    </infNFe>
  </NFe>
  <protNFe versao=""4.00"">
    <infProt>
      <tpAmb>2</tpAmb>
      <verAplic>MOCK_1.0</verAplic>
      <chNFe>{accessKey}</chNFe>
      <dhRecbto>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:sszzz}</dhRecbto>
      <nProt>{protocol}</nProt>
      <digVal>MOCK_DIGEST</digVal>
      <cStat>100</cStat>
      <xMotivo>Autorizado o uso da NF-e</xMotivo>
    </infProt>
  </protNFe>
</nfeProc>";
    }

    private class MockNfceState
    {
        public string AccessKey { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AuthorizationDate { get; set; }
        public string? CancellationProtocol { get; set; }
        public DateTime? CancellationDate { get; set; }
    }
}

/// <summary>
/// Configuration options for MockProvider.
/// </summary>
public class MockProviderOptions
{
    /// <summary>
    /// Simulated network delay in milliseconds.
    /// </summary>
    public int SimulatedDelayMs { get; set; } = 500;

    /// <summary>
    /// Whether to simulate random transmission failures.
    /// </summary>
    public bool SimulateRandomFailures { get; set; } = false;

    /// <summary>
    /// Failure rate when SimulateRandomFailures is true (0.0 to 1.0).
    /// </summary>
    public double FailureRate { get; set; } = 0.1;

    /// <summary>
    /// Whether to simulate service unavailability.
    /// </summary>
    public bool SimulateUnavailability { get; set; } = false;
}
