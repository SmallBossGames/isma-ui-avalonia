using System;
using System.Globalization;
using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Converters;

public static class ModeToVisibilityConverter
{
    public static bool Convert(BlueprintEditorMode mode, string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
            return false;

        return Enum.TryParse(parameter, true, out BlueprintEditorMode target) && mode == target;
    }
}
