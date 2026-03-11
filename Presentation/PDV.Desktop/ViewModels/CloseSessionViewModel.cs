using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Desktop.Services;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class CloseSessionViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICashSessionService _cashSessionService;
    private readonly ICashSessionQuery _cashSessionQuery;
    private readonly IThermalPrinterService _thermalPrinterService;
    private readonly IOperatorSessionService _operatorSessionService;
    private PDV.Shared.DTOs.CashBalanceResultDto? _lastBalanceResult;

    [ObservableProperty]
    private string _countedCashBalanceText = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showResult;

    // Result panel
    [ObservableProperty]
    private decimal _openingBalance;

    [ObservableProperty]
    private decimal _totalSupply;

    [ObservableProperty]
    private decimal _totalBleed;

    [ObservableProperty]
    private decimal _totalSalesCash;

    [ObservableProperty]
    private decimal _totalSalesCard;

    [ObservableProperty]
    private decimal _totalSalesPix;

    [ObservableProperty]
    private decimal _totalSalesOther;

    [ObservableProperty]
    private decimal _expectedCashBalance;

    [ObservableProperty]
    private decimal _countedBalance;

    [ObservableProperty]
    private decimal _difference;

    [ObservableProperty]
    private bool _isDifferencePositive;

    [ObservableProperty]
    private string _printStatusMessage = string.Empty;

    public event Action? SessionClosed;

    public CloseSessionViewModel(
        IUnitOfWork unitOfWork,
        ICashSessionService cashSessionService,
        ICashSessionQuery cashSessionQuery,
        IThermalPrinterService thermalPrinterService,
        IOperatorSessionService operatorSessionService)
    {
        _unitOfWork = unitOfWork;
        _cashSessionService = cashSessionService;
        _cashSessionQuery = cashSessionQuery;
        _thermalPrinterService = thermalPrinterService;
        _operatorSessionService = operatorSessionService;
    }

    private decimal ParseCountedBalance()
    {
        if (string.IsNullOrWhiteSpace(CountedCashBalanceText))
            return 0;

        var text = CountedCashBalanceText.Trim().Replace(",", ".");
        return decimal.TryParse(text, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var value) ? value : -1;
    }

    [RelayCommand]
    private async Task CloseSessionAsync()
    {
        if (_cashSessionService.CurrentSession == null)
        {
            SetError(Res.Session_Close_NoSession);
            return;
        }

        var countedCash = ParseCountedBalance();
        if (countedCash < 0)
        {
            SetError(Res.Session_Close_InvalidValue);
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            var sessionId = _cashSessionService.CurrentSession.Id;

            var balanceResult = await _cashSessionQuery.CalculateSessionBalanceAsync(sessionId);

            var session = await _unitOfWork.CashSessions.GetByIdAsync(sessionId)
                ?? throw new InvalidOperationException(Res.Session_Close_NoSession);

            session.Close(countedCash, balanceResult.ExpectedCashBalance);
            _unitOfWork.CashSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            // Populate result
            OpeningBalance = balanceResult.OpeningBalance;
            TotalSupply = balanceResult.TotalSupply;
            TotalBleed = balanceResult.TotalBleed;
            TotalSalesCash = balanceResult.TotalSalesCash;
            TotalSalesCard = balanceResult.TotalSalesCard;
            TotalSalesPix = balanceResult.TotalSalesPix;
            TotalSalesOther = balanceResult.TotalSalesOther;
            ExpectedCashBalance = balanceResult.ExpectedCashBalance;
            CountedBalance = countedCash;
            Difference = countedCash - balanceResult.ExpectedCashBalance;
            IsDifferencePositive = Difference >= 0;

            _lastBalanceResult = balanceResult;

            ShowResult = true;

            _cashSessionService.ClearSession();
        }
        catch (Exception ex)
        {
            SetError(string.Format(Res.Session_Close_Error, ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PrintClosingReceiptAsync()
    {
        if (_lastBalanceResult == null)
            return;

        try
        {
            var operatorName = _operatorSessionService.CurrentOperator?.Name ?? "N/A";
            var terminalId = Environment.MachineName;

            var receiptBytes = CashSessionReceiptBuilder.BuildClosingReceipt(
                _lastBalanceResult,
                CountedBalance,
                operatorName,
                terminalId,
                DateTime.Now.AddHours(-1),
                DateTime.Now);

            var printers = await _thermalPrinterService.GetAvailablePrintersAsync();
            var printerName = printers.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(printerName))
            {
                PrintStatusMessage = Res.Global_Msg_PrintFailed;
                return;
            }

            var success = await _thermalPrinterService.PrintRawBytesAsync(receiptBytes, printerName);
            PrintStatusMessage = success ? Res.Global_Msg_PrintSent : Res.Global_Msg_PrintFailed;
        }
        catch (Exception ex)
        {
            PrintStatusMessage = string.Format(Res.Global_Msg_PrintError, ex.Message);
        }
    }

    [RelayCommand]
    private void ConfirmClose()
    {
        SessionClosed?.Invoke();
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
