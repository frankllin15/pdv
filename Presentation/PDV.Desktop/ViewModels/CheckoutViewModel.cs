using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Desktop.ViewModels;

public partial class CheckoutViewModel : ViewModelBase
{
    private readonly IProductQuery _productQuery;
    private readonly IUnitOfWork _unitOfWork;

    private Sale? _currentSale;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _discount;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private int _saleNumber;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private bool _isPaymentMode;

    [ObservableProperty]
    private decimal _amountPaid;

    [ObservableProperty]
    private decimal _change;

    public ObservableCollection<SaleItemViewModel> Items { get; } = new();

    public CheckoutViewModel(IProductQuery productQuery, IUnitOfWork unitOfWork)
    {
        _productQuery = productQuery;
        _unitOfWork = unitOfWork;
    }

    [RelayCommand]
    private async Task StartNewSaleAsync()
    {
        try
        {
            var nextNumber = await _unitOfWork.Sales.GetNextSaleNumberAsync();
            _currentSale = new Sale(nextNumber);
            await _unitOfWork.Sales.AddAsync(_currentSale);
            await _unitOfWork.SaveChangesAsync();

            SaleNumber = nextNumber;
            Items.Clear();
            UpdateTotals();
            IsPaymentMode = false;
            AmountPaid = 0;
            Change = 0;
            BarcodeInput = string.Empty;
            SetStatus("New sale started", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error starting sale: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task SearchProductAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput))
            return;

        if (_currentSale == null)
        {
            await StartNewSaleAsync();
        }

        try
        {
            var product = await _productQuery.GetByBarcodeAsync(BarcodeInput.Trim());
            if (product == null)
            {
                SetStatus($"Product not found: {BarcodeInput}", true);
                return;
            }

            await AddProductToSaleAsync(product);
            BarcodeInput = string.Empty;
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", true);
        }
    }

    private async Task AddProductToSaleAsync(ProductDto productDto)
    {
        if (_currentSale == null) return;

        var productEntity = await _unitOfWork.Products.GetByIdAsync(productDto.Id);
        if (productEntity == null)
        {
            SetStatus("Product not found in database", true);
            return;
        }

        var saleWithItems = await _unitOfWork.Sales.GetByIdWithItemsAsync(_currentSale.Id);
        if (saleWithItems == null) return;

        _currentSale = saleWithItems;
        _currentSale.AddItem(productEntity, 1);
        await _unitOfWork.SaveChangesAsync();

        RefreshItemsFromSale();
        SetStatus($"Added: {productDto.ShortDescription}", false);
    }

    private void RefreshItemsFromSale()
    {
        if (_currentSale == null) return;

        Items.Clear();
        var sequence = 1;
        foreach (var item in _currentSale.Items)
        {
            Items.Add(new SaleItemViewModel
            {
                Id = item.Id,
                Sequence = sequence++,
                Barcode = item.Barcode,
                Description = item.ProductDescription,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.Total
            });
        }

        UpdateTotals();
    }

    [RelayCommand]
    private async Task RemoveSelectedItemAsync(SaleItemViewModel? item)
    {
        if (item == null || _currentSale == null) return;

        try
        {
            var saleWithItems = await _unitOfWork.Sales.GetByIdWithItemsAsync(_currentSale.Id);
            if (saleWithItems == null) return;

            _currentSale = saleWithItems;
            _currentSale.RemoveItem(item.Id);
            await _unitOfWork.SaveChangesAsync();

            RefreshItemsFromSale();
            SetStatus("Item removed", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error removing item: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task UpdateItemQuantityAsync(SaleItemViewModel? item)
    {
        if (item == null || _currentSale == null || item.Quantity <= 0) return;

        try
        {
            var saleWithItems = await _unitOfWork.Sales.GetByIdWithItemsAsync(_currentSale.Id);
            if (saleWithItems == null) return;

            _currentSale = saleWithItems;
            _currentSale.UpdateItemQuantity(item.Id, item.Quantity);
            await _unitOfWork.SaveChangesAsync();

            RefreshItemsFromSale();
            SetStatus("Quantity updated", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error updating quantity: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void EnterPaymentMode()
    {
        if (_currentSale == null || !Items.Any())
        {
            SetStatus("No items in sale", true);
            return;
        }

        IsPaymentMode = true;
        AmountPaid = 0;
        Change = 0;
        SetStatus("Select payment method", false);
    }

    [RelayCommand]
    private async Task ProcessPaymentAsync(string paymentMethodStr)
    {
        if (_currentSale == null) return;

        if (!Enum.TryParse<PaymentMethod>(paymentMethodStr, out var paymentMethod))
        {
            SetStatus("Invalid payment method", true);
            return;
        }

        try
        {
            var saleWithItems = await _unitOfWork.Sales.GetByIdWithItemsAndPaymentsAsync(_currentSale.Id);
            if (saleWithItems == null) return;

            _currentSale = saleWithItems;

            var amountToPay = AmountPaid > 0 ? AmountPaid : Total;
            string? authCode = null;

            // Simulate card authorization
            if (paymentMethod is PaymentMethod.CreditCard or PaymentMethod.DebitCard)
            {
                authCode = GenerateAuthCode();
            }

            _currentSale.AddPayment(paymentMethod, amountToPay, authCode);

            if (_currentSale.GetRemainingAmount() <= 0)
            {
                _currentSale.Complete();
                Change = _currentSale.GetChange();
            }

            await _unitOfWork.SaveChangesAsync();

            if (_currentSale.Status == SaleStatus.Completed)
            {
                var changeMessage = Change > 0 ? $" - Change: {Change:C}" : "";
                SetStatus($"Sale #{SaleNumber} completed!{changeMessage}", false);
                IsPaymentMode = false;

                // Auto-start new sale after a brief moment
                await Task.Delay(2000);
                await StartNewSaleAsync();
            }
            else
            {
                SetStatus($"Remaining: {_currentSale.GetRemainingAmount():C}", false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Payment error: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task CancelSaleAsync()
    {
        if (_currentSale == null)
        {
            await StartNewSaleAsync();
            return;
        }

        try
        {
            var sale = await _unitOfWork.Sales.GetByIdAsync(_currentSale.Id);
            if (sale != null && sale.Status == SaleStatus.InProgress)
            {
                sale.Cancel();
                await _unitOfWork.SaveChangesAsync();
            }

            SetStatus("Sale cancelled", false);
            await StartNewSaleAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Error cancelling: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void ExitPaymentMode()
    {
        IsPaymentMode = false;
        SetStatus("Returned to sale", false);
    }

    private void UpdateTotals()
    {
        if (_currentSale != null)
        {
            Subtotal = _currentSale.Subtotal;
            Discount = _currentSale.Discount;
            Total = _currentSale.Total;
            ItemCount = _currentSale.Items.Count;
        }
        else
        {
            Subtotal = 0;
            Discount = 0;
            Total = 0;
            ItemCount = 0;
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private static string GenerateAuthCode()
    {
        return $"NSU{DateTime.Now:HHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}

public partial class SaleItemViewModel : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty]
    private int _sequence;

    [ObservableProperty]
    private string _barcode = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _total;
}
