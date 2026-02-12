using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Core.Entities;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class OpenSessionViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorSessionService _operatorSession;
    private readonly ICashSessionService _cashSessionService;
    private readonly ICashSessionQuery _cashSessionQuery;

    [ObservableProperty]
    private string _openingBalanceText = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    public event Action? SessionOpened;

    public OpenSessionViewModel(
        IUnitOfWork unitOfWork,
        IOperatorSessionService operatorSession,
        ICashSessionService cashSessionService,
        ICashSessionQuery cashSessionQuery)
    {
        _unitOfWork = unitOfWork;
        _operatorSession = operatorSession;
        _cashSessionService = cashSessionService;
        _cashSessionQuery = cashSessionQuery;
    }

    private decimal ParseBalance()
    {
        if (string.IsNullOrWhiteSpace(OpeningBalanceText))
            return 0;

        var text = OpeningBalanceText.Trim().Replace(",", ".");
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : -1;
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        var openingBalance = ParseBalance();
        if (openingBalance < 0)
        {
            SetError(Res.Session_Open_InvalidBalance);
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            var operatorId = _operatorSession.CurrentOperator?.Id
                ?? throw new InvalidOperationException(Res.Session_Open_NoOperator);

            var terminalId = Environment.MachineName;

            var session = new CashSession(operatorId, terminalId, openingBalance);
            await _unitOfWork.CashSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();

            var sessionDto = new CashSessionDto(
                session.Id,
                session.OperatorId,
                _operatorSession.CurrentOperator?.Name ?? string.Empty,
                session.TerminalId,
                session.OpenedAt,
                session.ClosedAt,
                session.OpeningBalance,
                session.ClosingBalance,
                session.CalculatedBalance,
                session.Status
            );

            _cashSessionService.SetCurrentSession(sessionDto);
            SessionOpened?.Invoke();
        }
        catch (Exception ex)
        {
            SetError(string.Format(Res.Session_Open_Error, ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
