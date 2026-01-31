using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PDV.Desktop.ViewModels;

namespace PDV.Desktop.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        AvaloniaXamlLoader.Load(this);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is HomeViewModel viewModel)
        {
            await viewModel.LoadDashboardDataAsync();
        }
    }
}
