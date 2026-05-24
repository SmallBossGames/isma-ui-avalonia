namespace ISMA.ViewModels.ViewModels;

public static class BlueprintEditorModeOperators
{
    public static bool EqualDefault(BlueprintEditorMode mode)
    {
        return mode == BlueprintEditorMode.Default;
    }

    public static bool EqualAddTransition(BlueprintEditorMode mode)
    {
        return mode == BlueprintEditorMode.AddTransition;
    }

    public static bool EqualRemoveState(BlueprintEditorMode mode)
    {
        return mode == BlueprintEditorMode.RemoveState;
    }

    public static bool EqualRemoveTransition(BlueprintEditorMode mode)
    {
        return mode == BlueprintEditorMode.RemoveTransition;
    }
}
