using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Repositories;

public interface ICashSessionRepository : IRepository<CashSession>
{
    Task<CashSession?> GetOpenSessionByTerminalAsync(string terminalId, CancellationToken cancellationToken = default);
    Task<CashSession?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
}
