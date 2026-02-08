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

    public async Task<PagedResult<ProductDto>> GetActivePagedAsync(string? searchTerm = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
        var whereClause = hasSearch
            ? "WHERE IsActive = 1 AND (Barcode LIKE @SearchTerm OR Description LIKE @SearchTerm)"
            : "WHERE IsActive = 1";

        var countSql = $"SELECT COUNT(*) FROM Products {whereClause}";
        var dataSql = $@"
            SELECT Id, Barcode, Description, ShortDescription, UnitPrice,
                   UnitOfMeasure, StockQuantity, TaxCode, TaxRate, IsActive
            FROM Products
            {whereClause}
            ORDER BY Description
            LIMIT @PageSize OFFSET @Offset";

        var parameters = new
        {
            SearchTerm = hasSearch ? $"%{searchTerm!.Trim()}%" : null,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        };

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<ProductDto>(dataSql, parameters)).ToList();

        return new PagedResult<ProductDto>(items, totalCount, page, pageSize);
    }
}
