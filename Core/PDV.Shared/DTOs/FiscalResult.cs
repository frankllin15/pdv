namespace PDV.Shared.DTOs;

public record FiscalResult(
    bool Success,
    string? AccessKey,
    string? Protocol,
    int StatusCode,
    string StatusMessage,
    string? AuthorizedXml,
    bool IsContingency,
    string? DanfeContent = null,
    int ReprintNumber = 0,
    string? QrCodeUrl = null,
    string? QrCodeBase64 = null,
    byte[]? PdfContent = null
)
{
    /// <summary>
    /// Whether QR code data is available.
    /// </summary>
    public bool HasQrCode => !string.IsNullOrEmpty(QrCodeUrl) && !string.IsNullOrEmpty(QrCodeBase64);

    /// <summary>
    /// Whether PDF content is available.
    /// </summary>
    public bool HasPdf => PdfContent != null && PdfContent.Length > 0;

    public static FiscalResult Authorized(string accessKey, string protocol, string? xml = null) =>
        new(true, accessKey, protocol, 100, "Autorizado", xml, false);

    public static FiscalResult Contingency(string accessKey, int number) =>
        new(true, accessKey, null, 0, $"Contingência - NFC-e {number} gerada offline", null, true);

    public static FiscalResult Rejected(int statusCode, string message) =>
        new(false, null, null, statusCode, message, null, false);

    public static FiscalResult Error(string message) =>
        new(false, null, null, -1, message, null, false);

    public static FiscalResult Reprint(string accessKey, string danfeContent, int reprintNumber, bool isContingency, string? qrCodeUrl = null, string? qrCodeBase64 = null, byte[]? pdfContent = null) =>
        new(true, accessKey, null, 100, $"Reimpressão #{reprintNumber} gerada com sucesso", null, isContingency, danfeContent, reprintNumber, qrCodeUrl, qrCodeBase64, pdfContent);
}
