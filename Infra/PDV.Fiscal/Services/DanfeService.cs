using System.Text;
using PDV.Core.Entities;
using PDV.Fiscal.Utilities;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Fiscal.Services;

/// <summary>
/// Service for generating DANFE (Documento Auxiliar da Nota Fiscal Eletrônica).
/// Generates text-based receipts for thermal printers.
/// </summary>
public class DanfeService
{
    private const int LineWidth = 48; // Standard 80mm thermal printer width
    private readonly DanfePdfService? _pdfService;

    public DanfeService()
    {
        _pdfService = new DanfePdfService();
    }

    /// <summary>
    /// Generates a complete DANFE with QR code for an NFC-e.
    /// </summary>
    /// <param name="sale">The sale associated with the fiscal transaction</param>
    /// <param name="config">Active fiscal configuration</param>
    /// <param name="transaction">The fiscal transaction</param>
    /// <param name="isReprint">Whether this is a reprint (2nd copy)</param>
    /// <param name="reprintDate">Date/time of the reprint</param>
    /// <param name="reprintNumber">Reprint number (for audit purposes)</param>
    /// <param name="includePdf">Whether to generate PDF (default: true)</param>
    /// <returns>DanfeResult containing text content, QR code data, and optionally PDF</returns>
    public DanfeResult GenerateDanfe(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction,
        bool isReprint = false,
        DateTime? reprintDate = null,
        int reprintNumber = 0,
        bool includePdf = true)
    {
        // Generate QR Code URL and image
        var qrCodeUrl = GenerateQrCodeUrl(transaction, config, sale.Total);
        string? qrCodeBase64 = null;
        byte[]? qrCodeBytes = null;

        if (!string.IsNullOrEmpty(qrCodeUrl))
        {
            qrCodeBytes = QrCodeGenerator.GenerateQrCodeBytes(qrCodeUrl, pixelsPerModule: 4);
            qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
        }

        // Generate text content
        var textContent = GenerateDanfeText(sale, config, transaction, qrCodeUrl, isReprint, reprintDate);

        // Generate PDF with QR Code
        byte[]? pdfContent = null;
        if (includePdf && _pdfService != null)
        {
            try
            {
                pdfContent = _pdfService.GeneratePdf(
                    sale, config, transaction, qrCodeBytes,
                    isReprint, reprintDate, reprintNumber);
            }
            catch
            {
                // PDF generation failed - continue without PDF
                pdfContent = null;
            }
        }

        if (isReprint)
        {
            return string.IsNullOrEmpty(qrCodeUrl)
                ? DanfeResult.ReprintWithoutQrCode(textContent, reprintNumber, pdfContent)
                : DanfeResult.Reprint(textContent, qrCodeUrl, qrCodeBase64!, reprintNumber, pdfContent);
        }

        return string.IsNullOrEmpty(qrCodeUrl)
            ? DanfeResult.SuccessWithoutQrCode(textContent, pdfContent)
            : DanfeResult.Success(textContent, qrCodeUrl, qrCodeBase64!, pdfContent);
    }

    /// <summary>
    /// Generates a text-based DANFE for an NFC-e.
    /// </summary>
    /// <param name="sale">The sale associated with the fiscal transaction</param>
    /// <param name="config">Active fiscal configuration</param>
    /// <param name="transaction">The fiscal transaction</param>
    /// <param name="isReprint">Whether this is a reprint (2nd copy)</param>
    /// <param name="reprintDate">Date/time of the reprint</param>
    [Obsolete("Use GenerateDanfe instead which returns DanfeResult with QR code data")]
    public string GenerateDanfeText(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction,
        bool isReprint = false,
        DateTime? reprintDate = null)
    {
        var qrCodeUrl = GenerateQrCodeUrl(transaction, config, sale.Total);
        return GenerateDanfeText(sale, config, transaction, qrCodeUrl, isReprint, reprintDate);
    }

    /// <summary>
    /// Generates a text-based DANFE for an NFC-e with QR code URL.
    /// </summary>
    private string GenerateDanfeText(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction,
        string? qrCodeUrl,
        bool isReprint = false,
        DateTime? reprintDate = null)
    {
        var sb = new StringBuilder();

        // Header
        AppendCentered(sb, config.TradeName.ToUpperInvariant());
        AppendCentered(sb, config.LegalName);
        AppendCentered(sb, $"CNPJ: {DocumentValidator.FormatCnpj(config.TaxId)}");
        AppendCentered(sb, config.Address);
        AppendCentered(sb, $"{config.Neighborhood} - CEP: {FormatZipCode(config.ZipCode)}");
        AppendLine(sb);

        // Reprint marker (2nd copy)
        if (isReprint)
        {
            AppendCentered(sb, "================================");
            AppendCentered(sb, "** REIMPRESSAO - 2a VIA **");
            AppendCentered(sb, $"Reimpresso em: {(reprintDate ?? DateTime.Now):dd/MM/yyyy HH:mm}");
            AppendCentered(sb, "================================");
            AppendLine(sb);
        }

        // Document info
        AppendCentered(sb, "DANFE NFC-e - Documento Auxiliar");
        AppendCentered(sb, "da Nota Fiscal Eletronica para Consumidor Final");
        AppendCentered(sb, $"NFC-e n. {transaction.Number} Serie {transaction.Series}");
        AppendCentered(sb, sale.SaleDate.ToString("dd/MM/yyyy HH:mm:ss"));
        AppendLine(sb);

        // Consumer
        if (!string.IsNullOrEmpty(sale.CustomerDocument))
        {
            var doc = TextSanitizer.OnlyNumbers(sale.CustomerDocument);
            if (doc.Length == 11)
                AppendLine(sb, $"CPF do Consumidor: {DocumentValidator.FormatCpf(doc)}");
            else if (doc.Length == 14)
                AppendLine(sb, $"CNPJ do Consumidor: {DocumentValidator.FormatCnpj(doc)}");
        }
        else
        {
            AppendLine(sb, "CONSUMIDOR NAO IDENTIFICADO");
        }
        AppendLine(sb);

        // Items header - 48 cols: COD(6) + SP + DESC(31) + SP + VALOR(9)
        AppendLine(sb, "CODIGO DESCRICAO                           VALOR");
        AppendSeparator(sb);

        // Items
        var itemNumber = 1;
        foreach (var item in sale.Items)
        {
            var code = item.ProductId.ToString()[..6].ToUpperInvariant();
            var desc = TruncateString(TextSanitizer.SanitizeForXml(item.ProductDescription), 31);
            var qty = item.Quantity.ToString("N3").Replace(",000", "");
            var unitPrice = item.UnitPrice.ToString("N2");
            var totalPrice = (item.Quantity * item.UnitPrice).ToString("N2");

            // Linha 1: código + descrição + valor total
            AppendLine(sb, $"{code} {desc,-31} {totalPrice,9}");
            // Linha 2: quantidade x unitário (recuado)
            AppendLine(sb, $"       {qty} x {unitPrice}");

            if (item.Discount > 0)
            {
                AppendLine(sb, $"       Desconto: -{item.Discount:N2}");
            }

            itemNumber++;
        }

        AppendSeparator(sb);

        // Totals - alinhado à direita em 48 cols
        AppendLineAligned(sb, "QTD. ITENS", sale.Items.Count.ToString());
        AppendLineAligned(sb, "SUBTOTAL", $"R$ {sale.Subtotal:N2}");

        if (sale.Discount > 0)
        {
            AppendLineAligned(sb, "DESCONTO", $"-R$ {sale.Discount:N2}");
        }

        AppendLineAligned(sb, "VALOR TOTAL", $"R$ {sale.Total:N2}");
        AppendLine(sb);

        // Payments
        AppendLine(sb, "FORMA PAGAMENTO                          VALOR");
        AppendSeparator(sb);
        foreach (var payment in sale.Payments)
        {
            var methodName = GetPaymentMethodName(payment.Method);
            AppendLine(sb, $"{methodName,-32} R$ {payment.Amount,10:N2}");
        }

        var change = sale.GetChange();
        if (change > 0)
        {
            AppendLine(sb, $"{"TROCO",-32} R$ {change,10:N2}");
        }
        AppendLine(sb);

        // Fiscal information
        AppendSeparator(sb);
        AppendCentered(sb, "INFORMACOES FISCAIS");
        AppendLine(sb);

        if (transaction.IsContingency)
        {
            AppendCentered(sb, "*** EMITIDA EM CONTINGENCIA ***");
            AppendCentered(sb, "Pendente de transmissao a SEFAZ");
            AppendLine(sb);
        }

        // Access key
        AppendCentered(sb, "Consulte pela Chave de Acesso em:");
        AppendCentered(sb, QrCodeGenerator.GetConsultationUrl(config.State, config.IsProduction));
        AppendLine(sb);
        AppendCentered(sb, "CHAVE DE ACESSO");
        AppendAccessKey(sb, transaction.AccessKey);
        AppendLine(sb);

        // Protocol
        if (!string.IsNullOrEmpty(transaction.Protocol))
        {
            AppendLine(sb, $"Protocolo de Autorizacao: {transaction.Protocol}");
            if (transaction.AuthorizationDate.HasValue)
            {
                AppendLine(sb, $"Data: {transaction.AuthorizationDate.Value:dd/MM/yyyy HH:mm:ss}");
            }
        }
        AppendLine(sb);

        // QR Code section
        AppendSeparator(sb);
        if (!string.IsNullOrEmpty(qrCodeUrl))
        {
            AppendCentered(sb, "QRCODE PARA CONSULTA VIA APP");
            AppendLine(sb);
            // QR Code marker - will be replaced by actual QR code image on print
            // The [QRCODE] tag can be parsed by the printer service
            AppendCentered(sb, "[QRCODE]");
            AppendLine(sb);
            // Also include the URL for text-only printers or debugging
            AppendCentered(sb, "URL de Consulta:");
            // Break URL into multiple lines if too long
            AppendQrCodeUrl(sb, qrCodeUrl);
        }
        else
        {
            AppendCentered(sb, "CSC NAO CONFIGURADO");
            AppendCentered(sb, "QR CODE NAO DISPONIVEL");
        }
        AppendLine(sb);

        // Footer
        AppendSeparator(sb);
        if (config.IsProduction)
        {
            AppendCentered(sb, "AMBIENTE DE PRODUCAO");
        }
        else
        {
            AppendCentered(sb, "** SEM VALOR FISCAL **");
            AppendCentered(sb, "** HOMOLOGACAO **");
        }
        AppendLine(sb);

        return sb.ToString();
    }

    /// <summary>
    /// Generates QR code content URL for the DANFE.
    /// </summary>
    public string GenerateQrCodeUrl(
        FiscalTransaction transaction,
        FiscalConfiguration config,
        decimal totalValue)
    {
        if (string.IsNullOrEmpty(config.CscId) || string.IsNullOrEmpty(config.CscToken))
        {
            return string.Empty;
        }

        return QrCodeGenerator.GenerateQrCodeUrl(
            transaction.AccessKey,
            transaction.CreatedAt,
            totalValue,
            null,
            config.CscId,
            config.CscToken,
            config.IsProduction,
            config.State);
    }

    private static void AppendLine(StringBuilder sb, string? text = null)
    {
        sb.AppendLine(text ?? string.Empty);
    }

    private static void AppendCentered(StringBuilder sb, string text)
    {
        text = TruncateString(text, LineWidth);
        var padding = (LineWidth - text.Length) / 2;
        sb.AppendLine(new string(' ', Math.Max(0, padding)) + text);
    }

    private static void AppendSeparator(StringBuilder sb)
    {
        sb.AppendLine(new string('-', LineWidth));
    }

    private static void AppendLineAligned(StringBuilder sb, string label, string value)
    {
        var spaces = LineWidth - label.Length - value.Length;
        sb.AppendLine(label + new string(' ', Math.Max(1, spaces)) + value);
    }

    private static void AppendAccessKey(StringBuilder sb, string accessKey)
    {
        // Format in groups of 4
        var formatted = AccessKeyGenerator.Format(accessKey);
        var parts = formatted.Split(' ');

        // First line: first 6 groups
        AppendCentered(sb, string.Join(" ", parts.Take(6)));
        // Second line: remaining groups
        AppendCentered(sb, string.Join(" ", parts.Skip(6)));
    }

    private static string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string FormatZipCode(string zipCode)
    {
        zipCode = TextSanitizer.OnlyNumbers(zipCode);
        if (zipCode.Length != 8)
            return zipCode;

        return $"{zipCode[..5]}-{zipCode.Substring(5, 3)}";
    }

    private static string GetPaymentMethodName(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "DINHEIRO",
            PaymentMethod.CreditCard => "CARTAO CREDITO",
            PaymentMethod.DebitCard => "CARTAO DEBITO",
            PaymentMethod.Pix => "PIX",
            _ => "OUTROS"
        };
    }

    private static void AppendQrCodeUrl(StringBuilder sb, string url)
    {
        // Break URL into multiple lines of LineWidth chars for thermal printer
        const int urlLineWidth = LineWidth - 2; // Leave some margin
        var remaining = url;

        while (remaining.Length > 0)
        {
            var chunk = remaining.Length <= urlLineWidth
                ? remaining
                : remaining[..urlLineWidth];

            sb.AppendLine(chunk);
            remaining = remaining.Length <= urlLineWidth
                ? string.Empty
                : remaining[urlLineWidth..];
        }
    }
}
