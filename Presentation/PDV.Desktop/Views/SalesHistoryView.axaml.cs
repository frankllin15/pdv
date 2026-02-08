using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PDV.Desktop.ViewModels;

namespace PDV.Desktop.Views;

public partial class SalesHistoryView : UserControl
{
    public SalesHistoryView()
    {
        AvaloniaXamlLoader.Load(this);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SalesHistoryViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
