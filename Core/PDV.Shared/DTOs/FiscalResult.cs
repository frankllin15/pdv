namespace PDV.Shared.DTOs;

public record FiscalResult(
    bool Success,
    string? AccessKey,
    string? Protocol,
    int StatusCode,
    string StatusMessage,
    string? AuthorizedXml,
    bool IsContingency
)
{
    public static FiscalResult Authorized(string accessKey, string protocol, string? xml = null) =>
        new(true, accessKey, protocol, 100, "Autorizado", xml, false);

    public static FiscalResult Contingency(string accessKey, int number) =>
        new(true, accessKey, null, 0, $"Contingência - NFC-e {number} gerada offline", null, true);

    public static FiscalResult Rejected(int statusCode, string message) =>
        new(false, null, null, statusCode, message, null, false);

    public static FiscalResult Error(string message) =>
        new(false, null, null, -1, message, null, false);
}
