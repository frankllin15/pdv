using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace PDV.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private bool _isCheckoutActive = true;

    [ObservableProperty]
    private bool _isProductsActive;

    [ObservableProperty]
    private bool _isSalesHistoryActive;

    public MainViewModel()
    {
        _currentView = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? new CheckoutViewModel(null!, null!);
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
}
