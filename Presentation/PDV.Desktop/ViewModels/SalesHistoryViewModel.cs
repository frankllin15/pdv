using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Desktop.ViewModels;

public partial class SalesHistoryViewModel : ViewModelBase
{
    private readonly ISaleQuery _saleQuery;

    [ObservableProperty] private DateTimeOffset? _startDate = DateTimeOffset.Now.Date;

    [ObservableProperty] private DateTimeOffset? _endDate = DateTimeOffset.Now.Date;

    [ObservableProperty] private SaleStatus? _selectedStatus;

    [ObservableProperty] private string _statusMessage = "Select a date range and click Search";

    [ObservableProperty] private bool _isError;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private SaleHistoryItemViewModel? _selectedSale;

    [ObservableProperty] private SaleDetailViewModel? _saleDetail;

    public PaginationState Pagination { get; }

    // Summary
    [ObservableProperty] private int _totalSales;

    [ObservableProperty] private decimal _totalRevenue;

    [ObservableProperty] private decimal _averageTicket;

    [ObservableProperty] private int _completedSales;

    [ObservableProperty] private int _cancelledSales;

    public ObservableCollection<SaleHistoryItemViewModel> Sales { get; } = new();

    public List<SaleStatusOption> StatusOptions { get; } = new()
    {
        new SaleStatusOption(null, "All"),
        new SaleStatusOption(SaleStatus.Completed, "Completed"),
        new SaleStatusOption(SaleStatus.Cancelled, "Cancelled"),
        new SaleStatusOption(SaleStatus.InProgress, "In Progress")
    };

    public SalesHistoryViewModel(ISaleQuery saleQuery)
    {
        _saleQuery = saleQuery;
        Pagination = new PaginationState(LoadPageAsync);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        Pagination.Reset();
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        Pagination.Reset();
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        try
        {
            IsLoading = true;
            IsError = false;
            SetStatus("Searching...", false);

            Sales.Clear();
            SaleDetail = null;

            var start = StartDate?.DateTime ?? DateTime.Today;
            var end = EndDate?.DateTime ?? DateTime.Today;

            var result = await _saleQuery.GetPagedAsync(start, end, SelectedStatus, Pagination.CurrentPage);

            foreach (var sale in result.Items)
            {
                Sales.Add(new SaleHistoryItemViewModel
                {
                    Id = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    SaleDate = sale.SaleDate,
                    Total = sale.Total,
                    Status = sale.Status,
                    ItemCount = sale.ItemCount,
                    CustomerDocument = sale.CustomerDocument
                });
            }

            Pagination.Update(result);

            // Load summary
            var summary = await _saleQuery.GetSummaryAsync(start, end);
            TotalSales = summary.TotalSales;
            TotalRevenue = summary.TotalRevenue;
            AverageTicket = summary.AverageTicket;
            CompletedSales = summary.CompletedSales;
            CancelledSales = summary.CancelledSales;

            SetStatus($"{Pagination.TotalCount} sale(s) found", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadSaleDetailAsync(SaleHistoryItemViewModel? sale)
    {
        if (sale == null) return;

        try
        {
            var saleDto = await _saleQuery.GetByIdAsync(sale.Id);
            if (saleDto == null)
            {
                SetStatus("Sale not found", true);
                return;
            }

            SaleDetail = new SaleDetailViewModel
            {
                SaleNumber = saleDto.SaleNumber,
                SaleDate = saleDto.SaleDate,
                Subtotal = saleDto.Subtotal,
                Discount = saleDto.Discount,
                Total = saleDto.Total,
                Status = saleDto.Status,
                CustomerDocument = saleDto.CustomerDocument
            };

            SaleDetail.Items.Clear();
            foreach (var item in saleDto.Items)
            {
                SaleDetail.Items.Add(new SaleDetailItemViewModel
                {
                    Barcode = item.Barcode,
                    Description = item.ProductDescription,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Discount = item.Discount,
                    Total = item.Total
                });
            }

            SaleDetail.Payments.Clear();
            foreach (var payment in saleDto.Payments)
            {
                SaleDetail.Payments.Add(new SaleDetailPaymentViewModel
                {
                    Method = payment.Method,
                    Amount = payment.Amount,
                    AuthorizationCode = payment.AuthorizationCode,
                    PaymentDate = payment.PaymentDate
                });
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading sale: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void SetToday()
    {
        StartDate = DateTimeOffset.Now.Date;
        EndDate = DateTimeOffset.Now.Date;
    }

    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTimeOffset.Now.Date;
        var dayOfWeek = (int)today.DayOfWeek;
        StartDate = today.AddDays(-dayOfWeek);
        EndDate = today;
    }

    [RelayCommand]
    private void SetThisMonth()
    {
        var today = DateTimeOffset.Now;
        StartDate = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset);
        EndDate = today.Date;
    }

    [RelayCommand]
    private void ClearDetail()
    {
        SaleDetail = null;
        SelectedSale = null;
    }

    partial void OnSelectedSaleChanged(SaleHistoryItemViewModel? value)
    {
        if (value != null)
        {
            LoadSaleDetailCommand.Execute(value);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}

public record SaleStatusOption(SaleStatus? Value, string Label);

public partial class SaleHistoryItemViewModel : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty] private int _saleNumber;

    [ObservableProperty] private DateTime _saleDate;

    [ObservableProperty] private decimal _total;

    [ObservableProperty] private SaleStatus _status;

    [ObservableProperty] private int _itemCount;

    [ObservableProperty] private string? _customerDocument;

    public string StatusText => Status switch
    {
        SaleStatus.Completed => "Completed",
        SaleStatus.Cancelled => "Cancelled",
        SaleStatus.InProgress => "In Progress",
        _ => "Unknown"
    };
}

public partial class SaleDetailViewModel : ObservableObject
{
    [ObservableProperty] private int _saleNumber;

    [ObservableProperty] private DateTime _saleDate;

    [ObservableProperty] private decimal _subtotal;

    [ObservableProperty] private decimal _discount;

    [ObservableProperty] private decimal _total;

    [ObservableProperty] private SaleStatus _status;

    [ObservableProperty] private string? _customerDocument;

    public ObservableCollection<SaleDetailItemViewModel> Items { get; } = new();
    public ObservableCollection<SaleDetailPaymentViewModel> Payments { get; } = new();
}

public partial class SaleDetailItemViewModel : ObservableObject
{
    [ObservableProperty] private string _barcode = string.Empty;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private decimal _quantity;

    [ObservableProperty] private decimal _unitPrice;

    [ObservableProperty] private decimal _discount;

    [ObservableProperty] private decimal _total;
}

public partial class SaleDetailPaymentViewModel : ObservableObject
{
    [ObservableProperty] private PaymentMethod _method;

    [ObservableProperty] private decimal _amount;

    [ObservableProperty] private string? _authorizationCode;

    [ObservableProperty] private DateTime _paymentDate;

    public string MethodText => Method switch
    {
        PaymentMethod.Cash => "Cash",
        PaymentMethod.CreditCard => "Credit Card",
        PaymentMethod.DebitCard => "Debit Card",
        PaymentMethod.Pix => "PIX",
        PaymentMethod.FoodVoucher => "Food Voucher",
        PaymentMethod.MealVoucher => "Meal Voucher",
        _ => "Other"
    };
}