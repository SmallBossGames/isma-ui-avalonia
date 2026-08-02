namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// Stack-based undo/redo service with configurable max depth.
/// </summary>
public interface IUndoRedoService
{
    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether redo is available.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Pushes a new undoable action onto the stack.
    /// </summary>
    /// <param name="description">Human-readable description of the action.</param>
    /// <param name="execute">The action to execute (applied immediately).</param>
    /// <param name="undo">The action to undo (reverses the execute action).</param>
    void Push(string description, System.Action execute, System.Action undo);

    /// <summary>
    /// Undoes the last action.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone action.
    /// </summary>
    void Redo();
}
