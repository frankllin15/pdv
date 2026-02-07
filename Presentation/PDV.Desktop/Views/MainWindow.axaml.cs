using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace PDV.Desktop.Views;

public partial class MainWindow : Window
{
    public static readonly IValueConverter ActiveBrushConverter = new BoolToActiveBrushConverter();
    public static readonly IValueConverter SidebarActiveBrushConverter = new BoolToSidebarActiveBrushConverter();

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private class BoolToActiveBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive
                    ? new SolidColorBrush(Color.Parse("#2196F3"))
                    : new SolidColorBrush(Color.Parse("#555"));
            }
            return new SolidColorBrush(Color.Parse("#555"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    private class BoolToSidebarActiveBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive
                    ? new SolidColorBrush(Color.Parse("#1976D2"))
                    : new SolidColorBrush(Colors.Transparent);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
