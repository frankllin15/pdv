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
