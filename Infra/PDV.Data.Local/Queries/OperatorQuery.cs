using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;

namespace PDV.Data.Local.Queries;

public class OperatorQuery(string connectionString) : IOperatorQuery
{
    private IDbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task<OperatorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, Code, IsActive, IsAdmin, LastLoginAt
            FROM Operators
            WHERE Id = @Id
            LIMIT 1";

        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<OperatorQueryResult>(
            sql, new { Id = id.ToString().ToUpperInvariant() });

        return result == null ? null : MapToDto(result);
    }

    public async Task<OperatorDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, Code, IsActive, IsAdmin, LastLoginAt
            FROM Operators
            WHERE Code = @Code AND IsActive = 1
            LIMIT 1";

        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<OperatorQueryResult>(
            sql, new { Code = code.ToUpperInvariant() });

        return result == null ? null : MapToDto(result);
    }

    public async Task<IEnumerable<OperatorDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, Code, IsActive, IsAdmin, LastLoginAt
            FROM Operators
            WHERE IsActive = 1
            ORDER BY Name";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<OperatorQueryResult>(sql);

        return results.Select(MapToDto);
    }

    public async Task<bool> ValidatePinAsync(string code, string pinHash, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Operators
            WHERE Code = @Code AND PinHash = @PinHash AND IsActive = 1";

        using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            Code = code.ToUpperInvariant(),
            PinHash = pinHash
        });

        return count > 0;
    }

    private static OperatorDto MapToDto(OperatorQueryResult result)
    {
        var id = result.Id switch
        {
            string s => Guid.Parse(s),
            byte[] b => new Guid(b),
            Guid g => g,
            _ => Guid.Empty
        };

        return new OperatorDto(
            id,
            result.Name,
            result.Code,
            result.IsActive,
            result.IsAdmin,
            result.LastLoginAt
        );
    }

    private class OperatorQueryResult
    {
        public object Id { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
