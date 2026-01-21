using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Queries;

public interface ISaleQuery
{
    Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SaleSummaryDto>> GetDailySalesAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<SaleSummaryDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyTotalAsync(DateTime date, CancellationToken cancellationToken = default);
}
