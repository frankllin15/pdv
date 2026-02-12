using PDV.Core.Entities.Base;
using PDV.Shared.Enums;

namespace PDV.Core.Entities;

public class CashTransaction : Entity
{
    public Guid CashSessionId { get; private set; }
    public CashTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public Guid OperatorId { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public SyncState SyncState { get; private set; } = SyncState.Pending;

    public CashSession CashSession { get; private set; } = null!;

    private CashTransaction() { }

    public CashTransaction(Guid cashSessionId, CashTransactionType type, decimal amount, string? description, Guid operatorId)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        CashSessionId = cashSessionId;
        Type = type;
        Amount = amount;
        Description = description;
        OperatorId = operatorId;
        TransactionDate = DateTime.UtcNow;
    }

    public void SetSyncState(SyncState state)
    {
        SyncState = state;
        SetUpdatedAt();
    }
}
