namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// Sealed class hierarchy representing the current editor interaction mode.
/// Acts as a discriminated union for mutually exclusive operation modes.
/// </summary>
public abstract class EditorMode
{
    /// <summary>
    /// Default mode — no special interaction active.
    /// </summary>
    public sealed class Default : EditorMode { }

    /// <summary>
    /// User is clicking states to define a transition; collects 2 states (or 1 for a loop).
    /// </summary>
    public sealed class AddTransition : EditorMode { }

    /// <summary>
    /// User clicks states to delete them.
    /// </summary>
    public sealed class RemoveState : EditorMode { }

    /// <summary>
    /// User clicks transitions to delete them.
    /// </summary>
    public sealed class RemoveTransition : EditorMode { }
}
