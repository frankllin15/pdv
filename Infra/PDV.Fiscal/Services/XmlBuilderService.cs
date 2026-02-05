using System.Text;
using System.Xml;
using PDV.Core.Entities;
using PDV.Fiscal.Utilities;
using PDV.Shared.Enums;

namespace PDV.Fiscal.Services;

/// <summary>
/// Service for building NFC-e XML documents.
/// </summary>
public class XmlBuilderService
{
    private const string NfeNamespace = "http://www.portalfiscal.inf.br/nfe";
    private const string NfeVersion = "4.00";
    private const int NfceModel = 65;

    /// <summary>
    /// Builds the NFC-e XML for a sale.
    /// </summary>
    public string BuildNfceXml(
        Sale sale,
        FiscalConfiguration config,
        int number,
        int series,
        string accessKey,
        bool isContingency = false)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var writer = XmlWriter.Create(stringWriter, settings);

        writer.WriteStartDocument();
        writer.WriteStartElement("NFe", NfeNamespace);

        WriteInfNFe(writer, sale, config, number, series, accessKey, isContingency);

        writer.WriteEndElement(); // NFe
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    private void WriteInfNFe(
        XmlWriter writer,
        Sale sale,
        FiscalConfiguration config,
        int number,
        int series,
        string accessKey,
        bool isContingency)
    {
        writer.WriteStartElement("infNFe");
        writer.WriteAttributeString("Id", $"NFe{accessKey}");
        writer.WriteAttributeString("versao", NfeVersion);

        WriteIde(writer, sale, config, number, series, accessKey, isContingency);
        WriteEmit(writer, config);
        WriteDest(writer, sale);
        WriteProducts(writer, sale);
        WriteTotal(writer, sale);
        WriteTransp(writer);
        WritePag(writer, sale);
        WriteInfAdic(writer);

        writer.WriteEndElement(); // infNFe
    }

    private void WriteIde(
        XmlWriter writer,
        Sale sale,
        FiscalConfiguration config,
        int number,
        int series,
        string accessKey,
        bool isContingency)
    {
        var stateCode = AccessKeyGenerator.GetStateCode(config.State);
        var numericCode = accessKey.Substring(35, 8);

        writer.WriteStartElement("ide");
        writer.WriteElementString("cUF", stateCode);
        writer.WriteElementString("cNF", numericCode);
        writer.WriteElementString("natOp", "VENDA");
        writer.WriteElementString("mod", NfceModel.ToString());
        writer.WriteElementString("serie", series.ToString());
        writer.WriteElementString("nNF", number.ToString());
        writer.WriteElementString("dhEmi", sale.SaleDate.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        writer.WriteElementString("tpNF", "1"); // 1=Saída
        writer.WriteElementString("idDest", "1"); // 1=Operação interna
        writer.WriteElementString("cMunFG", config.CityCode);
        writer.WriteElementString("tpImp", "4"); // 4=DANFE NFC-e
        writer.WriteElementString("tpEmis", isContingency ? "9" : "1"); // 1=Normal, 9=Contingência offline
        writer.WriteElementString("cDV", accessKey[43].ToString());
        writer.WriteElementString("tpAmb", config.IsProduction ? "1" : "2"); // 1=Produção, 2=Homologação
        writer.WriteElementString("finNFe", "1"); // 1=NF-e normal
        writer.WriteElementString("indFinal", "1"); // 1=Consumidor final
        writer.WriteElementString("indPres", "1"); // 1=Operação presencial
        writer.WriteElementString("procEmi", "0"); // 0=Aplicativo próprio
        writer.WriteElementString("verProc", "PDV 1.0");
        writer.WriteEndElement(); // ide
    }

    private void WriteEmit(XmlWriter writer, FiscalConfiguration config)
    {
        writer.WriteStartElement("emit");
        writer.WriteElementString("CNPJ", config.TaxId);
        writer.WriteElementString("xNome", TextSanitizer.SanitizeCompanyName(config.LegalName));
        writer.WriteElementString("xFant", TextSanitizer.SanitizeCompanyName(config.TradeName));

        writer.WriteStartElement("enderEmit");
        writer.WriteElementString("xLgr", TextSanitizer.SanitizeAddress(config.Address));
        writer.WriteElementString("nro", config.AddressNumber);
        if (!string.IsNullOrEmpty(config.Neighborhood))
            writer.WriteElementString("xBairro", TextSanitizer.SanitizeForXml(config.Neighborhood, 60));
        writer.WriteElementString("cMun", config.CityCode);
        writer.WriteElementString("xMun", GetCityName(config.CityCode)); // Would need a lookup table
        writer.WriteElementString("UF", config.State);
        writer.WriteElementString("CEP", TextSanitizer.OnlyNumbers(config.ZipCode));
        writer.WriteElementString("cPais", "1058");
        writer.WriteElementString("xPais", "BRASIL");
        writer.WriteEndElement(); // enderEmit

        writer.WriteElementString("IE", TextSanitizer.OnlyNumbers(config.StateRegistration));
        writer.WriteElementString("CRT", config.TaxRegime.ToString());
        writer.WriteEndElement(); // emit
    }

    private void WriteDest(XmlWriter writer, Sale sale)
    {
        if (string.IsNullOrEmpty(sale.CustomerDocument))
        {
            return; // NFC-e doesn't require consumer data
        }

        var document = TextSanitizer.OnlyNumbers(sale.CustomerDocument);

        writer.WriteStartElement("dest");

        if (document.Length == 11)
        {
            writer.WriteElementString("CPF", document);
        }
        else if (document.Length == 14)
        {
            writer.WriteElementString("CNPJ", document);
        }

        writer.WriteElementString("indIEDest", "9"); // 9=Não contribuinte
        writer.WriteEndElement(); // dest
    }

    private void WriteProducts(XmlWriter writer, Sale sale)
    {
        var itemNumber = 1;
        foreach (var item in sale.Items)
        {
            writer.WriteStartElement("det");
            writer.WriteAttributeString("nItem", itemNumber.ToString());

            WriteProd(writer, item, itemNumber);
            WriteImposto(writer, item);

            writer.WriteEndElement(); // det
            itemNumber++;
        }
    }

    private void WriteProd(XmlWriter writer, SaleItem item, int itemNumber)
    {
        writer.WriteStartElement("prod");
        writer.WriteElementString("cProd", item.ProductId.ToString()[..8].ToUpperInvariant());
        writer.WriteElementString("cEAN", "SEM GTIN");
        writer.WriteElementString("xProd", TextSanitizer.SanitizeProductDescription(item.ProductDescription));
        writer.WriteElementString("NCM", "00000000"); // Should come from Product
        writer.WriteElementString("CFOP", "5102"); // Should come from Product
        writer.WriteElementString("uCom", "UN");
        writer.WriteElementString("qCom", TextSanitizer.FormatQuantity(item.Quantity));
        writer.WriteElementString("vUnCom", TextSanitizer.FormatDecimal(item.UnitPrice, 10));
        writer.WriteElementString("vProd", TextSanitizer.FormatMoney(item.Quantity * item.UnitPrice));
        writer.WriteElementString("cEANTrib", "SEM GTIN");
        writer.WriteElementString("uTrib", "UN");
        writer.WriteElementString("qTrib", TextSanitizer.FormatQuantity(item.Quantity));
        writer.WriteElementString("vUnTrib", TextSanitizer.FormatDecimal(item.UnitPrice, 10));
        writer.WriteElementString("indTot", "1"); // 1=Compõe total

        if (item.Discount > 0)
        {
            writer.WriteElementString("vDesc", TextSanitizer.FormatMoney(item.Discount));
        }

        writer.WriteEndElement(); // prod
    }

    private void WriteImposto(XmlWriter writer, SaleItem item)
    {
        // Simplified tax calculation for Simples Nacional
        writer.WriteStartElement("imposto");

        // ICMS
        writer.WriteStartElement("ICMS");
        writer.WriteStartElement("ICMSSN102"); // Simples Nacional - mercadoria adquirida não sujeita a substituição
        writer.WriteElementString("orig", "0"); // Nacional
        writer.WriteElementString("CSOSN", "102"); // Simples Nacional - tributada sem permissão de crédito
        writer.WriteEndElement(); // ICMSSN102
        writer.WriteEndElement(); // ICMS

        // PIS
        writer.WriteStartElement("PIS");
        writer.WriteStartElement("PISOutr");
        writer.WriteElementString("CST", "99");
        writer.WriteElementString("vBC", "0.00");
        writer.WriteElementString("pPIS", "0.00");
        writer.WriteElementString("vPIS", "0.00");
        writer.WriteEndElement(); // PISOutr
        writer.WriteEndElement(); // PIS

        // COFINS
        writer.WriteStartElement("COFINS");
        writer.WriteStartElement("COFINSOutr");
        writer.WriteElementString("CST", "99");
        writer.WriteElementString("vBC", "0.00");
        writer.WriteElementString("pCOFINS", "0.00");
        writer.WriteElementString("vCOFINS", "0.00");
        writer.WriteEndElement(); // COFINSOutr
        writer.WriteEndElement(); // COFINS

        writer.WriteEndElement(); // imposto
    }

    private void WriteTotal(XmlWriter writer, Sale sale)
    {
        writer.WriteStartElement("total");
        writer.WriteStartElement("ICMSTot");
        writer.WriteElementString("vBC", "0.00");
        writer.WriteElementString("vICMS", "0.00");
        writer.WriteElementString("vICMSDeson", "0.00");
        writer.WriteElementString("vFCP", "0.00");
        writer.WriteElementString("vBCST", "0.00");
        writer.WriteElementString("vST", "0.00");
        writer.WriteElementString("vFCPST", "0.00");
        writer.WriteElementString("vFCPSTRet", "0.00");
        writer.WriteElementString("vProd", TextSanitizer.FormatMoney(sale.Subtotal));
        writer.WriteElementString("vFrete", "0.00");
        writer.WriteElementString("vSeg", "0.00");
        writer.WriteElementString("vDesc", TextSanitizer.FormatMoney(sale.Discount));
        writer.WriteElementString("vII", "0.00");
        writer.WriteElementString("vIPI", "0.00");
        writer.WriteElementString("vIPIDevol", "0.00");
        writer.WriteElementString("vPIS", "0.00");
        writer.WriteElementString("vCOFINS", "0.00");
        writer.WriteElementString("vOutro", "0.00");
        writer.WriteElementString("vNF", TextSanitizer.FormatMoney(sale.Total));
        writer.WriteEndElement(); // ICMSTot
        writer.WriteEndElement(); // total
    }

    private void WriteTransp(XmlWriter writer)
    {
        writer.WriteStartElement("transp");
        writer.WriteElementString("modFrete", "9"); // 9=Sem frete
        writer.WriteEndElement(); // transp
    }

    private void WritePag(XmlWriter writer, Sale sale)
    {
        writer.WriteStartElement("pag");

        foreach (var payment in sale.Payments)
        {
            writer.WriteStartElement("detPag");
            writer.WriteElementString("tPag", GetPaymentTypeCode(payment.Method));
            writer.WriteElementString("vPag", TextSanitizer.FormatMoney(payment.Amount));

            // Card information
            if (payment.Method is PaymentMethod.CreditCard or PaymentMethod.DebitCard)
            {
                writer.WriteStartElement("card");
                writer.WriteElementString("tpIntegra", "2"); // 2=Não integrado
                writer.WriteElementString("tBand", GetCardBrandCode(payment.CardBrand));
                if (!string.IsNullOrEmpty(payment.AuthorizationCode))
                {
                    writer.WriteElementString("cAut", payment.AuthorizationCode);
                }
                writer.WriteEndElement(); // card
            }

            writer.WriteEndElement(); // detPag
        }

        // Change
        var change = sale.GetChange();
        if (change > 0)
        {
            writer.WriteElementString("vTroco", TextSanitizer.FormatMoney(change));
        }

        writer.WriteEndElement(); // pag
    }

    private void WriteInfAdic(XmlWriter writer)
    {
        writer.WriteStartElement("infAdic");
        writer.WriteElementString("infCpl", "Documento emitido por PDV - Ponto de Venda");
        writer.WriteEndElement(); // infAdic
    }

    private static string GetPaymentTypeCode(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "01",
            PaymentMethod.DebitCard => "04",
            PaymentMethod.CreditCard => "03",
            PaymentMethod.Pix => "17",
            _ => "99" // Outros
        };
    }

    private static string GetCardBrandCode(string? brand)
    {
        if (string.IsNullOrEmpty(brand))
            return "99";

        return brand.ToUpperInvariant() switch
        {
            "VISA" => "01",
            "MASTERCARD" => "02",
            "AMEX" or "AMERICAN EXPRESS" => "03",
            "ELO" => "04",
            "HIPERCARD" => "05",
            "DINERS" or "DINERS CLUB" => "06",
            _ => "99"
        };
    }

    private static string GetCityName(string cityCode)
    {
        // This would need a proper lookup table
        // For now, return a placeholder
        return "CIDADE";
    }
}
