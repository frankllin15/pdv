using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Queries;

public class CashSessionQuery(string connectionString) : ICashSessionQuery
{
    private IDbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task<CashSessionDto?> GetOpenSessionByTerminalAsync(string terminalId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT cs.Id, cs.OperatorId, o.Name as OperatorName, cs.TerminalId,
                   cs.OpenedAt, cs.ClosedAt, cs.OpeningBalance, cs.ClosingBalance,
                   cs.CalculatedBalance, cs.Status
            FROM CashSessions cs
            LEFT JOIN Operators o ON cs.OperatorId = o.Id
            WHERE cs.TerminalId = @TerminalId AND cs.Status = @Status
            LIMIT 1";

        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<CashSessionQueryResult>(sql, new
        {
            TerminalId = terminalId,
            Status = (int)SessionStatus.Open
        });

        if (result == null) return null;

        return new CashSessionDto(
            result.GetId(),
            result.GetOperatorId(),
            result.OperatorName ?? string.Empty,
            result.TerminalId,
            result.OpenedAt,
            result.ClosedAt,
            result.OpeningBalance,
            result.ClosingBalance,
            result.CalculatedBalance,
            (SessionStatus)result.Status
        );
    }

    public async Task<CashBalanceResultDto> CalculateSessionBalanceAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        const string sessionSql = @"
            SELECT OpeningBalance FROM CashSessions WHERE Id = @SessionId";

        const string transactionsSql = @"
            SELECT
                COALESCE(SUM(CASE WHEN Type = @Supply THEN Amount ELSE 0 END), 0) as TotalSupply,
                COALESCE(SUM(CASE WHEN Type = @Bleed THEN Amount ELSE 0 END), 0) as TotalBleed
            FROM CashTransactions
            WHERE CashSessionId = @SessionId";

        const string paymentsSql = @"
            SELECT
                COALESCE(SUM(CASE WHEN p.Method = @Cash THEN p.Amount ELSE 0 END), 0)
                    - COALESCE((SELECT SUM(s2.Change) FROM Sales s2 WHERE s2.CashSessionId = @SessionId AND s2.Status = @Completed AND s2.Change > 0), 0) as TotalSalesCash,
                COALESCE(SUM(CASE WHEN p.Method IN (@CreditCard, @DebitCard) THEN p.Amount ELSE 0 END), 0) as TotalSalesCard,
                COALESCE(SUM(CASE WHEN p.Method = @Pix THEN p.Amount ELSE 0 END), 0) as TotalSalesPix,
                COALESCE(SUM(CASE WHEN p.Method NOT IN (@Cash, @CreditCard, @DebitCard, @Pix) THEN p.Amount ELSE 0 END), 0) as TotalSalesOther
            FROM Payments p
            INNER JOIN Sales s ON p.SaleId = s.Id
            WHERE s.CashSessionId = @SessionId AND s.Status = @Completed";

        using var connection = CreateConnection();

        var openingBalance = await connection.ExecuteScalarAsync<decimal>(sessionSql, new { SessionId = sessionId });

        var txResult = await connection.QueryFirstOrDefaultAsync<TransactionSumResult>(transactionsSql, new
        {
            SessionId = sessionId,
            Supply = (int)CashTransactionType.Supply,
            Bleed = (int)CashTransactionType.Bleed
        });

        var pmtResult = await connection.QueryFirstOrDefaultAsync<PaymentSumResult>(paymentsSql, new
        {
            SessionId = sessionId,
            Cash = (int)PaymentMethod.Cash,
            CreditCard = (int)PaymentMethod.CreditCard,
            DebitCard = (int)PaymentMethod.DebitCard,
            Pix = (int)PaymentMethod.Pix,
            Completed = (int)SaleStatus.Completed
        });

        return new CashBalanceResultDto(
            openingBalance,
            txResult?.TotalSupply ?? 0,
            txResult?.TotalBleed ?? 0,
            pmtResult?.TotalSalesCash ?? 0,
            pmtResult?.TotalSalesCard ?? 0,
            pmtResult?.TotalSalesPix ?? 0,
            pmtResult?.TotalSalesOther ?? 0
        );
    }

    public async Task<IEnumerable<CashTransactionDto>> GetTransactionsBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT ct.Id, ct.CashSessionId, ct.Type, ct.Amount, ct.Description,
                   ct.OperatorId, o.Name as OperatorName, ct.TransactionDate
            FROM CashTransactions ct
            LEFT JOIN Operators o ON ct.OperatorId = o.Id
            WHERE ct.CashSessionId = @SessionId
            ORDER BY ct.TransactionDate DESC";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<CashTransactionQueryResult>(sql, new { SessionId = sessionId });

        return results.Select(r => new CashTransactionDto(
            r.GetId(),
            r.GetCashSessionId(),
            (CashTransactionType)r.Type,
            r.Amount,
            r.Description,
            r.GetOperatorId(),
            r.OperatorName ?? string.Empty,
            r.TransactionDate
        ));
    }

    private class CashSessionQueryResult
    {
        public object Id { get; set; } = null!;
        public object OperatorId { get; set; } = null!;
        public string? OperatorName { get; set; }
        public string TerminalId { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal CalculatedBalance { get; set; }
        public int Status { get; set; }

        public Guid GetId() => ParseGuid(Id);
        public Guid GetOperatorId() => ParseGuid(OperatorId);

        private static Guid ParseGuid(object value) => value switch
        {
            string s => Guid.Parse(s),
            byte[] b => new Guid(b),
            Guid g => g,
            _ => Guid.Empty
        };
    }

    private class CashTransactionQueryResult
    {
        public object Id { get; set; } = null!;
        public object CashSessionId { get; set; } = null!;
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public object OperatorId { get; set; } = null!;
        public string? OperatorName { get; set; }
        public DateTime TransactionDate { get; set; }

        public Guid GetId() => ParseGuid(Id);
        public Guid GetCashSessionId() => ParseGuid(CashSessionId);
        public Guid GetOperatorId() => ParseGuid(OperatorId);

        private static Guid ParseGuid(object value) => value switch
        {
            string s => Guid.Parse(s),
            byte[] b => new Guid(b),
            Guid g => g,
            _ => Guid.Empty
        };
    }

    private class TransactionSumResult
    {
        public decimal TotalSupply { get; set; }
        public decimal TotalBleed { get; set; }
    }

    private class PaymentSumResult
    {
        public decimal TotalSalesCash { get; set; }
        public decimal TotalSalesCard { get; set; }
        public decimal TotalSalesPix { get; set; }
        public decimal TotalSalesOther { get; set; }
    }
}
