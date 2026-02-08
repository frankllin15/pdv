using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Queries;

public class SaleQuery : ISaleQuery
{
    private readonly string _connectionString;

    public SaleQuery(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string saleSql = @"
            SELECT Id, SaleNumber, SaleDate, Subtotal, Discount, Total,
                   Status, CustomerDocument
            FROM Sales
            WHERE Id = @Id
            LIMIT 1";

        const string itemsSql = @"
            SELECT Id, ProductId, Barcode, ProductDescription,
                   Quantity, UnitPrice, Discount, Total
            FROM SaleItems
            WHERE SaleId = @SaleId";

        const string paymentsSql = @"
            SELECT Id, Method, Amount, AuthorizationCode, CardBrand, PaymentDate
            FROM Payments
            WHERE SaleId = @SaleId";

        using var connection = CreateConnection();

        var idBytes = id.ToByteArray();
        var sale = await connection.QueryFirstOrDefaultAsync<SaleQueryResult>(saleSql, new { Id = idBytes });
        if (sale == null) return null;

        var itemResults = await connection.QueryAsync<SaleItemQueryResult>(itemsSql, new { SaleId = idBytes });
        var items = itemResults.Select(i => new SaleItemDto(
            i.GetId(), i.GetProductId(), i.Barcode, i.ProductDescription,
            i.Quantity, i.UnitPrice, i.Discount, i.Total
        )).ToList();

        var paymentResults = await connection.QueryAsync<PaymentQueryResult>(paymentsSql, new { SaleId = idBytes });
        var payments = paymentResults.Select(p => new PaymentDto(
            p.GetId(), (PaymentMethod)p.Method, p.Amount, p.AuthorizationCode, p.CardBrand, p.PaymentDate
        )).ToList();

        return new SaleDto(
            sale.Id,
            sale.SaleNumber,
            sale.SaleDate,
            sale.Subtotal,
            sale.Discount,
            sale.Total,
            (SaleStatus)sale.Status,
            sale.CustomerDocument,
            items,
            payments
        );
    }

    public async Task<IEnumerable<SaleSummaryDto>> GetDailySalesAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        const string sql = @"
            SELECT s.Id, s.SaleNumber, s.SaleDate, s.Total, s.Status, s.CustomerDocument,
                   (SELECT COUNT(*) FROM SaleItems WHERE SaleId = s.Id) as ItemCount
            FROM Sales s
            WHERE s.SaleDate >= @StartDate AND s.SaleDate < @EndDate
            ORDER BY s.SaleDate DESC";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<SaleSummaryQueryResult>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.ToString("o")
        });

        return results.Select(r => new SaleSummaryDto(
            r.Id,
            r.SaleNumber,
            r.SaleDate,
            r.Total,
            (SaleStatus)r.Status,
            r.ItemCount,
            r.CustomerDocument
        ));
    }

    public async Task<IEnumerable<SaleSummaryDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.Id, s.SaleNumber, s.SaleDate, s.Total, s.Status, s.CustomerDocument,
                   (SELECT COUNT(*) FROM SaleItems WHERE SaleId = s.Id) as ItemCount
            FROM Sales s
            WHERE s.SaleDate >= @StartDate AND s.SaleDate <= @EndDate
            ORDER BY s.SaleDate DESC";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<SaleSummaryQueryResult>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.ToString("o")
        });

        return results.Select(r => new SaleSummaryDto(
            r.Id,
            r.SaleNumber,
            r.SaleDate,
            r.Total,
            (SaleStatus)r.Status,
            r.ItemCount,
            r.CustomerDocument
        ));
    }

    public async Task<decimal> GetDailyTotalAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        const string sql = @"
            SELECT COALESCE(SUM(Total), 0)
            FROM Sales
            WHERE SaleDate >= @StartDate AND SaleDate < @EndDate
              AND Status = @Status";

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<decimal>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.ToString("o"),
            Status = (int)SaleStatus.Completed
        });
    }

    public async Task<IEnumerable<SaleSummaryDto>> GetByDateRangeAndStatusAsync(
        DateTime startDate, DateTime endDate, SaleStatus? status = null, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT s.Id, s.SaleNumber, s.SaleDate, s.Total, s.Status, s.CustomerDocument,
                   (SELECT COUNT(*) FROM SaleItems WHERE SaleId = s.Id) as ItemCount
            FROM Sales s
            WHERE s.SaleDate >= @StartDate AND s.SaleDate <= @EndDate";

        if (status.HasValue)
        {
            sql += " AND s.Status = @Status";
        }

        sql += " ORDER BY s.SaleDate DESC";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<SaleSummaryQueryResult>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.AddDays(1).ToString("o"),
            Status = status.HasValue ? (int)status.Value : 0
        });

        return results.Select(r => new SaleSummaryDto(
            r.Id,
            r.SaleNumber,
            r.SaleDate,
            r.Total,
            (SaleStatus)r.Status,
            r.ItemCount,
            r.CustomerDocument
        ));
    }

    public async Task<SalesSummaryDto> GetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                COUNT(*) as TotalSales,
                COALESCE(SUM(CASE WHEN Status = @Completed THEN Total ELSE 0 END), 0) as TotalRevenue,
                COALESCE(AVG(CASE WHEN Status = @Completed THEN Total ELSE NULL END), 0) as AverageTicket,
                SUM(CASE WHEN Status = @Completed THEN 1 ELSE 0 END) as CompletedSales,
                SUM(CASE WHEN Status = @Cancelled THEN 1 ELSE 0 END) as CancelledSales
            FROM Sales
            WHERE SaleDate >= @StartDate AND SaleDate <= @EndDate";

        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<SalesSummaryQueryResult>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.AddDays(1).ToString("o"),
            Completed = (int)SaleStatus.Completed,
            Cancelled = (int)SaleStatus.Cancelled
        });

        return new SalesSummaryDto(
            result?.TotalSales ?? 0,
            result?.TotalRevenue ?? 0,
            result?.AverageTicket ?? 0,
            result?.CompletedSales ?? 0,
            result?.CancelledSales ?? 0
        );
    }

    public async Task<IEnumerable<PendingSaleSummaryDto>> GetPendingSalesByOperatorAsync(
        Guid operatorId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.Id, s.SaleNumber, s.SaleDate, s.Total,
                   (SELECT COUNT(*) FROM SaleItems WHERE SaleId = s.Id) as ItemCount
            FROM Sales s
            WHERE s.OperatorId = @OperatorId
              AND s.Status = @Status
            ORDER BY s.SaleDate DESC";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<PendingSaleQueryResult>(sql, new
        {
            OperatorId = operatorId.ToByteArray(),
            Status = (int)SaleStatus.InProgress
        });

        var now = DateTime.UtcNow;
        return results.Select(r => new PendingSaleSummaryDto(
            r.Id,
            r.SaleNumber,
            r.SaleDate,
            r.Total,
            r.ItemCount,
            now - r.SaleDate
        ));
    }

    // Helper classes for Dapper mapping
    private class SaleQueryResult
    {
        public Guid Id { get; set; }
        public int SaleNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public int Status { get; set; }
        public string? CustomerDocument { get; set; }
    }

    private class SaleSummaryQueryResult
    {
        public Guid Id { get; set; }
        public int SaleNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public int Status { get; set; }
        public int ItemCount { get; set; }
        public string? CustomerDocument { get; set; }
    }

    private class SalesSummaryQueryResult
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageTicket { get; set; }
        public int CompletedSales { get; set; }
        public int CancelledSales { get; set; }
    }

    private class SaleItemQueryResult
    {
        public object Id { get; set; } = null!;
        public object ProductId { get; set; } = null!;
        public string Barcode { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }

        public Guid GetId() => ParseGuid(Id);
        public Guid GetProductId() => ParseGuid(ProductId);

        private static Guid ParseGuid(object value) => value switch
        {
            string s => Guid.Parse(s),
            byte[] b => new Guid(b),
            Guid g => g,
            _ => Guid.Empty
        };
    }

    private class PaymentQueryResult
    {
        public object Id { get; set; } = null!;
        public int Method { get; set; }
        public decimal Amount { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? CardBrand { get; set; }
        public DateTime PaymentDate { get; set; }

        public Guid GetId() => Id switch
        {
            string s => Guid.Parse(s),
            byte[] b => new Guid(b),
            Guid g => g,
            _ => Guid.Empty
        };
    }

    private class PendingSaleQueryResult
    {
        public Guid Id { get; set; }
        public int SaleNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }
}
