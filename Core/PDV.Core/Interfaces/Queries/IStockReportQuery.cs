using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Queries;

public interface IStockReportQuery
{
    Task<IEnumerable<StockPositionRow>> GetCurrentStockPositionAsync(CancellationToken ct = default);
}
