namespace PDV.Core.Exceptions;

public class ProductNotFoundException : DomainException
{
    public string? Barcode { get; }
    public Guid? ProductId { get; }

    public ProductNotFoundException(string barcode)
        : base($"Product with barcode '{barcode}' was not found")
    {
        Barcode = barcode;
    }

    public ProductNotFoundException(Guid productId)
        : base($"Product with ID '{productId}' was not found")
    {
        ProductId = productId;
    }
}
