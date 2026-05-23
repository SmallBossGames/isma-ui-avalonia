using System.Globalization;

namespace ISMA.ViewModels.Converters;

public static class BoolToVisibilityConverter
{
    public static bool ToVisibility(bool value)
    {
        return value;
    }

    public static bool FromVisibility(bool value)
    {
        return value;
    }
}
