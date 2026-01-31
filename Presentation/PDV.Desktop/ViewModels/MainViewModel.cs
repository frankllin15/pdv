using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PDV.Core.Interfaces.Services;

namespace PDV.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IOperatorSessionService _sessionService;

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
        CurrentView = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? new CheckoutViewModel(null!, null!);
        ResetActiveStates();
        IsCheckoutActive = true;
    }

    [RelayCommand]
    private async Task NavigateToProductsAsync()
    {
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
        var viewModel = App.Services?.GetRequiredService<SalesHistoryViewModel>()
            ?? new SalesHistoryViewModel(null!);

        CurrentView = viewModel;
        ResetActiveStates();
        IsSalesHistoryActive = true;
    }

    [RelayCommand]
    private void Logout()
    {
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
