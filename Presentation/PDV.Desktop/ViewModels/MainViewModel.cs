using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PDV.Core.Interfaces.Services;

namespace PDV.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IOperatorSessionService _sessionService;
    private CheckoutViewModel? _checkoutViewModel;
    private Action? _pendingNavigation;

    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _currentOperatorName = string.Empty;

    [ObservableProperty]
    private bool _isCheckoutActive = true;

    [ObservableProperty]
    private bool _isProductsActive;

    [ObservableProperty]
    private bool _isSalesHistoryActive;

    [ObservableProperty]
    private bool _showNavigationConfirmDialog;

    public MainViewModel(IOperatorSessionService sessionService)
    {
        _sessionService = sessionService;
        _sessionService.OperatorChanged += OnOperatorChanged;

        // Start with login view
        var loginViewModel = App.Services?.GetRequiredService<LoginViewModel>();
        if (loginViewModel != null)
        {
            loginViewModel.LoginSuccessful += OnLoginSuccessful;
        }
        _currentView = loginViewModel ?? throw new InvalidOperationException("LoginViewModel not registered");
    }

    private bool HasPendingSale => _checkoutViewModel?.HasPendingSale == true;

    private bool TryNavigateWithConfirmation(Action navigationAction)
    {
        if (HasPendingSale)
        {
            _pendingNavigation = navigationAction;
            ShowNavigationConfirmDialog = true;
            return false;
        }

        navigationAction();
        return true;
    }

    [RelayCommand]
    private void ConfirmNavigation()
    {
        ShowNavigationConfirmDialog = false;
        _pendingNavigation?.Invoke();
        _pendingNavigation = null;
    }

    [RelayCommand]
    private void CancelNavigation()
    {
        ShowNavigationConfirmDialog = false;
        _pendingNavigation = null;
    }

    private void OnOperatorChanged(PDV.Shared.DTOs.OperatorDto? operatorDto)
    {
        IsLoggedIn = operatorDto != null;
        CurrentOperatorName = operatorDto?.Name ?? string.Empty;
    }

    private void OnLoginSuccessful()
    {
        IsLoggedIn = true;
        CurrentOperatorName = _sessionService.CurrentOperator?.Name ?? string.Empty;
        NavigateToCheckout();
    }

    private void ResetActiveStates()
    {
        IsCheckoutActive = false;
        IsProductsActive = false;
        IsSalesHistoryActive = false;
    }

    [RelayCommand]
    private void NavigateToCheckout()
    {
        _checkoutViewModel = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? throw new InvalidOperationException("CheckoutViewModel not registered");
        CurrentView = _checkoutViewModel;
        ResetActiveStates();
        IsCheckoutActive = true;
    }

    [RelayCommand]
    private void NavigateToProducts()
    {
        if (!TryNavigateWithConfirmation(DoNavigateToProducts))
            return;
    }

    private async void DoNavigateToProducts()
    {
        _checkoutViewModel = null;
        var viewModel = App.Services?.GetRequiredService<ProductsViewModel>()
            ?? new ProductsViewModel(null!, null!);

        CurrentView = viewModel;
        ResetActiveStates();
        IsProductsActive = true;

        // Auto-load products when navigating
        await viewModel.LoadProductsCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void NavigateToSalesHistory()
    {
        if (!TryNavigateWithConfirmation(DoNavigateToSalesHistory))
            return;
    }

    private void DoNavigateToSalesHistory()
    {
        _checkoutViewModel = null;
        var viewModel = App.Services?.GetRequiredService<SalesHistoryViewModel>()
            ?? new SalesHistoryViewModel(null!);

        CurrentView = viewModel;
        ResetActiveStates();
        IsSalesHistoryActive = true;
    }

    [RelayCommand]
    private void Logout()
    {
        if (!TryNavigateWithConfirmation(DoLogout))
            return;
    }

    private void DoLogout()
    {
        _checkoutViewModel = null;
        _sessionService.Logout();
        ResetActiveStates();

        var loginViewModel = App.Services?.GetRequiredService<LoginViewModel>();
        if (loginViewModel != null)
        {
            loginViewModel.LoginSuccessful += OnLoginSuccessful;
        }
        CurrentView = loginViewModel ?? throw new InvalidOperationException("LoginViewModel not registered");
    }
}
