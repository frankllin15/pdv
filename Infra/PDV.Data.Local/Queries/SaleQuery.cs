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

        var sale = await connection.QueryFirstOrDefaultAsync<SaleQueryResult>(saleSql, new { Id = id.ToString() });
        if (sale == null) return null;

        var items = (await connection.QueryAsync<SaleItemDto>(itemsSql, new { SaleId = id.ToString() })).ToList();
        var payments = (await connection.QueryAsync<PaymentDto>(paymentsSql, new { SaleId = id.ToString() })).ToList();

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
}
