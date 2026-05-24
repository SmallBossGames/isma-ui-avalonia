using ISMA.Domain.Models;

namespace ISMA.ViewModels.Converters;

public static class SaveTargetConverter
{
    public static string ToString(SaveTarget target)
    {
        return target switch
        {
            SaveTarget.Memory => "Memory",
            SaveTarget.File => "File",
            _ => "Unknown"
        };
    }

    public static SaveTarget ToSaveTarget(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "memory" => SaveTarget.Memory,
            "file" => SaveTarget.File,
            _ => SaveTarget.Memory
        };
    }
}
