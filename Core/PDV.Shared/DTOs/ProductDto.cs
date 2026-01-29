namespace PDV.Shared.DTOs;

public record ProductDto
{
    public Guid Id { get; init; } = Guid.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string UnitOfMeasure { get; init; } = string.Empty;
    public decimal StockQuantity { get; init; }
    public string? TaxCode { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsActive { get; init; }
}

public record CreateProductRequest
{
    public string Barcode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public decimal UnitPrice { get; init; }
    public string UnitOfMeasure { get; init; } = "UN";
    public decimal StockQuantity { get; init; }
    public string? TaxCode { get; init; }
    public decimal TaxRate { get; init; }
}

public record UpdateProductRequest
{
    public string Barcode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public decimal UnitPrice { get; init; }
    public string UnitOfMeasure { get; init; } = "UN";
    public decimal StockQuantity { get; init; }
    public string? TaxCode { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsActive { get; init; } = true;
}