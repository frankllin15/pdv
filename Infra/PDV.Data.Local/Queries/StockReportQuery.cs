using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;

namespace PDV.Data.Local.Queries;

public class StockReportQuery(string connectionString) : IStockReportQuery
{
    private IDbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task<IEnumerable<StockPositionRow>> GetCurrentStockPositionAsync(
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                Barcode,
                Description,
                UnitOfMeasure,
                UnitPrice,
                StockQuantity
            FROM Products
            WHERE IsActive = 1
            ORDER BY Description";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<StockQueryResult>(sql);

        return results.Select(r => new StockPositionRow(
            r.Barcode,
            r.Description,
            r.UnitOfMeasure,
            r.UnitPrice,
            r.StockQuantity,
            r.UnitPrice * r.StockQuantity
        ));
    }

    private class StockQueryResult
    {
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal StockQuantity { get; set; }
    }
}
