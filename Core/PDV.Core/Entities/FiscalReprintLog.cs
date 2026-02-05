using PDV.Core.Entities.Base;

namespace PDV.Core.Entities;

/// <summary>
/// Audit log for fiscal document reprints (NFC-e DANFE).
/// Records who requested the reprint, when, and why.
/// </summary>
public class FiscalReprintLog : Entity
{
    public Guid FiscalTransactionId { get; private set; }
    public Guid OperatorId { get; private set; }
    public DateTime ReprintedAt { get; private set; }
    public string? Reason { get; private set; }
    public int ReprintNumber { get; private set; }

    // Navigation properties
    public FiscalTransaction FiscalTransaction { get; private set; } = null!;
    public Operator Operator { get; private set; } = null!;

    private FiscalReprintLog() { }

    public FiscalReprintLog(
        Guid fiscalTransactionId,
        Guid operatorId,
        int reprintNumber,
        string? reason = null)
    {
        FiscalTransactionId = fiscalTransactionId;
        OperatorId = operatorId;
        ReprintNumber = reprintNumber;
        Reason = reason;
        ReprintedAt = DateTime.UtcNow;
    }
}
