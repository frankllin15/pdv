using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Core.Interfaces.Repositories;

public interface IFiscalTransactionRepository : IRepository<FiscalTransaction>
{
    Task<IEnumerable<FiscalTransaction>> GetPendingContingencyAsync(CancellationToken cancellationToken = default);
    Task<FiscalTransaction?> GetBySaleIdAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<FiscalTransaction?> GetByAccessKeyAsync(string accessKey, CancellationToken cancellationToken = default);
    Task<int> GetNextNumberAsync(int series, CancellationToken cancellationToken = default);
    Task<IEnumerable<FiscalTransaction>> GetByStatusAsync(FiscalStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<FiscalTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<FiscalTransaction> Items, int TotalCount)> GetPagedAsync(DateTime startDate, DateTime endDate, FiscalStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
