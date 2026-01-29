using PDV.Shared.Enums;

namespace PDV.Shared.DTOs;

public record SaleSummaryDto(
    Guid Id,
    int SaleNumber,
    DateTime SaleDate,
    decimal Total,
    SaleStatus Status,
    int ItemCount,
    string? CustomerDocument
);

public record SalesSummaryDto(
    int TotalSales,
    decimal TotalRevenue,
    decimal AverageTicket,
    int CompletedSales,
    int CancelledSales
);
