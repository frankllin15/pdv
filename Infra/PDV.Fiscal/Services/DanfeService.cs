using System.Text;
using PDV.Core.Entities;
using PDV.Fiscal.Utilities;
using PDV.Shared.Enums;

namespace PDV.Fiscal.Services;

/// <summary>
/// Service for generating DANFE (Documento Auxiliar da Nota Fiscal Eletrônica).
/// Generates text-based receipts for thermal printers.
/// </summary>
public class DanfeService
{
    private const int LineWidth = 48; // Standard 80mm thermal printer width

    /// <summary>
    /// Generates a text-based DANFE for an NFC-e.
    /// </summary>
    public string GenerateDanfeText(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction)
    {
        var sb = new StringBuilder();

        // Header
        AppendCentered(sb, config.TradeName.ToUpperInvariant());
        AppendCentered(sb, config.LegalName);
        AppendCentered(sb, $"CNPJ: {DocumentValidator.FormatCnpj(config.TaxId)}");
        AppendCentered(sb, config.Address);
        AppendCentered(sb, $"{config.Neighborhood} - CEP: {FormatZipCode(config.ZipCode)}");
        AppendLine(sb);

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
        AppendCentered(sb, GetConsultUrl(config.State));
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

        // QR Code placeholder
        AppendSeparator(sb);
        AppendCentered(sb, "[QR CODE]");
        AppendLine(sb);

        // Footer
        AppendSeparator(sb);
        AppendCentered(sb, config.IsProduction ? "AMBIENTE DE PRODUCAO" : "** SEM VALOR FISCAL **");
        AppendCentered(sb, "** HOMOLOGACAO **");
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

    private static string GetConsultUrl(string state)
    {
        return state.ToUpperInvariant() switch
        {
            "SP" => "www.nfce.fazenda.sp.gov.br",
            "RJ" => "www.fazenda.rj.gov.br/nfce",
            "MG" => "nfce.fazenda.mg.gov.br",
            "RS" => "www.sefaz.rs.gov.br/nfce",
            "PR" => "www.fazenda.pr.gov.br/nfce",
            "SC" => "sat.sef.sc.gov.br/nfce",
            "BA" => "nfe.sefaz.ba.gov.br",
            "PE" => "nfce.sefaz.pe.gov.br",
            _ => "www.nfe.fazenda.gov.br"
        };
    }
}
