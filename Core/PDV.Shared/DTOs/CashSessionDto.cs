using PDV.Shared.Enums;

namespace PDV.Shared.DTOs;

public record CashSessionDto(
    Guid Id,
    Guid OperatorId,
    string OperatorName,
    string TerminalId,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal CalculatedBalance,
    SessionStatus Status
);

public record CashTransactionDto(
    Guid Id,
    Guid CashSessionId,
    CashTransactionType Type,
    decimal Amount,
    string? Description,
    Guid OperatorId,
    string OperatorName,
    DateTime TransactionDate
);

public record CashBalanceResultDto(
    decimal OpeningBalance,
    decimal TotalSupply,
    decimal TotalBleed,
    decimal TotalSalesCash,
    decimal TotalSalesCard,
    decimal TotalSalesPix,
    decimal TotalSalesOther)
{
    public decimal ExpectedCashBalance =>
        OpeningBalance + TotalSupply - TotalBleed + TotalSalesCash;
}
