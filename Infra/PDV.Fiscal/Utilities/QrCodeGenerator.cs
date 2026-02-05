using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace PDV.Fiscal.Utilities;

/// <summary>
/// Generates QR codes for NFC-e receipts.
/// </summary>
public static class QrCodeGenerator
{
    /// <summary>
    /// Generates the QR code URL for an NFC-e.
    /// </summary>
    /// <param name="accessKey">44-digit access key</param>
    /// <param name="issueDate">Issue date</param>
    /// <param name="totalValue">Total invoice value</param>
    /// <param name="icmsDigest">ICMS digest (optional)</param>
    /// <param name="cscId">CSC ID</param>
    /// <param name="cscToken">CSC Token</param>
    /// <param name="isProduction">True for production, false for homologation</param>
    /// <param name="state">State abbreviation (e.g., "SP")</param>
    public static string GenerateQrCodeUrl(
        string accessKey,
        DateTime issueDate,
        decimal totalValue,
        string? icmsDigest,
        string cscId,
        string cscToken,
        bool isProduction,
        string state)
    {
        // Get base URL by state and environment
        var baseUrl = GetBaseUrl(state, isProduction);

        // Build query parameters
        var queryBuilder = new StringBuilder();
        queryBuilder.Append($"chNFe={accessKey}");
        queryBuilder.Append($"&nVersao=100");
        queryBuilder.Append($"&tpAmb={(isProduction ? "1" : "2")}");

        // Add optional parameters if customer CPF is present
        if (!string.IsNullOrEmpty(icmsDigest))
        {
            queryBuilder.Append($"&dhEmi={Uri.EscapeDataString(issueDate.ToString("yyyy-MM-ddTHH:mm:sszzz"))}");
            queryBuilder.Append($"&vNF={TextSanitizer.FormatMoney(totalValue)}");
            queryBuilder.Append($"&vICMS={icmsDigest}");
            queryBuilder.Append($"&digVal={icmsDigest}");
        }

        queryBuilder.Append($"&cIdToken={cscId}");

        // Calculate hash
        var hashInput = $"{cscId}{cscToken}";
        var hash = CalculateSha1Hash(hashInput);
        queryBuilder.Append($"&cHashQRCode={hash}");

        return $"{baseUrl}?{queryBuilder}";
    }

    /// <summary>
    /// Generates a QR code image as base64 PNG.
    /// </summary>
    public static string GenerateQrCodeBase64(string content, int pixelsPerModule = 4)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        return Convert.ToBase64String(qrCodeBytes);
    }

    /// <summary>
    /// Generates a QR code image as byte array.
    /// </summary>
    public static byte[] GenerateQrCodeBytes(string content, int pixelsPerModule = 4)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Gets the SEFAZ QR code base URL for a state.
    /// </summary>
    private static string GetBaseUrl(string state, bool isProduction)
    {
        // Production URLs
        var productionUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AC"] = "http://www.sefaznet.ac.gov.br/nfce/qrcode",
            ["AL"] = "http://nfce.sefaz.al.gov.br/consultaNFCe.aspx",
            ["AM"] = "http://sistemas.sefaz.am.gov.br/nfceweb/consultarNFCe.do",
            ["AP"] = "https://www.sefaz.ap.gov.br/nfce/nfcep.php",
            ["BA"] = "http://nfe.sefaz.ba.gov.br/servicos/nfce/default.aspx",
            ["CE"] = "http://nfce.sefaz.ce.gov.br/pages/ShowNFCe.html",
            ["DF"] = "http://dec.fazenda.df.gov.br/ConsultarNFCe.aspx",
            ["ES"] = "http://app.sefaz.es.gov.br/ConsultaNFCe/",
            ["GO"] = "http://nfe.sefaz.go.gov.br/nfeweb/sites/nfce/danfeNFCe",
            ["MA"] = "http://www.nfce.sefaz.ma.gov.br/portal/consultarNFCe.do",
            ["MG"] = "https://portalsped.fazenda.mg.gov.br/portalnfce/sistema/qrcode.xhtml",
            ["MS"] = "http://www.dfe.ms.gov.br/nfce/qrcode",
            ["MT"] = "http://www.sefaz.mt.gov.br/nfce/consultanfce",
            ["PA"] = "https://appnfc.sefa.pa.gov.br/portal/view/consultas/nfce/nfceForm.seam",
            ["PB"] = "http://www.receita.pb.gov.br/nfce",
            ["PE"] = "http://nfce.sefaz.pe.gov.br/nfce-web/consultarNFCe",
            ["PI"] = "http://www.sefaz.pi.gov.br/nfce/qrcode",
            ["PR"] = "http://www.fazenda.pr.gov.br/nfce/qrcode",
            ["RJ"] = "http://www4.fazenda.rj.gov.br/consultaNFCe/QRCode",
            ["RN"] = "http://nfce.set.rn.gov.br/consultarNFCe.aspx",
            ["RO"] = "http://www.nfce.sefin.ro.gov.br/consultanfce/consulta.jsp",
            ["RR"] = "https://www.sefaz.rr.gov.br/nfce/servlet/qrcode",
            ["RS"] = "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx",
            ["SC"] = "https://sat.sef.sc.gov.br/nfce/consulta",
            ["SE"] = "http://www.nfce.se.gov.br/portal/consultarNFCe.jsp",
            ["SP"] = "https://www.nfce.fazenda.sp.gov.br/NFCeConsultaPublica/Paginas/ConsultaQRCode.aspx",
            ["TO"] = "http://www.sefaz.to.gov.br/nfce/qrcode"
        };

        // Homologation URLs (simplified - in real implementation each state has different URLs)
        var homologationUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SP"] = "https://www.homologacao.nfce.fazenda.sp.gov.br/NFCeConsultaPublica/Paginas/ConsultaQRCode.aspx"
        };

        if (isProduction)
        {
            return productionUrls.GetValueOrDefault(state, productionUrls["SP"]);
        }

        return homologationUrls.GetValueOrDefault(state, homologationUrls["SP"]);
    }

    /// <summary>
    /// Calculates SHA-1 hash for QR code signature.
    /// </summary>
    private static string CalculateSha1Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }
}
