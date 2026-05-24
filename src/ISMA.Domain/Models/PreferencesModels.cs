namespace ISMA.Domain.Models;

public sealed class WindowPreferences
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
}

public enum ProjectType { LismaText, Blueprint, Legacy }

public sealed class DefaultFilesPreferences
{
    public string[] LastOpenedProjectPath { get; set; } = Array.Empty<string>();
}

public sealed class Preferences
{
    public WindowPreferences WindowPreferences { get; set; } = new();
    public DefaultFilesPreferences DefaultFilesPreferences { get; set; } = new();
}
