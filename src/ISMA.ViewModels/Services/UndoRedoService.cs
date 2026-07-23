namespace ISMA.ViewModels.Services;

/// <summary>
/// Represents a single undoable/redoable operation.
/// </summary>
public class UndoableAction
{
    private readonly Action _execute;
    private readonly Action _undo;
    private readonly string _description;

    public UndoableAction(string description, Action execute, Action undo)
    {
        _description = description;
        _execute = execute;
        _undo = undo;
    }

    public string Description => _description;

    public void Execute() => _execute();

    public void Undo() => _undo();
}

/// <summary>
/// Service that manages an undo/redo stack for operations.
/// </summary>
public class UndoRedoService : IUndoRedoService
{
    private const int MaxHistory = 50;

    private readonly List<UndoableAction> _undoStack = new();
    private readonly List<UndoableAction> _redoStack = new();

    /// <summary>
    /// Whether undo is available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Whether redo is available.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// The description of the last action on the undo stack.
    /// </summary>
    public string? LastActionDescription => _undoStack.LastOrDefault()?.Description;

    /// <summary>
    /// Pushes a new undoable action onto the stack.
    /// </summary>
    /// <param name="description">User-visible description of the action.</param>
    /// <param name="execute">The action to execute.</param>
    /// <param name="undo">The action to undo.</param>
    public void Push(string description, Action execute, Action undo)
    {
        execute();
        var action = new UndoableAction(description, execute, undo);
        _undoStack.Add(action);
        _redoStack.Clear();

        if (_undoStack.Count > MaxHistory)
        {
            _undoStack.RemoveAt(0);
        }
    }

    /// <summary>
    /// Executes the last action on the undo stack and moves it to the redo stack.
    /// </summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;

        var action = _redoStack.Last();
        _redoStack.RemoveAt(_redoStack.Count - 1);
        action.Execute();
        _undoStack.Add(action);
        return true;
    }

    /// <summary>
    /// Undoes the last action on the undo stack and moves it to the redo stack.
    /// </summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;

        var action = _undoStack.Last();
        _undoStack.RemoveAt(_undoStack.Count - 1);
        action.Undo();
        _redoStack.Add(action);
        return true;
    }

    /// <summary>
    /// Clears both stacks.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
