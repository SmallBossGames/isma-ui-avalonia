using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Converters;

public class ModeToVisibilityConverter
{
    public bool Convert(BlueprintEditorMode mode, string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
            return false;

        return Enum.TryParse(parameter, true, out BlueprintEditorMode target) && mode == target;
    }
}
