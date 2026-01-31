using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Queries;

public interface IOperatorQuery
{
    Task<OperatorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperatorDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<OperatorDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidatePinAsync(string code, string pinHash, CancellationToken cancellationToken = default);
}
