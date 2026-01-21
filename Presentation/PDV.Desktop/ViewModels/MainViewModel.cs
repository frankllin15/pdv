using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace PDV.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    public MainViewModel()
    {
        _currentView = App.Services?.GetRequiredService<CheckoutViewModel>()
            ?? new CheckoutViewModel(null!, null!);
    }
}
