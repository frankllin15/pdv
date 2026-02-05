using PDV.Core.Entities;
using PDV.Fiscal.Utilities;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PDV.Fiscal.Services;

/// <summary>
/// Service for generating DANFE NFC-e as PDF with QR code.
/// Uses QuestPDF for PDF generation.
/// </summary>
public class DanfePdfService
{
    private const float PageWidth = 80f; // 80mm thermal paper width
    private const float Margin = 3f;     // 3mm margins

    static DanfePdfService()
    {
        // Configure QuestPDF license (Community license is free for small businesses)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a DANFE PDF with QR code.
    /// </summary>
    /// <param name="sale">Sale data</param>
    /// <param name="config">Fiscal configuration</param>
    /// <param name="transaction">Fiscal transaction</param>
    /// <param name="qrCodeBytes">QR code image as PNG bytes (optional)</param>
    /// <param name="isReprint">Whether this is a reprint</param>
    /// <param name="reprintDate">Reprint date/time</param>
    /// <param name="reprintNumber">Reprint number</param>
    /// <returns>PDF as byte array</returns>
    public byte[] GeneratePdf(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction,
        byte[]? qrCodeBytes = null,
        bool isReprint = false,
        DateTime? reprintDate = null,
        int reprintNumber = 0)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageWidth, 297, Unit.Millimetre); // 80mm wide, auto height (will be trimmed)
                page.MarginHorizontal(Margin, Unit.Millimetre);
                page.MarginVertical(2, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Content().Column(column =>
                {
                    column.Spacing(2);

                    // Header - Company Info
                    BuildHeader(column, config);

                    // Reprint marker
                    if (isReprint)
                    {
                        BuildReprintMarker(column, reprintDate ?? DateTime.Now, reprintNumber);
                    }

                    // Document info
                    BuildDocumentInfo(column, transaction, sale);

                    // Consumer
                    BuildConsumerInfo(column, sale);

                    // Items
                    BuildItems(column, sale);

                    // Totals
                    BuildTotals(column, sale);

                    // Payments
                    BuildPayments(column, sale);

                    // Fiscal info
                    BuildFiscalInfo(column, transaction, config);

                    // QR Code
                    BuildQrCodeSection(column, qrCodeBytes, config, transaction);

                    // Footer
                    BuildFooter(column, config);
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Generates a DANFE PDF from a DanfeResult.
    /// </summary>
    public byte[] GeneratePdfFromResult(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction,
        DanfeResult danfeResult)
    {
        byte[]? qrCodeBytes = null;

        if (!string.IsNullOrEmpty(danfeResult.QrCodeBase64))
        {
            qrCodeBytes = Convert.FromBase64String(danfeResult.QrCodeBase64);
        }

        return GeneratePdf(
            sale, config, transaction, qrCodeBytes,
            danfeResult.IsReprint,
            danfeResult.IsReprint ? DateTime.Now : null,
            danfeResult.ReprintNumber);
    }

    /// <summary>
    /// Saves the DANFE PDF to a file.
    /// </summary>
    public void SavePdf(
        Sale sale,
        FiscalConfiguration config,
        FiscalTransaction transaction,
        string filePath,
        byte[]? qrCodeBytes = null,
        bool isReprint = false,
        DateTime? reprintDate = null,
        int reprintNumber = 0)
    {
        var pdfBytes = GeneratePdf(sale, config, transaction, qrCodeBytes, isReprint, reprintDate, reprintNumber);
        File.WriteAllBytes(filePath, pdfBytes);
    }

    private static void BuildHeader(ColumnDescriptor column, FiscalConfiguration config)
    {
        column.Item().AlignCenter().Text(config.TradeName.ToUpperInvariant())
            .Bold().FontSize(10);

        column.Item().AlignCenter().Text(config.LegalName)
            .FontSize(7);

        column.Item().AlignCenter().Text($"CNPJ: {DocumentValidator.FormatCnpj(config.TaxId)}")
            .FontSize(7);

        column.Item().AlignCenter().Text(config.Address)
            .FontSize(7);

        column.Item().AlignCenter().Text($"{config.Neighborhood} - CEP: {FormatZipCode(config.ZipCode)}")
            .FontSize(7);

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildReprintMarker(ColumnDescriptor column, DateTime reprintDate, int reprintNumber)
    {
        column.Item().Border(1).BorderColor(Colors.Black).Padding(4).Column(inner =>
        {
            inner.Item().AlignCenter().Text("** REIMPRESSAO - 2a VIA **")
                .Bold().FontSize(9);
            inner.Item().AlignCenter().Text($"Reimpressao #{reprintNumber}")
                .FontSize(7);
            inner.Item().AlignCenter().Text($"Data: {reprintDate:dd/MM/yyyy HH:mm}")
                .FontSize(7);
        });

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildDocumentInfo(ColumnDescriptor column, FiscalTransaction transaction, Sale sale)
    {
        column.Item().AlignCenter().Text("DANFE NFC-e")
            .Bold().FontSize(9);

        column.Item().AlignCenter().Text("Documento Auxiliar da Nota Fiscal")
            .FontSize(6);

        column.Item().AlignCenter().Text("Eletronica para Consumidor Final")
            .FontSize(6);

        column.Item().AlignCenter().Text($"NFC-e n. {transaction.Number} Serie {transaction.Series}")
            .Bold().FontSize(8);

        column.Item().AlignCenter().Text(sale.SaleDate.ToString("dd/MM/yyyy HH:mm:ss"))
            .FontSize(7);

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildConsumerInfo(ColumnDescriptor column, Sale sale)
    {
        if (!string.IsNullOrEmpty(sale.CustomerDocument))
        {
            var doc = TextSanitizer.OnlyNumbers(sale.CustomerDocument);
            var label = doc.Length == 11 ? "CPF" : "CNPJ";
            var formatted = doc.Length == 11
                ? DocumentValidator.FormatCpf(doc)
                : DocumentValidator.FormatCnpj(doc);

            column.Item().Text($"{label} do Consumidor: {formatted}")
                .FontSize(7);
        }
        else
        {
            column.Item().Text("CONSUMIDOR NAO IDENTIFICADO")
                .FontSize(7);
        }

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildItems(ColumnDescriptor column, Sale sale)
    {
        // Header
        column.Item().Row(row =>
        {
            row.RelativeItem(2).Text("CODIGO").FontSize(6).Bold();
            row.RelativeItem(4).Text("DESCRICAO").FontSize(6).Bold();
            row.RelativeItem(2).AlignRight().Text("VALOR").FontSize(6).Bold();
        });

        column.Item().LineHorizontal(0.3f).LineColor(Colors.Grey.Lighten1);

        // Items
        foreach (var item in sale.Items)
        {
            var code = item.ProductId.ToString()[..6].ToUpperInvariant();
            var desc = TruncateString(TextSanitizer.SanitizeForXml(item.ProductDescription), 25);
            var total = item.Quantity * item.UnitPrice;

            column.Item().Row(row =>
            {
                row.RelativeItem(2).Text(code).FontSize(6);
                row.RelativeItem(4).Text(desc).FontSize(6);
                row.RelativeItem(2).AlignRight().Text($"{total:N2}").FontSize(6);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem(2).Text("");
                row.RelativeItem(6).Text($"   {item.Quantity:N3} x {item.UnitPrice:N2}").FontSize(5).FontColor(Colors.Grey.Darken1);
            });

            if (item.Discount > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem(2).Text("");
                    row.RelativeItem(6).Text($"   Desconto: -{item.Discount:N2}").FontSize(5).FontColor(Colors.Green.Darken1);
                });
            }
        }

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildTotals(ColumnDescriptor column, Sale sale)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().Text("QTD. ITENS").FontSize(7);
            row.RelativeItem().AlignRight().Text(sale.Items.Count.ToString()).FontSize(7);
        });

        column.Item().Row(row =>
        {
            row.RelativeItem().Text("SUBTOTAL").FontSize(7);
            row.RelativeItem().AlignRight().Text($"R$ {sale.Subtotal:N2}").FontSize(7);
        });

        if (sale.Discount > 0)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("DESCONTO").FontSize(7).FontColor(Colors.Green.Darken1);
                row.RelativeItem().AlignRight().Text($"-R$ {sale.Discount:N2}").FontSize(7).FontColor(Colors.Green.Darken1);
            });
        }

        column.Item().Row(row =>
        {
            row.RelativeItem().Text("VALOR TOTAL").Bold().FontSize(9);
            row.RelativeItem().AlignRight().Text($"R$ {sale.Total:N2}").Bold().FontSize(9);
        });

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildPayments(ColumnDescriptor column, Sale sale)
    {
        column.Item().Text("FORMA DE PAGAMENTO").FontSize(7).Bold();

        foreach (var payment in sale.Payments)
        {
            var methodName = GetPaymentMethodName(payment.Method);
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(methodName).FontSize(7);
                row.RelativeItem().AlignRight().Text($"R$ {payment.Amount:N2}").FontSize(7);
            });
        }

        var change = sale.GetChange();
        if (change > 0)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("TROCO").FontSize(7);
                row.RelativeItem().AlignRight().Text($"R$ {change:N2}").FontSize(7);
            });
        }

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildFiscalInfo(ColumnDescriptor column, FiscalTransaction transaction, FiscalConfiguration config)
    {
        column.Item().AlignCenter().Text("INFORMACOES FISCAIS")
            .Bold().FontSize(8);

        // Contingency warning
        if (transaction.IsContingency)
        {
            column.Item().Background(Colors.Orange.Lighten3).Padding(4).Column(inner =>
            {
                inner.Item().AlignCenter().Text("*** EMITIDA EM CONTINGENCIA ***")
                    .Bold().FontSize(8).FontColor(Colors.Orange.Darken3);
                inner.Item().AlignCenter().Text("Pendente de transmissao a SEFAZ")
                    .FontSize(7).FontColor(Colors.Orange.Darken2);
            });
        }

        // Consultation URL
        column.Item().AlignCenter().Text("Consulte pela Chave de Acesso em:")
            .FontSize(6);
        column.Item().AlignCenter().Text(QrCodeGenerator.GetConsultationUrl(config.State, config.IsProduction))
            .FontSize(6).FontColor(Colors.Blue.Darken1);

        // Access Key
        column.Item().PaddingTop(4).AlignCenter().Text("CHAVE DE ACESSO")
            .Bold().FontSize(7);

        var formattedKey = FormatAccessKey(transaction.AccessKey);
        column.Item().AlignCenter().Text(formattedKey)
            .FontSize(6).FontFamily("Courier New");

        // Protocol
        if (!string.IsNullOrEmpty(transaction.Protocol))
        {
            column.Item().PaddingTop(4).Text($"Protocolo: {transaction.Protocol}")
                .FontSize(6);

            if (transaction.AuthorizationDate.HasValue)
            {
                column.Item().Text($"Data Autorizacao: {transaction.AuthorizationDate.Value:dd/MM/yyyy HH:mm:ss}")
                    .FontSize(6);
            }
        }

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildQrCodeSection(ColumnDescriptor column, byte[]? qrCodeBytes, FiscalConfiguration config, FiscalTransaction transaction)
    {
        column.Item().AlignCenter().Text("QR CODE PARA CONSULTA")
            .Bold().FontSize(7);

        if (qrCodeBytes != null && qrCodeBytes.Length > 0)
        {
            // Display QR Code image
            column.Item().AlignCenter().Padding(4).Width(50, Unit.Millimetre).Height(50, Unit.Millimetre)
                .Image(qrCodeBytes);
        }
        else
        {
            // No QR Code available
            column.Item().AlignCenter().Padding(8).Border(1).BorderColor(Colors.Grey.Lighten1)
                .Text("QR CODE NAO DISPONIVEL\n(CSC nao configurado)")
                .FontSize(7).FontColor(Colors.Grey.Darken1);
        }

        column.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void BuildFooter(ColumnDescriptor column, FiscalConfiguration config)
    {
        if (config.IsProduction)
        {
            column.Item().AlignCenter().Text("AMBIENTE DE PRODUCAO")
                .FontSize(7);
        }
        else
        {
            column.Item().Background(Colors.Yellow.Lighten3).Padding(4).Column(inner =>
            {
                inner.Item().AlignCenter().Text("** SEM VALOR FISCAL **")
                    .Bold().FontSize(8);
                inner.Item().AlignCenter().Text("** HOMOLOGACAO **")
                    .Bold().FontSize(8);
            });
        }

        column.Item().PaddingTop(8).AlignCenter().Text("Obrigado pela preferencia!")
            .FontSize(6).Italic();
    }

    private static string FormatZipCode(string zipCode)
    {
        zipCode = TextSanitizer.OnlyNumbers(zipCode);
        if (zipCode.Length != 8)
            return zipCode;

        return $"{zipCode[..5]}-{zipCode.Substring(5, 3)}";
    }

    private static string FormatAccessKey(string accessKey)
    {
        if (string.IsNullOrEmpty(accessKey) || accessKey.Length != 44)
            return accessKey;

        // Format in groups of 4
        var parts = new List<string>();
        for (int i = 0; i < accessKey.Length; i += 4)
        {
            parts.Add(accessKey.Substring(i, Math.Min(4, accessKey.Length - i)));
        }

        // Split into two lines
        var line1 = string.Join(" ", parts.Take(6));
        var line2 = string.Join(" ", parts.Skip(6));

        return $"{line1}\n{line2}";
    }

    private static string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= maxLength ? text : text[..maxLength];
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
}
