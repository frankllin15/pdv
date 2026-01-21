using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PDV.Desktop.Converters;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colors)
        {
            var parts = colors.Split('|');
            if (parts.Length == 2)
            {
                var colorStr = boolValue ? parts[0] : parts[1];
                return Color.Parse(colorStr);
            }
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colors)
        {
            var parts = colors.Split('|');
            if (parts.Length == 2)
            {
                var colorStr = boolValue ? parts[0] : parts[1];
                return new SolidColorBrush(Color.Parse(colorStr));
            }
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
