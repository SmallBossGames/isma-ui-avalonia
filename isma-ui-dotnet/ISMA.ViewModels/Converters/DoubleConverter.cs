namespace ISMA.ViewModels.Converters;

public static class DoubleConverter
{
    public static double ToDouble(string value, double defaultValue = 0.0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return double.TryParse(value, out var result) ? result : defaultValue;
    }

    public static string ToString(double value, string format = "G")
    {
        return value.ToString(format);
    }
}
