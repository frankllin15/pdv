using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Shared.Enums;

namespace PDV.Desktop.ViewModels;

public partial class FiscalHistoryViewModel : ViewModelBase
{
    private readonly IFiscalTransactionRepository _transactionRepository;
    private readonly IFiscalReprintLogRepository _reprintLogRepository;
    private readonly IFiscalManager _fiscalManager;
    private readonly IOperatorSessionService _sessionService;
    private readonly IReceiptPrinterService _printerService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now.Date.AddDays(-7);

    [ObservableProperty]
    private DateTimeOffset? _endDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private FiscalStatus? _selectedStatus;

    [ObservableProperty]
    private string _statusMessage = "Selecione um periodo e clique em Buscar";

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
        new FiscalStatusOption(null, "Todos"),
        new FiscalStatusOption(FiscalStatus.Authorized, "Autorizado"),
        new FiscalStatusOption(FiscalStatus.Contingency, "Contingencia"),
        new FiscalStatusOption(FiscalStatus.Cancelled, "Cancelado"),
        new FiscalStatusOption(FiscalStatus.Rejected, "Rejeitado")
    };

    public FiscalHistoryViewModel(
        IFiscalTransactionRepository transactionRepository,
        IFiscalReprintLogRepository reprintLogRepository,
        IFiscalManager fiscalManager,
        IOperatorSessionService sessionService,
        IReceiptPrinterService printerService)
    {
        _transactionRepository = transactionRepository;
        _reprintLogRepository = reprintLogRepository;
        _fiscalManager = fiscalManager;
        _sessionService = sessionService;
        _printerService = printerService;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            IsLoading = true;
            IsError = false;
            SetStatus("Buscando...", false);

            Transactions.Clear();
            TransactionDetail = null;

            var start = StartDate?.DateTime ?? DateTime.Today.AddDays(-7);
            var end = (EndDate?.DateTime ?? DateTime.Today).AddDays(1).AddSeconds(-1); // End of day

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
                        UpdateSummary();
                        SetStatus("1 transacao encontrada", false);
                        return;
                    }
                    SetStatus("Transacao nao encontrada com a chave informada", true);
                    return;
                }
            }

            // Search by date range
            var transactions = await _transactionRepository.GetByDateRangeAsync(start, end);

            // Filter by status if selected
            if (SelectedStatus.HasValue)
            {
                transactions = transactions.Where(t => t.Status == SelectedStatus.Value);
            }

            foreach (var transaction in transactions.OrderByDescending(t => t.CreatedAt))
            {
                Transactions.Add(MapToViewModel(transaction));
            }

            UpdateSummary();
            SetStatus($"{Transactions.Count} transacao(oes) encontrada(s)", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Erro: {ex.Message}", true);
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
                SetStatus("Transacao nao encontrada", true);
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
                    OperatorName = log.Operator?.Name ?? "Desconhecido",
                    Reason = log.Reason
                });
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao carregar detalhes: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void OpenReprintDialog()
    {
        if (TransactionDetail == null)
        {
            SetStatus("Selecione uma transacao primeiro", true);
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
        await ReprintAsync(openPdf: false);
    }

    [RelayCommand]
    private async Task ReprintAsPdfAsync()
    {
        await ReprintAsync(openPdf: true);
    }

    private async Task ReprintAsync(bool openPdf)
    {
        if (TransactionDetail == null)
        {
            SetStatus("Nenhuma transacao selecionada", true);
            return;
        }

        var operatorId = _sessionService.CurrentOperator?.Id;
        if (operatorId == null)
        {
            SetStatus("Operador nao logado", true);
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
                SetStatus($"Erro na reimpressao: {result.StatusMessage}", true);
                return;
            }

            // Update detail to show new reprint count
            TransactionDetail.ReprintCount = result.ReprintNumber;

            if (openPdf && result.HasQrCode)
            {
                // Open PDF with QR Code visible
                if (result.PdfContent != null && result.PdfContent.Length > 0)
                {
                    var fileName = $"DANFE_NFC-e_{TransactionDetail.Number}_Reprint{result.ReprintNumber}.pdf";
                    var pdfPath = await _printerService.OpenPdfAsync(result.PdfContent, fileName);
                    SetStatus($"PDF aberto: {pdfPath}", false);
                }
                else
                {
                    SetStatus("PDF nao disponivel, gerando apenas texto", true);
                    if (!string.IsNullOrEmpty(result.DanfeContent))
                    {
                        await _printerService.PrintAsync(result.DanfeContent);
                    }
                }
            }
            else
            {
                // Print text content (for thermal printers)
                if (!string.IsNullOrEmpty(result.DanfeContent))
                {
                    await _printerService.PrintAsync(result.DanfeContent);
                }

                var qrCodeInfo = result.HasQrCode ? " (com QR Code)" : " (sem QR Code)";
                SetStatus($"Reimpressao #{result.ReprintNumber} enviada para impressora{qrCodeInfo}", false);
            }

            // Reload detail to show new reprint in history
            await LoadTransactionDetailAsync(SelectedTransaction);
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao reimprimir: {ex.Message}", true);
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
            SetStatus("Nenhuma transacao selecionada", true);
            return;
        }

        var operatorId = _sessionService.CurrentOperator?.Id;
        if (operatorId == null)
        {
            SetStatus("Operador nao logado", true);
            return;
        }

        try
        {
            IsLoading = true;

            // Generate DANFE without creating reprint log (just for viewing)
            var result = await _fiscalManager.ReprintDanfeAsync(
                TransactionDetail.AccessKey,
                operatorId.Value,
                "Salvar PDF para consulta");

            if (!result.Success || result.PdfContent == null)
            {
                SetStatus("Erro ao gerar PDF", true);
                return;
            }

            // Save to Documents folder
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var fileName = $"DANFE_NFC-e_{TransactionDetail.Number}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(documentsPath, "PDV", "DANFE", fileName);

            await _printerService.SavePdfAsync(result.PdfContent, filePath);

            SetStatus($"PDF salvo em: {filePath}", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao salvar PDF: {ex.Message}", true);
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
        FiscalStatus.Pending => "Pendente",
        FiscalStatus.Authorized => "Autorizado",
        FiscalStatus.Rejected => "Rejeitado",
        FiscalStatus.Cancelled => "Cancelado",
        FiscalStatus.Contingency => "Contingencia",
        _ => "Desconhecido"
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
}
