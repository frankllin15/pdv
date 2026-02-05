using Avalonia.Controls;
using Avalonia.Interactivity;
using PDV.Desktop.ViewModels;

namespace PDV.Desktop.Views;

public partial class FiscalConfigView : UserControl
{
    public FiscalConfigView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FiscalConfigViewModel viewModel)
        {
            await viewModel.LoadConfigCommand.ExecuteAsync(null);
        }
    }
}
