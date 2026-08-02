using System;
using System.Collections.Generic;

namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// Stack-based undo/redo implementation with max 50 entries.
/// </summary>
public class UndoRedoService : IUndoRedoService
{
    private const int MaxEntries = 50;

    private readonly Stack<(string Description, System.Action Execute, System.Action Undo)> _undoStack = new();
    private readonly Stack<(string Description, System.Action Execute, System.Action Undo)> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Push(string description, System.Action execute, System.Action undo)
    {
        execute();
        _undoStack.Push((description, execute, undo));
        _redoStack.Clear();

        // Trim to max entries
        while (_undoStack.Count > MaxEntries)
        {
            _undoStack.Pop();
        }
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var (description, execute, undo) = _undoStack.Pop();
        undo();
        _redoStack.Push((description, execute, undo));
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var (description, execute, undo) = _redoStack.Pop();
        execute();
        _undoStack.Push((description, execute, undo));
    }
}
