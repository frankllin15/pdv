namespace PDV.Shared.DTOs;

/// <summary>
/// Result of DANFE generation containing text content, QR code data, and optionally PDF.
/// </summary>
public record DanfeResult(
    string TextContent,
    string? QrCodeUrl,
    string? QrCodeBase64,
    bool IsReprint,
    int ReprintNumber,
    byte[]? PdfContent = null
)
{
    /// <summary>
    /// Creates a successful DANFE result with QR code data.
    /// </summary>
    public static DanfeResult Success(string textContent, string qrCodeUrl, string qrCodeBase64, byte[]? pdfContent = null) =>
        new(textContent, qrCodeUrl, qrCodeBase64, false, 0, pdfContent);

    /// <summary>
    /// Creates a successful DANFE result without QR code (CSC not configured).
    /// </summary>
    public static DanfeResult SuccessWithoutQrCode(string textContent, byte[]? pdfContent = null) =>
        new(textContent, null, null, false, 0, pdfContent);

    /// <summary>
    /// Creates a reprint DANFE result with QR code data.
    /// </summary>
    public static DanfeResult Reprint(string textContent, string qrCodeUrl, string qrCodeBase64, int reprintNumber, byte[]? pdfContent = null) =>
        new(textContent, qrCodeUrl, qrCodeBase64, true, reprintNumber, pdfContent);

    /// <summary>
    /// Creates a reprint DANFE result without QR code.
    /// </summary>
    public static DanfeResult ReprintWithoutQrCode(string textContent, int reprintNumber, byte[]? pdfContent = null) =>
        new(textContent, null, null, true, reprintNumber, pdfContent);

    /// <summary>
    /// Whether the QR code is available for this DANFE.
    /// </summary>
    public bool HasQrCode => !string.IsNullOrEmpty(QrCodeUrl) && !string.IsNullOrEmpty(QrCodeBase64);

    /// <summary>
    /// Whether the PDF is available for this DANFE.
    /// </summary>
    public bool HasPdf => PdfContent != null && PdfContent.Length > 0;

    /// <summary>
    /// Gets the QR code as PNG bytes (decoded from base64).
    /// </summary>
    public byte[]? GetQrCodeBytes() =>
        string.IsNullOrEmpty(QrCodeBase64) ? null : Convert.FromBase64String(QrCodeBase64);
}
