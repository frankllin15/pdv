using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Repositories;

public interface IOperatorRepository : IRepository<Operator>
{
    Task<Operator?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
