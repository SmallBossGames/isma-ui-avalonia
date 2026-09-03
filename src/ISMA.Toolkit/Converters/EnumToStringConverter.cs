using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ISMA.Toolkit.Converters;

/// <summary>
/// Converts between enum values and their string representations.
/// </summary>
public class EnumToStringConverter : IValueConverter
{
    private readonly Type _enumType;

    public EnumToStringConverter(Type enumType)
    {
        _enumType = enumType;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        return value.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || value is not string stringValue) return null;
        return Enum.Parse(_enumType, stringValue, ignoreCase: true);
    }
}
