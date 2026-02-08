using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Core.Interfaces.Queries;

public interface ISaleQuery
{
    Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SaleSummaryDto>> GetDailySalesAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<SaleSummaryDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<SaleSummaryDto>> GetByDateRangeAndStatusAsync(DateTime startDate, DateTime endDate, SaleStatus? status = null, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyTotalAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<SalesSummaryDto> GetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<PendingSaleSummaryDto>> GetPendingSalesByOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default);
    Task<PagedResult<SaleSummaryDto>> GetPagedAsync(DateTime startDate, DateTime endDate, SaleStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
