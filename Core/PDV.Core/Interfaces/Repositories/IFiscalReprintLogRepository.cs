using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Repositories;

public interface IFiscalReprintLogRepository : IRepository<FiscalReprintLog>
{
    Task<IEnumerable<FiscalReprintLog>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<int> GetReprintCountAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FiscalReprintLog>> GetByOperatorIdAsync(Guid operatorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FiscalReprintLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
