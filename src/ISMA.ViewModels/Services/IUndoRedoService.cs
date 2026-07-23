namespace ISMA.ViewModels.Services;

/// <summary>
/// Interface for undo/redo service.
/// </summary>
public interface IUndoRedoService
{
    /// <summary>
    /// Whether undo is available.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Whether redo is available.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Pushes a new undoable action onto the stack.
    /// </summary>
    void Push(string description, System.Action execute, System.Action undo);

    /// <summary>
    /// Undoes the last action.
    /// </summary>
    bool Undo();

    /// <summary>
    /// Redoes the last undone action.
    /// </summary>
    bool Redo();

    /// <summary>
    /// Clears both stacks.
    /// </summary>
    void Clear();
}
