using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Converters;

public class ModeToVisibilityConverter
{
    public bool Convert(EditorMode mode, string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
            return false;

        return parameter.ToLowerInvariant() switch
        {
            "default" => mode is EditorMode.Default,
            "addtransition" => mode is EditorMode.AddTransition,
            "removestate" => mode is EditorMode.RemoveState,
            "removetransition" => mode is EditorMode.RemoveTransition,
            _ => false
        };
    }
}
