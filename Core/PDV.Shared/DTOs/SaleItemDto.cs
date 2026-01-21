namespace PDV.Shared.DTOs;

public record SaleItemDto(
    Guid Id,
    Guid ProductId,
    string Barcode,
    string ProductDescription,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total
);
