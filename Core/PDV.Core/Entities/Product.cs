using PDV.Core.Entities.Base;
using PDV.Shared.Enums;

namespace PDV.Core.Entities;

public class Product : Entity
{
    public string Barcode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public string UnitOfMeasure { get; private set; } = "UN";
    public decimal StockQuantity { get; private set; }
    public string? TaxCode { get; private set; } // NCM
    public decimal TaxRate { get; private set; } // ICMS
    public string? Cfop { get; private set; }    // Código Fiscal de Operação (e.g., "5102")
    public string? Cest { get; private set; }    // Código Especificador da Substituição Tributária
    public TaxOrigin TaxOrigin { get; private set; } = TaxOrigin.National; // Origem da mercadoria
    public string? Cst { get; private set; }     // Código de Situação Tributária ICMS
    public bool IsActive { get; private set; } = true;
    public SyncState SyncState { get; private set; } = SyncState.Pending;

    private Product() { }

    public Product(
        string barcode,
        string description,
        decimal unitPrice,
        string? shortDescription = null,
        string unitOfMeasure = "UN",
        decimal stockQuantity = 0,
        string? taxCode = null,
        decimal taxRate = 0,
        string? cfop = null,
        string? cest = null,
        TaxOrigin taxOrigin = TaxOrigin.National,
        string? cst = null)
    {
        SetBarcode(barcode);
        SetDescription(description);
        ShortDescription = shortDescription ?? description;
        SetUnitPrice(unitPrice);
        UnitOfMeasure = unitOfMeasure;
        StockQuantity = stockQuantity;
        TaxCode = taxCode;
        TaxRate = taxRate;
        Cfop = cfop;
        Cest = cest;
        TaxOrigin = taxOrigin;
        Cst = cst;
    }

    public void SetBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode cannot be empty", nameof(barcode));

        Barcode = barcode.Trim();
        SetUpdatedAt();
    }

    public void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        Description = description.Trim();
        SetUpdatedAt();
    }

    public void SetUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        UnitPrice = unitPrice;
        SetUpdatedAt();
    }

    public void UpdateStock(decimal quantity)
    {
        StockQuantity += quantity;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void SetSyncState(SyncState state)
    {
        SyncState = state;
        SetUpdatedAt();
    }

    public void SetFiscalData(string? cfop, string? cest, TaxOrigin taxOrigin, string? cst)
    {
        Cfop = cfop;
        Cest = cest;
        TaxOrigin = taxOrigin;
        Cst = cst;
        SetUpdatedAt();
    }
}
