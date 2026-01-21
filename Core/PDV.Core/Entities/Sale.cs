using PDV.Core.Entities.Base;
using PDV.Shared.Enums;

namespace PDV.Core.Entities;

public class Sale : Entity
{
    public int SaleNumber { get; private set; }
    public DateTime SaleDate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }
    public SaleStatus Status { get; private set; }
    public string? CustomerDocument { get; private set; } // CPF
    public Guid? OperatorId { get; private set; }
    public SyncState SyncState { get; private set; } = SyncState.Pending;

    private readonly List<SaleItem> _items = new();
    private readonly List<Payment> _payments = new();

    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Sale() { }

    public Sale(int saleNumber, Guid? operatorId = null)
    {
        SaleNumber = saleNumber;
        SaleDate = DateTime.UtcNow;
        Status = SaleStatus.InProgress;
        OperatorId = operatorId;
    }

    public SaleItem AddItem(Product product, decimal quantity, decimal discount = 0)
    {
        if (Status != SaleStatus.InProgress)
            throw new InvalidOperationException("Cannot add items to a completed or cancelled sale");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.SetQuantity(existingItem.Quantity + quantity);
            CalculateTotals();
            return existingItem;
        }

        var item = new SaleItem(
            Id,
            product.Id,
            product.Barcode,
            product.Description,
            quantity,
            product.UnitPrice,
            discount);

        _items.Add(item);
        CalculateTotals();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != SaleStatus.InProgress)
            throw new InvalidOperationException("Cannot remove items from a completed or cancelled sale");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            CalculateTotals();
        }
    }

    public void UpdateItemQuantity(Guid itemId, decimal newQuantity)
    {
        if (Status != SaleStatus.InProgress)
            throw new InvalidOperationException("Cannot update items in a completed or cancelled sale");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.SetQuantity(newQuantity);
            CalculateTotals();
        }
    }

    public Payment AddPayment(PaymentMethod method, decimal amount, string? authorizationCode = null, string? cardBrand = null)
    {
        if (Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("Cannot add payments to a cancelled sale");

        var payment = new Payment(Id, method, amount, authorizationCode, cardBrand);
        _payments.Add(payment);
        SetUpdatedAt();
        return payment;
    }

    public void SetCustomerDocument(string? document)
    {
        CustomerDocument = document;
        SetUpdatedAt();
    }

    public void SetDiscount(decimal discount)
    {
        if (discount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(discount));

        Discount = discount;
        CalculateTotals();
    }

    public void Complete()
    {
        if (Status != SaleStatus.InProgress)
            throw new InvalidOperationException("Only in-progress sales can be completed");

        if (!_items.Any())
            throw new InvalidOperationException("Cannot complete a sale without items");

        var totalPaid = _payments.Sum(p => p.Amount);
        if (totalPaid < Total)
            throw new InvalidOperationException($"Payment insufficient. Total: {Total:C}, Paid: {totalPaid:C}");

        Status = SaleStatus.Completed;
        SetUpdatedAt();
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed sale");

        Status = SaleStatus.Cancelled;
        SetUpdatedAt();
    }

    public decimal GetChange()
    {
        var totalPaid = _payments.Sum(p => p.Amount);
        return totalPaid > Total ? totalPaid - Total : 0;
    }

    public decimal GetRemainingAmount()
    {
        var totalPaid = _payments.Sum(p => p.Amount);
        return Total > totalPaid ? Total - totalPaid : 0;
    }

    public void SetSyncState(SyncState state)
    {
        SyncState = state;
        SetUpdatedAt();
    }

    private void CalculateTotals()
    {
        Subtotal = _items.Sum(i => i.Quantity * i.UnitPrice);
        Total = Subtotal - Discount - _items.Sum(i => i.Discount);
        if (Total < 0) Total = 0;
        SetUpdatedAt();
    }

    // For EF Core to load items
    public void LoadItems(IEnumerable<SaleItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    // For EF Core to load payments
    public void LoadPayments(IEnumerable<Payment> payments)
    {
        _payments.Clear();
        _payments.AddRange(payments);
    }
}
