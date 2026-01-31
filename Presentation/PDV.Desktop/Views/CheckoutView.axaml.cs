using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PDV.Desktop.ViewModels;

namespace PDV.Desktop.Views;

public partial class CheckoutView : UserControl
{
    public CheckoutView()
    {
        AvaloniaXamlLoader.Load(this);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CheckoutViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }

        // Find the DataGrid and attach event handler
        var dataGrid = this.FindControl<DataGrid>("ItemsDataGrid");
        if (dataGrid != null)
        {
            dataGrid.CellEditEnded += OnCellEditEnded;
        }
    }

    private async void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit &&
            e.Row.DataContext is SaleItemViewModel item &&
            DataContext is CheckoutViewModel viewModel)
        {
            await viewModel.UpdateItemQuantityCommand.ExecuteAsync(item);
        }
    }
}
