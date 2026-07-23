namespace ISMA.ViewModels.ViewModels;

public abstract class EditorMode
{
    public sealed class Default : EditorMode { }
    public sealed class AddTransition : EditorMode { }
    public sealed class RemoveState : EditorMode { }
    public sealed class RemoveTransition : EditorMode { }
}
