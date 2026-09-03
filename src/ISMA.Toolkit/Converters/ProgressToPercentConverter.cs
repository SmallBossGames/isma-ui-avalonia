namespace ISMA.Toolkit.Converters;

public static class ProgressToPercentConverter
{
    public static int ToPercent(double progress)
    {
        return Math.Clamp((int)(progress * 100), 0, 100);
    }

    public static double FromPercent(int percent)
    {
        return Math.Clamp(percent / 100.0, 0.0, 1.0);
    }
}
