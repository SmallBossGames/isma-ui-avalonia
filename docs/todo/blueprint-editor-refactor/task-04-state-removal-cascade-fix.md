# Task 04: Fix State Removal Cascade Bug

**Scope:** `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs`
**Effort:** Low
**Risk:** Low

## Problem Description

When `RemoveStateCommand` executes, it only removes the state from `_model.States`. The `BlueprintTransactionViewModel` and `BlueprintLoopTransactionViewModel` collections still contain references to the removed state. The `ReloadViews()` method tries to find states by name and silently skips broken references, but the `ObservableCollection` still holds stale `BlueprintTransactionViewModel` objects with `StartState` or `EndState` pointing to a state that no longer exists in the model.

## Expectations

- `RemoveState()` removes the state from `_model.States` AND removes all transactions and loops referencing that state
- The `ObservableCollection` properties (`Transactions`, `LoopTransactions`) are repopulated correctly
- No orphaned references remain in any collection after state removal

## Implementation

### Key Design Decisions

- This is a minimal fix — no architectural changes. Just correct the existing `RemoveState()` method.
- The fix mirrors what the JavaFX `CanvasViewModel.removeState()` does: cascade removal.

### Implementation Steps

1. In `BlueprintEditorViewModel.RemoveState()`, after removing the state from `_model.States`:
   - Remove all transactions where `tx.StartStateName == stateName` or `tx.EndStateName == stateName`
   - Remove all loops where `loop.StateName == stateName`
2. Use `_model.Transactions.RemoveAll()` and `_model.LoopTransactions.RemoveAll()` with the appropriate predicates
3. Call `ReloadViews()` to repopulate the ViewModel collections (this will rebuild everything from the cleaned model)

### High-Level Description

**Existing code:** `RemoveState()` at line 339-365 only removes the state from `_model.States`. Transactions and loops referencing the removed state are not cleaned up.

**Target approach:** `RemoveState()` removes the state, then removes matching transactions, then removes matching loops, then calls `ReloadViews()`. This matches the JavaFX behavior exactly.

## Tests

### Test Scenarios

1. Add a state, add a transaction from that state, remove the state — transaction count goes to 0
2. Add a state, add a loop on that state, remove the state — loop count goes to 0
3. Add a state with multiple transactions, remove the state — all transactions removed
4. Remove Main state — no-op (protected, same as before)
5. Remove Init state — no-op (protected, same as before)

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test isma-ui-dotnet.slnx --filter "BlueprintProject_CanRemoveStates"` passes
- [ ] `grep -A 20 "private void RemoveState" src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` shows transaction and loop cleanup

## Acceptance Criteria

- [ ] `RemoveState()` removes associated transactions
- [ ] `RemoveState()` removes associated loops
- [ ] No orphaned references in `Transactions` or `LoopTransactions` after state removal
- [ ] Existing test `BlueprintProject_CanRemoveStates` still passes
- [ ] New test `BlueprintProject_RemoveState_CascadesToTransactions` passes
- [ ] New test `BlueprintProject_RemoveState_CascadesToLoops` passes
