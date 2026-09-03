namespace ISMA.Toolkit.Converters;

public static class IntegerConverter
{
    public static int ToInt(string value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public static string ToString(int value)
    {
        return value.ToString();
    }
}
