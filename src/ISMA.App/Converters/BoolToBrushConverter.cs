using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ISMA.App.Converters;

/// <summary>
/// Converts a boolean value to a brush. Returns red when true, transparent when false.
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ? Brushes.Red : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
