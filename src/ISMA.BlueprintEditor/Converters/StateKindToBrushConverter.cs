using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Converters;

/// <summary>
/// Converts a <see cref="StateKind"/> to its fill brush:
/// Main → #90EE90 (light green), Init → #ADD8E6 (light blue), User → #FF7F50 (coral).
/// </summary>
public class StateKindToBrushConverter : IValueConverter
{
    private static readonly IBrush MainBrush = new SolidColorBrush(Color.Parse("#90EE90"));
    private static readonly IBrush InitBrush = new SolidColorBrush(Color.Parse("#ADD8E6"));
    private static readonly IBrush UserBrush = new SolidColorBrush(Color.Parse("#FF7F50"));

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            StateKind.Main => MainBrush,
            StateKind.Init => InitBrush,
            StateKind.User => UserBrush,
            _ => null,
        };
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
