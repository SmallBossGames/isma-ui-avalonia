# Task 01: BlueprintCanvasViewModel

**Scope:** `src/ISMA.ViewModels/ViewModels/BlueprintCanvasViewModel.cs` (new file)
**Effort:** Medium
**Risk:** Very low

## Problem Description

The JavaFX version has `CanvasViewModel` — an editor-only data class that tracks runtime relationships between UI nodes and models. When a state is removed, `CanvasViewModel.removeState()` cascades to remove all associated transactions and loops. The Avalonia version has no equivalent. The ViewModel manages collections directly, so state removal leaves orphaned references in the transaction/loop collections.

## Expectations

- A new file `src/ISMA.ViewModels/ViewModels/BlueprintCanvasViewModel.cs` exists
- It tracks editor-only data: state-to-model mapping, transaction-to-state references, loop-to-state references
- `RemoveState(stateName)` removes the state AND all transactions/loops referencing it
- `RemoveTransaction(startState, endState, predicate)` removes a specific transaction
- `RemoveLoop(stateName)` removes a specific loop
- `ClearAll()` resets everything
- No UI framework references — pure C# with domain models
- `BlueprintEditorViewModel` uses this as its internal canvas tracker

## Implementation

### Key Design Decisions

- The `BlueprintCanvasViewModel` is an internal helper — not a public service. It's owned by `BlueprintEditorViewModel`.
- It tracks by state **name** (not reference) to match the domain model's approach.
- It holds `BlueprintStateViewModel` references for the active editor session (not domain models).
- Transaction/loop removal uses the same identity criteria as the domain model: `(startState, endState, predicate)` for transactions, `stateName` for loops.

### Implementation Steps

1. Create `BlueprintCanvasViewModel.cs` in `src/ISMA.ViewModels/ViewModels/`
2. Define internal data structures:
   - `_states` — `List<BlueprintStateViewModel>` tracking active editor states
   - `_transactions` — `List<BlueprintTransactionViewModel>` tracking active editor transactions
   - `_loops` — `List<BlueprintLoopTransactionViewModel>` tracking active editor loops
3. Implement `AddState(state)` — adds to `_states`
4. Implement `AddTransaction(tx)` — adds to `_transactions`
5. Implement `AddLoop(loop)` — adds to `_loops`
6. Implement `RemoveState(stateName)` — removes from `_states`, then removes all transactions where `StartState.Name == stateName` or `EndState.Name == stateName`, then removes all loops where `State.Name == stateName`
7. Implement `RemoveTransaction(startName, endName, predicate)` — removes matching transaction
8. Implement `RemoveLoop(stateName)` — removes matching loop
9. Implement `GetStateByName(name)` — lookup helper
10. Implement `ClearAll()` — clears all collections
11. No public properties needed — the ViewModel manages the collections directly

### High-Level Description

**Existing code:** No `BlueprintCanvasViewModel` exists. `BlueprintEditorViewModel` manages `ObservableCollection<BlueprintStateViewModel>`, `ObservableCollection<BlueprintTransactionViewModel>`, and `ObservableCollection<BlueprintLoopTransactionViewModel>` directly. The `RemoveStateCommand` only removes from the model, not from the ViewModel's own collections.

**Target approach:** `BlueprintCanvasViewModel` is a private field inside `BlueprintEditorViewModel`. It tracks the editor session's state/transaction/loop relationships. When `ReloadViews()` populates the `ObservableCollection`s, it also populates the `BlueprintCanvasViewModel`. When state removal happens, `BlueprintCanvasViewModel.RemoveState()` cascades first, then the model is updated, then `ReloadViews()` repopulates both the model and the canvas.

## Tests

### Test Scenarios

1. Add a state, add a transaction from that state, remove the state — transaction is also removed from canvas
2. Add a state, add a loop on that state, remove the state — loop is also removed from canvas
3. Remove a state that has no transactions or loops — only the state is removed
4. Remove a state that is referenced by multiple transactions — all transactions removed
5. ClearAll() removes everything
6. GetStateByName returns correct state, null for non-existent

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `grep -r "BlueprintCanvasViewModel" src/ISMA.ViewModels/` finds the new file
- [ ] No UI framework imports (`Avalonia.*`) in `BlueprintCanvasViewModel.cs`
- [ ] File is in `src/ISMA.ViewModels/ViewModels/`

## Acceptance Criteria

- [ ] `BlueprintCanvasViewModel.cs` exists with all methods implemented
- [ ] `RemoveState()` cascades to transactions and loops
- [ ] `RemoveTransaction()` removes by identity
- [ ] `RemoveLoop()` removes by state name
- [ ] `ClearAll()` empties all collections
- [ ] No Avalonia dependencies in the file
- [ ] `BlueprintEditorViewModel` has a `BlueprintCanvasViewModel` field (integration wired but not yet used)
