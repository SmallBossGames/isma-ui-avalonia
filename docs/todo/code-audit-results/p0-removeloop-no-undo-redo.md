# RemoveLoop does not use undo/redo service

**Status:** ✅ RESOLVED

**Resolved in commit:** `69d5d1c` — `fix(viewmodels): add undo/redo support to RemoveLoop command`

**Original Location:** `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs:1079-1117`

**Category:** correctness

The `RemoveLoop` command now calls `PushUndo` to register the operation with the undo/redo service, consistent with `RemoveState` and `RemoveTransition`.

```csharp
[RelayCommand]
private void RemoveLoop()
{
    if (SelectedState == null)
        return;

    var stateId = SelectedState.Id;

    var loopToRemove = LoopTransactions.FirstOrDefault(l => l.StateId == stateId);
    if (loopToRemove != null)
    {
        LoopTransactions.Remove(loopToRemove);
    }

    SelectedState = null;
    Mode = new EditorMode.Default();

    PushUndo($"Remove loop",
        () =>
        {
            if (LoopTransactions.All(l => l.StateId != stateId))
            {
                LoopTransactions.Add(new BlueprintLoopTransactionViewModel
                {
                    StateId = stateId,
                    Predicate = "",
                    Alias = ""
                });
            }
        },
        () =>
        {
            var loop = LoopTransactions.FirstOrDefault(l => l.StateId == stateId);
            if (loop != null)
            {
                LoopTransactions.Remove(loop);
            }
        });
}
```

**Impact:** Resolved. Removing a loop transaction is now reversible via Undo.
