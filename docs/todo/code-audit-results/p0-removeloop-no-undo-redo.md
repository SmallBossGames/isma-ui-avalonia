# RemoveLoop does not use undo/redo service

**Status:** RESOLVED
**Resolved in commit:** `69d5d1c fix(viewmodels): add undo/redo support to RemoveLoop command`

**Location:** `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs:1079-1117`

**Category:** correctness

~~The `RemoveLoop` command removes a loop transaction from `LoopTransactions` but does **not** push the operation to the undo/redo service. Compare with `RemoveState` (line 929-949) and `RemoveTransition` (line 979-1003), both of which call `PushUndo`.~~

~~```csharp
// BlueprintEditorViewModel.cs:1043-1059
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
}
~~

~~The `_undoRedoService` field is available and used for other operations, making this an inconsistency.~~

~~**Impact:** Removing a loop transaction is not reversible via Undo. This is inconsistent with the behavior of removing states and transitions, which are undoable. Users lose data when they accidentally remove a loop.~~

The `RemoveLoop` method now calls `PushUndo` with proper execute/undo callbacks, consistent with `RemoveState` and `RemoveTransition`.
