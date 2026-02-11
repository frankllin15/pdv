using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Services;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class LoginViewModel(IOperatorSessionService sessionService) : ViewModelBase
{
    [ObservableProperty]
    private string _operatorCode = string.Empty;

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    public event Action? LoginSuccessful;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(OperatorCode))
        {
            SetError(Res.Login_Msg_EnterCode);
            return;
        }

        if (string.IsNullOrWhiteSpace(Pin))
        {
            SetError(Res.Login_Msg_EnterPin);
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            var success = await sessionService.LoginAsync(OperatorCode, Pin);

            if (success)
            {
                ClearForm();
                LoginSuccessful?.Invoke();
            }
            else
            {
                SetError(Res.Login_Msg_InvalidCredentials);
            }
        }
        catch (Exception ex)
        {
            SetError(string.Format(Res.Login_Msg_LoginError, ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearPin()
    {
        Pin = string.Empty;
        HasError = false;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        Pin = string.Empty;
    }

    private void ClearForm()
    {
        OperatorCode = string.Empty;
        Pin = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
