using Avalonia.Controls;
using Avalonia.Interactivity;
using PDV.Desktop.ViewModels;

namespace PDV.Desktop.Views;

public partial class ReceiptPreviewView : UserControl
{
    public ReceiptPreviewView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReceiptPreviewViewModel viewModel)
        {
            await viewModel.LoadPrintersCommand.ExecuteAsync(null);
        }
    }
}
