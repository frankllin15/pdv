namespace PDV.Shared.DTOs;

public record PendingSaleSummaryDto(
    Guid Id,
    int SaleNumber,
    DateTime SaleDate,
    decimal Total,
    int ItemCount,
    TimeSpan Age
);
