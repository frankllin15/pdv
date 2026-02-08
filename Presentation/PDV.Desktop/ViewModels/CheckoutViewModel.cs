using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Desktop.Services;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Desktop.ViewModels;

public partial class CheckoutViewModel : ViewModelBase
{
    private readonly IProductQuery _productQuery;
    private readonly ISaleQuery _saleQuery;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorSessionService _operatorSession;
    private readonly IFiscalManager? _fiscalManager;
    private readonly IReceiptPrinterService? _printerService;
    private readonly IFiscalConfigurationRepository? _fiscalConfigRepository;
    private readonly IFiscalTransactionRepository? _fiscalTransactionRepository;
    private readonly BackgroundSyncService? _backgroundSyncService;

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

    [ObservableProperty]
    private bool _showPendingSalesDialog;

    [ObservableProperty]
    private bool _showCancelConfirmDialog;

    [ObservableProperty]
    private bool _showNewSaleConfirmDialog;

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _showProductSearch;

    [ObservableProperty]
    private string _productSearchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private int _searchCurrentPage = 1;

    [ObservableProperty]
    private int _searchTotalPages;

    [ObservableProperty]
    private int _searchTotalCount;

    [ObservableProperty]
    private bool _hasNextPage;

    [ObservableProperty]
    private bool _hasPreviousPage;

    [ObservableProperty]
    private bool _showReceiptPreview;

    [ObservableProperty]
    private string _receiptContent = string.Empty;

    [ObservableProperty]
    private string _receiptFiscalNumber = string.Empty;

    [ObservableProperty]
    private bool _receiptIsContingency;

    public ObservableCollection<SaleItemViewModel> Items { get; } = new();

    public ObservableCollection<PendingSaleSummaryDto> PendingSales { get; } = new();

    public ObservableCollection<ProductDto> ProductSearchResults { get; } = new();

    /// <summary>
    /// Indicates if there's a sale in progress with items
    /// </summary>
    public bool HasPendingSale => _currentSale != null && Items.Count > 0;

    public CheckoutViewModel(
        IProductQuery productQuery,
        ISaleQuery saleQuery,
        IUnitOfWork unitOfWork,
        IOperatorSessionService operatorSession,
        IFiscalManager? fiscalManager = null,
        IReceiptPrinterService? printerService = null,
        IFiscalConfigurationRepository? fiscalConfigRepository = null,
        IFiscalTransactionRepository? fiscalTransactionRepository = null,
        BackgroundSyncService? backgroundSyncService = null)
    {
        _productQuery = productQuery;
        _saleQuery = saleQuery;
        _unitOfWork = unitOfWork;
        _operatorSession = operatorSession;
        _fiscalManager = fiscalManager;
        _printerService = printerService;
        _fiscalConfigRepository = fiscalConfigRepository;
        _fiscalTransactionRepository = fiscalTransactionRepository;
        _backgroundSyncService = backgroundSyncService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            if (_operatorSession.CurrentOperator == null)
            {
                SetStatus("No operator logged in", true);
                IsInitialized = true;
                return;
            }

            var pendingSales = await _saleQuery.GetPendingSalesByOperatorAsync(
                _operatorSession.CurrentOperator.Id);

            PendingSales.Clear();
            foreach (var sale in pendingSales)
            {
                PendingSales.Add(sale);
            }

            if (PendingSales.Count > 0)
            {
                ShowPendingSalesDialog = true;
                SetStatus($"You have {PendingSales.Count} pending sale(s)", false);
            }
            else
            {
                SetStatus("Ready - scan a product to start", false);
            }

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            SetStatus($"Error initializing: {ex.Message}", true);
            IsInitialized = true;
        }
    }

    [RelayCommand]
    private async Task ResumeSaleAsync(PendingSaleSummaryDto? pendingSale)
    {
        if (pendingSale == null) return;

        try
        {
            var sale = await _unitOfWork.Sales.GetByIdWithItemsAsync(pendingSale.Id);
            if (sale == null)
            {
                SetStatus("Sale not found", true);
                return;
            }

            _currentSale = sale;
            SaleNumber = sale.SaleNumber;
            RefreshItemsFromSale();
            ShowPendingSalesDialog = false;
            SetStatus($"Resumed sale #{sale.SaleNumber}", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error resuming sale: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task DiscardPendingSalesAsync()
    {
        if (_operatorSession.CurrentOperator == null) return;

        try
        {
            var count = await _unitOfWork.Sales.CancelPendingSalesByOperatorAsync(
                _operatorSession.CurrentOperator.Id);
            await _unitOfWork.SaveChangesAsync();

            PendingSales.Clear();
            ShowPendingSalesDialog = false;
            SetStatus($"Discarded {count} pending sale(s). Scan a product to start.", false);
            ResetCheckoutState();
        }
        catch (Exception ex)
        {
            SetStatus($"Error discarding sales: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void StartNewKeepPending()
    {
        ShowPendingSalesDialog = false;
        SetStatus("Ready - scan a product to start new sale", false);
    }

    [RelayCommand]
    private async Task OpenProductSearchAsync()
    {
        ShowProductSearch = true;
        ProductSearchText = string.Empty;
        SearchCurrentPage = 1;
        await LoadProductPageAsync();
    }

    [RelayCommand]
    private void CloseProductSearch()
    {
        ShowProductSearch = false;
        ProductSearchText = string.Empty;
        ProductSearchResults.Clear();
    }

    [RelayCommand]
    private async Task SearchProductsByDescriptionAsync()
    {
        SearchCurrentPage = 1;
        await LoadProductPageAsync();
    }

    [RelayCommand]
    private async Task ProductSearchNextPageAsync()
    {
        if (!HasNextPage) return;
        SearchCurrentPage++;
        await LoadProductPageAsync();
    }

    [RelayCommand]
    private async Task ProductSearchPreviousPageAsync()
    {
        if (!HasPreviousPage) return;
        SearchCurrentPage--;
        await LoadProductPageAsync();
    }

    private async Task LoadProductPageAsync()
    {
        try
        {
            IsSearching = true;
            var searchTerm = string.IsNullOrWhiteSpace(ProductSearchText) ? null : ProductSearchText.Trim();
            var result = await _productQuery.GetActivePagedAsync(searchTerm, SearchCurrentPage);

            ProductSearchResults.Clear();
            foreach (var product in result.Items)
            {
                ProductSearchResults.Add(product);
            }

            SearchTotalPages = result.TotalPages;
            SearchTotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;
            HasPreviousPage = result.HasPreviousPage;
        }
        catch (Exception ex)
        {
            SetStatus($"Search error: {ex.Message}", true);
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task AddProductFromSearchAsync(ProductDto? product)
    {
        if (product == null) return;

        if (_currentSale == null)
        {
            await StartNewSaleAsync();
        }

        await AddProductToSaleAsync(product);
        CloseProductSearch();
    }

    [RelayCommand]
    private async Task StartNewSaleAsync()
    {
        try
        {
            var operatorId = _operatorSession.CurrentOperator?.Id;
            var nextNumber = await _unitOfWork.Sales.GetNextSaleNumberAsync();
            _currentSale = new Sale(nextNumber, operatorId);
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

    private void ResetCheckoutState()
    {
        _currentSale = null;
        SaleNumber = 0;
        Items.Clear();
        UpdateTotals();
        IsPaymentMode = false;
        AmountPaid = 0;
        Change = 0;
        BarcodeInput = string.Empty;
        SetStatus("Ready - scan a product to start", false);
    }

    [RelayCommand]
    private async Task SearchProductAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput))
            return;

        var barcode = BarcodeInput.Trim();

        if (_currentSale == null)
        {
            await StartNewSaleAsync();
        }

        try
        {
            var product = await _productQuery.GetByBarcodeAsync(barcode);
            if (product == null)
            {
                SetStatus($"Product not found: {barcode}", true);
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

                // Process fiscal (NFC-e) after sale completion
                await ProcessFiscalAsync();

                // Trigger cloud sync in background (fire-and-forget)
                _ = _backgroundSyncService?.SyncNowAsync();

                // Reset state - new sale will be created when first item is added
                if (!ShowReceiptPreview)
                {
                    ResetCheckoutState();
                }
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
    private void RequestCancelSale()
    {
        if (_currentSale == null || Items.Count == 0)
        {
            // No sale or empty sale, no need for confirmation
            return;
        }

        ShowCancelConfirmDialog = true;
    }

    [RelayCommand]
    private async Task ConfirmCancelSaleAsync()
    {
        ShowCancelConfirmDialog = false;

        if (_currentSale == null)
        {
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

            _currentSale = null;
            SaleNumber = 0;
            Items.Clear();
            UpdateTotals();
            IsPaymentMode = false;
            SetStatus("Sale cancelled", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error cancelling: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void DismissCancelDialog()
    {
        ShowCancelConfirmDialog = false;
    }

    [RelayCommand]
    private void RequestNewSale()
    {
        if (_currentSale != null && Items.Count > 0)
        {
            // Has items, show confirmation
            ShowNewSaleConfirmDialog = true;
            return;
        }

        // No current sale or empty, just reset
        ResetCheckoutState();
    }

    [RelayCommand]
    private async Task ConfirmNewSaleAsync()
    {
        ShowNewSaleConfirmDialog = false;

        // Cancel current sale first if exists
        if (_currentSale != null)
        {
            try
            {
                var sale = await _unitOfWork.Sales.GetByIdAsync(_currentSale.Id);
                if (sale != null && sale.Status == SaleStatus.InProgress)
                {
                    sale.Cancel();
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error cancelling current sale: {ex.Message}", true);
                return;
            }
        }

        ResetCheckoutState();
    }

    [RelayCommand]
    private void DismissNewSaleDialog()
    {
        ShowNewSaleConfirmDialog = false;
    }

    [RelayCommand]
    private void ExitPaymentMode()
    {
        IsPaymentMode = false;
        SetStatus("Returned to sale", false);
    }

    private async Task ProcessFiscalAsync()
    {
        if (_fiscalManager == null || _currentSale == null) return;

        try
        {
            if (!await _fiscalManager.IsConfiguredAsync()) return;

            var result = await _fiscalManager.IssueNfceAsync(_currentSale);

            if (result.Success)
            {
                var msg = result.IsContingency
                    ? $"NFC-e {_currentSale.FiscalNumber} em contingencia"
                    : $"NFC-e {_currentSale.FiscalNumber} autorizada";
                SetStatus(msg, false);

                // Generate and show receipt
                await GenerateAndShowReceiptAsync(result.IsContingency);
            }
            else
            {
                SetStatus($"Erro fiscal: {result.StatusMessage}", true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao processar fiscal: {ex.Message}", true);
        }
    }

    private async Task GenerateAndShowReceiptAsync(bool isContingency)
    {
        if (_currentSale == null || _fiscalTransactionRepository == null || _fiscalConfigRepository == null)
            return;

        try
        {
            var transaction = await _fiscalTransactionRepository.GetBySaleIdAsync(_currentSale.Id);
            var config = await _fiscalConfigRepository.GetActiveAsync();

            if (transaction == null || config == null)
                return;

            // Generate DANFE content using FiscalManager
            var danfeContent = await _fiscalManager!.GenerateDanfeAsync(transaction);

            // Set receipt data for preview
            ReceiptContent = danfeContent;
            ReceiptFiscalNumber = _currentSale.FiscalNumber?.ToString() ?? transaction.Number.ToString();
            ReceiptIsContingency = isContingency;

            // Show preview
            ShowReceiptPreview = true;
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao gerar recibo: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (string.IsNullOrEmpty(ReceiptContent) || _printerService == null)
            return;

        try
        {
            SetStatus("Imprimindo...", false);
            var success = await _printerService.PrintWithDialogAsync(ReceiptContent);

            if (success)
            {
                SetStatus("Impressao enviada!", false);
            }
            else
            {
                SetStatus("Falha ao imprimir", true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao imprimir: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task PrintReceiptAndCloseAsync()
    {
        await PrintReceiptAsync();
        await CloseReceiptPreviewAsync();
    }

    [RelayCommand]
    private Task CloseReceiptPreviewAsync()
    {
        ShowReceiptPreview = false;
        ReceiptContent = string.Empty;

        // Reset state - new sale will be created when first item is added
        ResetCheckoutState();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveReceiptToFileAsync()
    {
        if (string.IsNullOrEmpty(ReceiptContent))
            return;

        try
        {
            var fileName = $"NFC-e_{ReceiptFiscalNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, "PDV", "Recibos", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, ReceiptContent);

            SetStatus($"Salvo em: {filePath}", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao salvar: {ex.Message}", true);
        }
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
