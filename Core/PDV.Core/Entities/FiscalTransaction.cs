using PDV.Core.Entities.Base;
using PDV.Shared.Enums;

namespace PDV.Core.Entities;

public class FiscalTransaction : Entity
{
    public Guid SaleId { get; private set; }
    public string AccessKey { get; private set; } = string.Empty;  // 44 digits
    public int Number { get; private set; }                         // NFC-e number
    public int Series { get; private set; }                         // Series
    public FiscalStatus Status { get; private set; }
    public string? Protocol { get; private set; }                   // SEFAZ protocol
    public int? StatusCode { get; private set; }                    // SEFAZ cStat
    public string? StatusMessage { get; private set; }              // SEFAZ xMotivo
    public string? XmlRequest { get; private set; }                 // Sent XML
    public string? XmlResponse { get; private set; }                // Response XML
    public bool IsContingency { get; private set; }
    public DateTime? AuthorizationDate { get; private set; }
    public DateTime? CancellationDate { get; private set; }
    public string? CancellationProtocol { get; private set; }
    public string? CancellationJustification { get; private set; }

    public Sale Sale { get; private set; } = null!;

    private FiscalTransaction() { }

    public FiscalTransaction(
        Guid saleId,
        string accessKey,
        int number,
        int series,
        bool isContingency = false)
    {
        SaleId = saleId;
        AccessKey = accessKey;
        Number = number;
        Series = series;
        Status = isContingency ? FiscalStatus.Contingency : FiscalStatus.Pending;
        IsContingency = isContingency;
    }

    public void SetXmlRequest(string xml)
    {
        XmlRequest = xml;
        SetUpdatedAt();
    }

    public void SetAuthorized(string protocol, int statusCode, string statusMessage, string? xmlResponse = null)
    {
        Protocol = protocol;
        StatusCode = statusCode;
        StatusMessage = statusMessage;
        XmlResponse = xmlResponse;
        Status = FiscalStatus.Authorized;
        AuthorizationDate = DateTime.UtcNow;
        IsContingency = false;
        SetUpdatedAt();
    }

    public void SetRejected(int statusCode, string statusMessage, string? xmlResponse = null)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
        XmlResponse = xmlResponse;
        Status = FiscalStatus.Rejected;
        SetUpdatedAt();
    }

    public void SetCancelled(string protocol, string justification)
    {
        CancellationProtocol = protocol;
        CancellationJustification = justification;
        CancellationDate = DateTime.UtcNow;
        Status = FiscalStatus.Cancelled;
        SetUpdatedAt();
    }

    public void SetContingency()
    {
        Status = FiscalStatus.Contingency;
        IsContingency = true;
        SetUpdatedAt();
    }
}
