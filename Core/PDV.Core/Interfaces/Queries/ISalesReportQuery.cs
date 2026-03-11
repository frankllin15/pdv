using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Queries;

public interface ISalesReportQuery
{
    Task<IEnumerable<SalesByPeriodRow>> GetSalesByPeriodAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default);

    Task<IEnumerable<RevenueByPaymentMethodRow>> GetRevenueByPaymentMethodAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
