using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;

namespace PDV.Data.Local.Queries;

public class ProductQuery : IProductQuery
{
    private readonly string _connectionString;

    public ProductQuery(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Barcode, Description, ShortDescription, UnitPrice,
                   UnitOfMeasure, StockQuantity, TaxCode, TaxRate, IsActive
            FROM Products
            WHERE Barcode = @Barcode AND IsActive = 1
            LIMIT 1";

        using var connection = CreateConnection(); 
        return await connection.QueryFirstOrDefaultAsync<ProductDto>(sql, new { Barcode = barcode });
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Barcode, Description, ShortDescription, UnitPrice,
                   UnitOfMeasure, StockQuantity, TaxCode, TaxRate, IsActive
            FROM Products
            WHERE Id = @Id
            LIMIT 1";

        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProductDto>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(string searchTerm, int limit = 10, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Barcode, Description, ShortDescription, UnitPrice,
                   UnitOfMeasure, StockQuantity, TaxCode, TaxRate, IsActive
            FROM Products
            WHERE IsActive = 1
              AND (Barcode LIKE @SearchTerm OR Description LIKE @SearchTerm)
            ORDER BY Description
            LIMIT @Limit";

        using var connection = CreateConnection();
        return await connection.QueryAsync<ProductDto>(sql, new
        {
            SearchTerm = $"%{searchTerm}%",
            Limit = limit
        });
    }

    public async Task<IEnumerable<ProductDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Barcode, Description, ShortDescription, UnitPrice,
                   UnitOfMeasure, StockQuantity, TaxCode, TaxRate, IsActive
            FROM Products
            WHERE IsActive = 1
            ORDER BY Description";

        using var connection = CreateConnection();
        return await connection.QueryAsync<ProductDto>(sql);
    }
}
