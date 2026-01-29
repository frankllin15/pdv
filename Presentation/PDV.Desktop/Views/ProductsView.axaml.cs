using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PDV.Desktop.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
