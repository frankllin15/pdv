using PDV.Core.Entities.Base;
using PDV.Shared.Enums;

namespace PDV.Core.Entities;

public class CashSession : Entity
{
    public Guid OperatorId { get; private set; }
    public string TerminalId { get; private set; } = string.Empty;
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public decimal CalculatedBalance { get; private set; }
    public SessionStatus Status { get; private set; }
    public SyncState SyncState { get; private set; } = SyncState.Pending;

    public decimal Difference => ClosingBalance - CalculatedBalance;

    private readonly List<CashTransaction> _transactions = new();
    public IReadOnlyCollection<CashTransaction> Transactions => _transactions.AsReadOnly();

    private CashSession() { }

    public CashSession(Guid operatorId, string terminalId, decimal openingBalance)
    {
        if (openingBalance < 0)
            throw new ArgumentException("Opening balance cannot be negative", nameof(openingBalance));

        OperatorId = operatorId;
        TerminalId = terminalId;
        OpeningBalance = openingBalance;
        OpenedAt = DateTime.UtcNow;
        Status = SessionStatus.Open;
    }

    public void Close(decimal countedBalance, decimal systemCalculatedBalance)
    {
        if (Status != SessionStatus.Open)
            throw new InvalidOperationException("Only open sessions can be closed");

        ClosingBalance = countedBalance;
        CalculatedBalance = systemCalculatedBalance;
        ClosedAt = DateTime.UtcNow;
        Status = SessionStatus.Closed;
        SetUpdatedAt();
    }

    public void SetSyncState(SyncState state)
    {
        SyncState = state;
        SetUpdatedAt();
    }
}
