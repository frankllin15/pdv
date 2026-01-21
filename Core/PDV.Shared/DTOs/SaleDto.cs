using PDV.Shared.Enums;

namespace PDV.Shared.DTOs;

public record SaleDto(
    Guid Id,
    int SaleNumber,
    DateTime SaleDate,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    SaleStatus Status,
    string? CustomerDocument,
    IReadOnlyList<SaleItemDto> Items,
    IReadOnlyList<PaymentDto> Payments
);
