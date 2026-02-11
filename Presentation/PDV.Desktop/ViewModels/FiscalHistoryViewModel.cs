using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Desktop.Services;
using PDV.Shared.Enums;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class FiscalHistoryViewModel : ViewModelBase
{
    private readonly IFiscalTransactionRepository _transactionRepository;
    private readonly IFiscalReprintLogRepository _reprintLogRepository;
    private readonly IFiscalManager _fiscalManager;
    private readonly IOperatorSessionService _sessionService;
    private readonly IReceiptPrinterService _printerService;
    private readonly IThermalPrinterService? _thermalPrinterService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now.Date.AddDays(-7);

    [ObservableProperty]
    private DateTimeOffset? _endDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private FiscalStatus? _selectedStatus;

    [ObservableProperty]
    private string _statusMessage = Res.FiscalHist_Msg_SelectPeriod;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private FiscalTransactionItemViewModel? _selectedTransaction;

    [ObservableProperty]
    private FiscalTransactionDetailViewModel? _transactionDetail;

    [ObservableProperty]
    private bool _showReprintDialog;

    [ObservableProperty]
    private string _reprintReason = string.Empty;

    // Thermal Printer Settings
    [ObservableProperty]
    private bool _useThermalPrinter;

    [ObservableProperty]
    private string? _selectedThermalPrinter;

    [ObservableProperty]
    private int _qrCodeSize = 6;

    public ObservableCollection<string> AvailablePrinters { get; } = new();

    public PaginationState Pagination { get; }

    // Summary
    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private int _authorizedCount;

    [ObservableProperty]
    private int _contingencyCount;

    [ObservableProperty]
    private int _cancelledCount;

    public ObservableCollection<FiscalTransactionItemViewModel> Transactions { get; } = new();

    public List<FiscalStatusOption> StatusOptions { get; } = new()
    {
        new FiscalStatusOption(null, Res.FiscalHist_StatusOpt_All),
        new FiscalStatusOption(FiscalStatus.Authorized, Res.FiscalHist_StatusOpt_Authorized),
        new FiscalStatusOption(FiscalStatus.Contingency, Res.FiscalHist_StatusOpt_Contingency),
        new FiscalStatusOption(FiscalStatus.Cancelled, Res.FiscalHist_StatusOpt_Cancelled),
        new FiscalStatusOption(FiscalStatus.Rejected, Res.FiscalHist_StatusOpt_Rejected)
    };

    public FiscalHistoryViewModel(
        IFiscalTransactionRepository transactionRepository,
        IFiscalReprintLogRepository reprintLogRepository,
        IFiscalManager fiscalManager,
        IOperatorSessionService sessionService,
        IReceiptPrinterService printerService,
        IThermalPrinterService? thermalPrinterService = null)
    {
        _transactionRepository = transactionRepository;
        _reprintLogRepository = reprintLogRepository;
        _fiscalManager = fiscalManager;
        _sessionService = sessionService;
        _printerService = printerService;
        _thermalPrinterService = thermalPrinterService;
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
            SetStatus(Res.FiscalHist_Msg_Searching, false);

            Transactions.Clear();
            TransactionDetail = null;

            var start = StartDate?.DateTime ?? DateTime.Today.AddDays(-7);
            var end = (EndDate?.DateTime ?? DateTime.Today).AddDays(1).AddSeconds(-1);

            // If search text is an access key (44 digits), search by key
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var cleanSearch = SearchText.Replace(" ", "").Trim();
                if (cleanSearch.Length == 44)
                {
                    var transaction = await _transactionRepository.GetByAccessKeyAsync(cleanSearch);
                    if (transaction != null)
                    {
                        Transactions.Add(MapToViewModel(transaction));
                        Pagination.Update(1, 1, 20);
                        UpdateSummary();
                        SetStatus(Res.FiscalHist_Msg_FoundOne, false);
                        return;
                    }
                    SetStatus(Res.FiscalHist_Msg_NotFoundByKey, true);
                    return;
                }
            }

            var (items, totalCount) = await _transactionRepository.GetPagedAsync(start, end, SelectedStatus, Pagination.CurrentPage);

            foreach (var transaction in items)
            {
                Transactions.Add(MapToViewModel(transaction));
            }

            Pagination.Update(totalCount, Pagination.CurrentPage, 20);

            UpdateSummary();
            SetStatus(string.Format(Res.FiscalHist_Msg_FoundCount, Pagination.TotalCount), false);
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_Error, ex.Message), true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTransactionDetailAsync(FiscalTransactionItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(item.Id);
            if (transaction == null)
            {
                SetStatus(Res.FiscalHist_Msg_TransactionNotFound, true);
                return;
            }

            var reprintCount = await _reprintLogRepository.GetReprintCountAsync(transaction.Id);
            var reprintLogs = await _reprintLogRepository.GetByTransactionIdAsync(transaction.Id);

            TransactionDetail = new FiscalTransactionDetailViewModel
            {
                Id = transaction.Id,
                AccessKey = transaction.AccessKey,
                Number = transaction.Number,
                Series = transaction.Series,
                Status = transaction.Status,
                StatusText = GetStatusText(transaction.Status),
                IsContingency = transaction.IsContingency,
                Protocol = transaction.Protocol,
                AuthorizationDate = transaction.AuthorizationDate,
                CreatedAt = transaction.CreatedAt,
                ReprintCount = reprintCount,
                CancellationDate = transaction.CancellationDate,
                CancellationJustification = transaction.CancellationJustification
            };

            foreach (var log in reprintLogs.Take(5))
            {
                TransactionDetail.ReprintHistory.Add(new ReprintLogItemViewModel
                {
                    ReprintNumber = log.ReprintNumber,
                    ReprintedAt = log.ReprintedAt,
                    OperatorName = log.Operator?.Name ?? Res.FiscalHist_Lbl_UnknownOperator,
                    Reason = log.Reason
                });
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.FiscalHist_Msg_DetailError, ex.Message), true);
        }
    }

    [RelayCommand]
    private void OpenReprintDialog()
    {
        if (TransactionDetail == null)
        {
            SetStatus(Res.FiscalHist_Msg_SelectFirst, true);
            return;
        }

        ReprintReason = string.Empty;
        ShowReprintDialog = true;
    }

    [RelayCommand]
    private void CancelReprintDialog()
    {
        ShowReprintDialog = false;
        ReprintReason = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmReprintAsync()
    {
        await ReprintAsync(openPdf: false, useThermal: false);
    }

    [RelayCommand]
    private async Task ReprintAsPdfAsync()
    {
        await ReprintAsync(openPdf: true, useThermal: false);
    }

    [RelayCommand]
    private async Task ReprintThermalAsync()
    {
        await ReprintAsync(openPdf: false, useThermal: true);
    }

    [RelayCommand]
    private async Task LoadPrintersAsync()
    {
        try
        {
            AvailablePrinters.Clear();
            var printers = await _printerService.GetAvailablePrintersAsync();
            foreach (var printer in printers)
            {
                AvailablePrinters.Add(printer);
            }

            if (AvailablePrinters.Any() && string.IsNullOrEmpty(SelectedThermalPrinter))
            {
                SelectedThermalPrinter = AvailablePrinters.First();
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_LoadPrintersError, ex.Message), true);
        }
    }

    [RelayCommand]
    private async Task TestQrCodeAsync()
    {
        if (_thermalPrinterService == null)
        {
            SetStatus(Res.FiscalHist_Msg_ThermalNotAvailable, true);
            return;
        }

        if (string.IsNullOrEmpty(SelectedThermalPrinter))
        {
            SetStatus(Res.FiscalHist_Msg_SelectPrinterFirst, true);
            return;
        }

        try
        {
            IsLoading = true;
            SetStatus(Res.FiscalHist_Msg_TestingQr, false);

            var success = await _thermalPrinterService.TestQrCodeSupportAsync(SelectedThermalPrinter);

            if (success)
            {
                SetStatus(Res.FiscalHist_Msg_TestQrSuccess, false);
            }
            else
            {
                SetStatus(Res.FiscalHist_Msg_TestQrFailed, true);
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.FiscalHist_Msg_TestQrError, ex.Message), true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReprintAsync(bool openPdf, bool useThermal)
    {
        if (TransactionDetail == null)
        {
            SetStatus(Res.FiscalHist_Msg_NoTransaction, true);
            return;
        }

        var operatorId = _sessionService.CurrentOperator?.Id;
        if (operatorId == null)
        {
            SetStatus(Res.FiscalHist_Msg_NotLoggedIn, true);
            return;
        }

        try
        {
            IsLoading = true;
            ShowReprintDialog = false;

            var result = await _fiscalManager.ReprintDanfeAsync(
                TransactionDetail.AccessKey,
                operatorId.Value,
                string.IsNullOrWhiteSpace(ReprintReason) ? null : ReprintReason.Trim());

            if (!result.Success)
            {
                SetStatus(string.Format(Res.FiscalHist_Msg_ReprintError, result.StatusMessage), true);
                return;
            }

            // Update detail to show new reprint count
            TransactionDetail.ReprintCount = result.ReprintNumber;

            if (useThermal && _thermalPrinterService != null)
            {
                // ESC/POS thermal printing with native QR Code
                if (string.IsNullOrEmpty(SelectedThermalPrinter))
                {
                    SetStatus(Res.FiscalHist_Msg_SelectThermal, true);
                    return;
                }

                if (!string.IsNullOrEmpty(result.DanfeContent))
                {
                    var printSuccess = await _thermalPrinterService.PrintDanfeWithQrCodeAsync(
                        result.DanfeContent,
                        result.QrCodeUrl,
                        SelectedThermalPrinter,
                        QrCodeSize);

                    if (printSuccess)
                    {
                        var qrCodeInfo = result.HasQrCode ? Res.FiscalHist_Msg_WithNativeQr : "";
                        SetStatus(string.Format(Res.FiscalHist_Msg_ReprintEscPos, result.ReprintNumber, qrCodeInfo), false);
                    }
                    else
                    {
                        SetStatus(Res.FiscalHist_Msg_EscPosFailed, true);
                    }
                }
            }
            else if (openPdf && result.HasQrCode)
            {
                // Open PDF with QR Code visible
                if (result.PdfContent != null && result.PdfContent.Length > 0)
                {
                    var fileName = $"DANFE_NFC-e_{TransactionDetail.Number}_Reprint{result.ReprintNumber}.pdf";
                    var pdfPath = await _printerService.OpenPdfAsync(result.PdfContent, fileName);
                    SetStatus(string.Format(Res.FiscalHist_Msg_PdfOpened, pdfPath), false);
                }
                else
                {
                    SetStatus(Res.FiscalHist_Msg_PdfUnavailable, true);
                    if (!string.IsNullOrEmpty(result.DanfeContent))
                    {
                        await _printerService.PrintAsync(result.DanfeContent);
                    }
                }
            }
            else
            {
                // Print text content (standard printing)
                if (!string.IsNullOrEmpty(result.DanfeContent))
                {
                    await _printerService.PrintAsync(result.DanfeContent);
                }

                var qrCodeInfo = result.HasQrCode ? Res.FiscalHist_Msg_QrCodeAvailPdf : "";
                SetStatus(string.Format(Res.FiscalHist_Msg_ReprintSentToPrinter, result.ReprintNumber, qrCodeInfo), false);
            }

            // Reload detail to show new reprint in history
            await LoadTransactionDetailAsync(SelectedTransaction);
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.FiscalHist_Msg_ReprintFinalError, ex.Message), true);
        }
        finally
        {
            IsLoading = false;
            ReprintReason = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SavePdfAsync()
    {
        if (TransactionDetail == null)
        {
            SetStatus(Res.FiscalHist_Msg_NoTransaction, true);
            return;
        }

        var operatorId = _sessionService.CurrentOperator?.Id;
        if (operatorId == null)
        {
            SetStatus(Res.FiscalHist_Msg_NotLoggedIn, true);
            return;
        }

        try
        {
            IsLoading = true;

            // Generate DANFE without creating reprint log (just for viewing)
            var result = await _fiscalManager.ReprintDanfeAsync(
                TransactionDetail.AccessKey,
                operatorId.Value,
                Res.FiscalHist_Msg_SavePdfConsult);

            if (!result.Success || result.PdfContent == null)
            {
                SetStatus(Res.FiscalHist_Msg_PdfError, true);
                return;
            }

            // Save to Documents folder
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var fileName = $"DANFE_NFC-e_{TransactionDetail.Number}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(documentsPath, "PDV", "DANFE", fileName);

            await _printerService.SavePdfAsync(result.PdfContent, filePath);

            SetStatus(string.Format(Res.FiscalHist_Msg_PdfSaved, filePath), false);
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.FiscalHist_Msg_PdfSaveError, ex.Message), true);
        }
        finally
        {
            IsLoading = false;
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
        TransactionDetail = null;
        SelectedTransaction = null;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    partial void OnSelectedTransactionChanged(FiscalTransactionItemViewModel? value)
    {
        if (value != null)
        {
            LoadTransactionDetailCommand.Execute(value);
        }
    }

    private FiscalTransactionItemViewModel MapToViewModel(PDV.Core.Entities.FiscalTransaction transaction)
    {
        return new FiscalTransactionItemViewModel
        {
            Id = transaction.Id,
            AccessKey = transaction.AccessKey,
            Number = transaction.Number,
            Series = transaction.Series,
            Status = transaction.Status,
            StatusText = GetStatusText(transaction.Status),
            IsContingency = transaction.IsContingency,
            CreatedAt = transaction.CreatedAt,
            SaleTotal = transaction.Sale?.Total ?? 0
        };
    }

    private void UpdateSummary()
    {
        TotalTransactions = Transactions.Count;
        AuthorizedCount = Transactions.Count(t => t.Status == FiscalStatus.Authorized);
        ContingencyCount = Transactions.Count(t => t.Status == FiscalStatus.Contingency);
        CancelledCount = Transactions.Count(t => t.Status == FiscalStatus.Cancelled);
    }

    private static string GetStatusText(FiscalStatus status) => status switch
    {
        FiscalStatus.Pending => Res.FiscalHist_Status_Pending,
        FiscalStatus.Authorized => Res.FiscalHist_Status_Authorized,
        FiscalStatus.Rejected => Res.FiscalHist_Status_Rejected,
        FiscalStatus.Cancelled => Res.FiscalHist_Status_Cancelled,
        FiscalStatus.Contingency => Res.FiscalHist_Status_Contingency,
        _ => Res.Global_Lbl_Unknown
    };

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}

public record FiscalStatusOption(FiscalStatus? Value, string Label);

public partial class FiscalTransactionItemViewModel : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty]
    private string _accessKey = string.Empty;

    [ObservableProperty]
    private int _number;

    [ObservableProperty]
    private int _series;

    [ObservableProperty]
    private FiscalStatus _status;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isContingency;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private decimal _saleTotal;

    public string FormattedAccessKey => FormatAccessKey(AccessKey);

    private static string FormatAccessKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length != 44)
            return key;

        // Show first 11 and last 11 chars with ... in middle
        return $"{key[..11]}...{key[^11..]}";
    }
}

public partial class FiscalTransactionDetailViewModel : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty]
    private string _accessKey = string.Empty;

    [ObservableProperty]
    private int _number;

    [ObservableProperty]
    private int _series;

    [ObservableProperty]
    private FiscalStatus _status;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isContingency;

    [ObservableProperty]
    private string? _protocol;

    [ObservableProperty]
    private DateTime? _authorizationDate;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private int _reprintCount;

    [ObservableProperty]
    private DateTime? _cancellationDate;

    [ObservableProperty]
    private string? _cancellationJustification;

    public string NfceNumberDisplay => string.Format(Res.FiscalHist_Lbl_NfceNumber, Number);
    public string ReprintTotalDisplay => string.Format(Res.FiscalHist_Lbl_ReprintTotal, ReprintCount);

    partial void OnNumberChanged(int value) => OnPropertyChanged(nameof(NfceNumberDisplay));
    partial void OnReprintCountChanged(int value) => OnPropertyChanged(nameof(ReprintTotalDisplay));

    public ObservableCollection<ReprintLogItemViewModel> ReprintHistory { get; } = new();

    public string FormattedAccessKey
    {
        get
        {
            if (string.IsNullOrEmpty(AccessKey) || AccessKey.Length != 44)
                return AccessKey;

            // Format in groups of 4 for readability
            var parts = new List<string>();
            for (int i = 0; i < AccessKey.Length; i += 4)
            {
                parts.Add(AccessKey.Substring(i, Math.Min(4, AccessKey.Length - i)));
            }
            return string.Join(" ", parts);
        }
    }
}

public partial class ReprintLogItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _reprintNumber;

    [ObservableProperty]
    private DateTime _reprintedAt;

    [ObservableProperty]
    private string _operatorName = string.Empty;

    [ObservableProperty]
    private string? _reason;

    public string OperatorNameDisplay => string.Format(Res.FiscalHist_Lbl_ReprintBy, OperatorName);

    partial void OnOperatorNameChanged(string value) => OnPropertyChanged(nameof(OperatorNameDisplay));
}
