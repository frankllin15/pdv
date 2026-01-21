namespace PDV.Shared.DTOs;

public record ProductDto(
    Guid Id,
    string Barcode,
    string Description,
    string ShortDescription,
    decimal UnitPrice,
    string UnitOfMeasure,
    decimal StockQuantity,
    string? TaxCode,
    decimal TaxRate,
    bool IsActive
);
