namespace ISMA.BlueprintEditor.ViewModels;

public abstract class EditorMode
{
    public bool IsNotEditingMode()
    {
        return this is IdleMode or RemoveTransitionMode;
    }
}

public sealed class IdleMode : EditorMode;

public sealed class AddTransitionMode : EditorMode
{
    public List<StateViewModel> SelectedStates { get; } = new();
}

public sealed class RemoveStateMode : EditorMode;

public sealed class RemoveTransitionMode : EditorMode;
