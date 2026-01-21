using PDV.Core.Entities.Base;

namespace PDV.Core.Entities;

public class SaleItem : Entity
{
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Barcode { get; private set; } = string.Empty;
    public string ProductDescription { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }

    // Navigation
    public Sale? Sale { get; private set; }

    private SaleItem() { }

    public SaleItem(
        Guid saleId,
        Guid productId,
        string barcode,
        string productDescription,
        decimal quantity,
        decimal unitPrice,
        decimal discount = 0)
    {
        SaleId = saleId;
        ProductId = productId;
        Barcode = barcode;
        ProductDescription = productDescription;
        SetQuantity(quantity);
        UnitPrice = unitPrice;
        Discount = discount;
        CalculateTotal();
    }

    public void SetQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        Quantity = quantity;
        CalculateTotal();
        SetUpdatedAt();
    }

    public void SetDiscount(decimal discount)
    {
        if (discount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(discount));

        Discount = discount;
        CalculateTotal();
        SetUpdatedAt();
    }

    private void CalculateTotal()
    {
        Total = (Quantity * UnitPrice) - Discount;
        if (Total < 0) Total = 0;
    }
}
