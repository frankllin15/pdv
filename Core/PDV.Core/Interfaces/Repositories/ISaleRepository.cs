using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Core.Interfaces.Repositories;

public interface ISaleRepository : IRepository<Sale>
{
    Task<Sale?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdWithItemsAndPaymentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetNextSaleNumberAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Sale>> GetByStatusAsync(SaleStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Sale>> GetPendingSyncAsync(CancellationToken cancellationToken = default);
}
