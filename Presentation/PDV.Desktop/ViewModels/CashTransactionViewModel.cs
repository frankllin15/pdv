using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Shared.Enums;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class CashTransactionViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorSessionService _operatorSession;
    private readonly ICashSessionService _cashSessionService;
    private readonly IOperatorQuery _operatorQuery;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private CashTransactionType _transactionType;

    [ObservableProperty]
    private bool _requiresAdminAuth;

    [ObservableProperty]
    private string _adminCode = string.Empty;

    [ObservableProperty]
    private string _adminPin = string.Empty;

    public string Title => TransactionType == CashTransactionType.Supply ? Res.CashTx_Supply : Res.CashTx_Bleed;

    public event Action? TransactionCompleted;
    public event Action? TransactionCancelled;

    public CashTransactionViewModel(
        IUnitOfWork unitOfWork,
        IOperatorSessionService operatorSession,
        ICashSessionService cashSessionService,
        IOperatorQuery operatorQuery)
    {
        _unitOfWork = unitOfWork;
        _operatorSession = operatorSession;
        _cashSessionService = cashSessionService;
        _operatorQuery = operatorQuery;
    }

    public void Initialize(CashTransactionType type)
    {
        TransactionType = type;
        OnPropertyChanged(nameof(Title));

        // Bleed requires admin auth for non-admin operators
        RequiresAdminAuth = type == CashTransactionType.Bleed
            && _operatorSession.CurrentOperator?.IsAdmin != true;
    }

    private decimal ParseAmount()
    {
        if (string.IsNullOrWhiteSpace(AmountText))
            return 0;

        var text = AmountText.Trim().Replace(",", ".");
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : -1;
    }

    [RelayCommand]
    private async Task ConfirmTransactionAsync()
    {
        var amount = ParseAmount();
        if (amount <= 0)
        {
            SetError(Res.CashTx_InvalidAmount);
            return;
        }

        if (_cashSessionService.CurrentSession == null)
        {
            SetError(Res.CashTx_NoSession);
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            // Validate admin auth for bleed
            if (RequiresAdminAuth)
            {
                if (string.IsNullOrWhiteSpace(AdminCode) || string.IsNullOrWhiteSpace(AdminPin))
                {
                    SetError(Res.CashTx_AdminRequired);
                    return;
                }

                var pinHash = HashPin(AdminPin);
                var isValid = await _operatorQuery.ValidatePinAsync(AdminCode, pinHash);
                if (!isValid)
                {
                    SetError(Res.CashTx_InvalidCredentials);
                    return;
                }

                // Verify the operator is actually an admin
                var adminOp = await _operatorQuery.GetByCodeAsync(AdminCode);
                if (adminOp?.IsAdmin != true)
                {
                    SetError(Res.CashTx_NotAdmin);
                    return;
                }
            }

            var operatorId = _operatorSession.CurrentOperator?.Id
                ?? throw new InvalidOperationException(Res.CashTx_NoOperator);

            var transaction = new CashTransaction(
                _cashSessionService.CurrentSession.Id,
                TransactionType,
                amount,
                string.IsNullOrWhiteSpace(Description) ? null : Description,
                operatorId
            );

            await _unitOfWork.CashTransactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            TransactionCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            SetError(string.Format(Res.CashTx_Error, ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelTransaction()
    {
        TransactionCancelled?.Invoke();
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private static string HashPin(string pin)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(pin);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
