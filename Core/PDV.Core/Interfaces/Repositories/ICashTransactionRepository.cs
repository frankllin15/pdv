using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Repositories;

public interface ICashTransactionRepository : IRepository<CashTransaction>
{
    Task<IEnumerable<CashTransaction>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
