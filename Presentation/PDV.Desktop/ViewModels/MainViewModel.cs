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
    private bool _isHomeActive = true;

    [ObservableProperty]
    private bool _isCheckoutActive;

    [ObservableProperty]
    private bool _isProductsActive;

    [ObservableProperty]
    private bool _isSalesHistoryActive;

    [ObservableProperty]
    private bool _isFiscalConfigActive;

    [ObservableProperty]
    private bool _isFiscalHistoryActive;

    [ObservableProperty]
    private bool _isSidebarVisible;

    [ObservableProperty]
    private bool _isCadastrosExpanded;

    [ObservableProperty]
    private bool _isFinanceiroExpanded;

    [ObservableProperty]
    private bool _isSettingsExpanded;

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
    private void ToggleCadastrosExpanded() => IsCadastrosExpanded = !IsCadastrosExpanded;

    [RelayCommand]
    private void ToggleFinanceiroExpanded() => IsFinanceiroExpanded = !IsFinanceiroExpanded;

    [RelayCommand]
    private void ToggleSettingsExpanded() => IsSettingsExpanded = !IsSettingsExpanded;

    [RelayCommand]
    private void NavigateBackFromCheckout()
    {
        if (!TryNavigateWithConfirmation(DoNavigateToHome)) return;
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
        IsSidebarVisible = true;
        CurrentOperatorName = _sessionService.CurrentOperator?.Name ?? string.Empty;
        NavigateToHome();
    }

    private void ResetActiveStates()
    {
        IsHomeActive = false;
        IsCheckoutActive = false;
        IsProductsActive = false;
        IsSalesHistoryActive = false;
        IsFiscalConfigActive = false;
        IsFiscalHistoryActive = false;
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        if (!TryNavigateWithConfirmation(DoNavigateToHome))
            return;
    }

    private void DoNavigateToHome()
    {
        _checkoutViewModel = null;
        var viewModel = App.Services?.GetRequiredService<HomeViewModel>()
            ?? throw new InvalidOperationException("HomeViewModel not registered");

        CurrentView = viewModel;
        ResetActiveStates();
        IsHomeActive = true;
        IsSidebarVisible = true;
    }

    [RelayCommand]
    private void NavigateToCheckout()
    {
        DoNavigateToCheckout();
    }

    private void DoNavigateToCheckout()
    {
        _checkoutViewModel = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? throw new InvalidOperationException("CheckoutViewModel not registered");
        CurrentView = _checkoutViewModel;
        ResetActiveStates();
        IsCheckoutActive = true;
        IsSidebarVisible = false;
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
        IsSidebarVisible = true;

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
        IsSidebarVisible = true;
    }

    [RelayCommand]
    private void NavigateToFiscalConfig()
    {
        if (!TryNavigateWithConfirmation(DoNavigateToFiscalConfig))
            return;
    }

    private void DoNavigateToFiscalConfig()
    {
        _checkoutViewModel = null;
        var viewModel = App.Services?.GetRequiredService<FiscalConfigViewModel>()
            ?? throw new InvalidOperationException("FiscalConfigViewModel not registered");

        CurrentView = viewModel;
        ResetActiveStates();
        IsFiscalConfigActive = true;
        IsSidebarVisible = true;
    }

    [RelayCommand]
    private void NavigateToFiscalHistory()
    {
        if (!TryNavigateWithConfirmation(DoNavigateToFiscalHistory))
            return;
    }

    private void DoNavigateToFiscalHistory()
    {
        _checkoutViewModel = null;
        var viewModel = App.Services?.GetRequiredService<FiscalHistoryViewModel>()
            ?? throw new InvalidOperationException("FiscalHistoryViewModel not registered");

        CurrentView = viewModel;
        ResetActiveStates();
        IsFiscalHistoryActive = true;
        IsSidebarVisible = true;
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
        IsSidebarVisible = false;

        var loginViewModel = App.Services?.GetRequiredService<LoginViewModel>();
        if (loginViewModel != null)
        {
            loginViewModel.LoginSuccessful += OnLoginSuccessful;
        }
        CurrentView = loginViewModel ?? throw new InvalidOperationException("LoginViewModel not registered");
    }
}
