namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// The editor interaction mode state machine.
/// </summary>
public abstract class EditorMode
{
    /// <summary>The idle mode: single click edits state names, double click opens body editors.</summary>
    public static EditorMode Idle { get; } = new IdleMode();

    /// <summary>
    /// Transition-adding mode: the first clicked state is the source,
    /// the second clicked state is the target (same state twice creates a loop).
    /// </summary>
    public sealed class AddTransition : EditorMode
    {
        /// <summary>States selected so far while adding a transition.</summary>
        public List<StateViewModel> SelectedStates { get; } = new();
    }

    /// <summary>State-removal mode: clicking a user state deletes it.</summary>
    public sealed class RemoveState : EditorMode
    {
    }

    /// <summary>Transition-removal mode: clicking an arrow/loop deletes it.</summary>
    public sealed class RemoveTransition : EditorMode
    {
    }

    private sealed class IdleMode : EditorMode
    {
    }
}
