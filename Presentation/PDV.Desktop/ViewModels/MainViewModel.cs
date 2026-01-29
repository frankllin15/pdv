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

    public MainViewModel()
    {
        _currentView = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? new CheckoutViewModel(null!, null!);
    }

    [RelayCommand]
    private void NavigateToCheckout()
    {
        CurrentView = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? new CheckoutViewModel(null!, null!);
        IsCheckoutActive = true;
        IsProductsActive = false;
    }

    [RelayCommand]
    private async Task NavigateToProductsAsync()
    {
        var viewModel = App.Services?.GetRequiredService<ProductsViewModel>()
            ?? new ProductsViewModel(null!, null!);

        CurrentView = viewModel;
        IsCheckoutActive = false;
        IsProductsActive = true;

        // Auto-load products when navigating
        await viewModel.LoadProductsCommand.ExecuteAsync(null);
    }
}
