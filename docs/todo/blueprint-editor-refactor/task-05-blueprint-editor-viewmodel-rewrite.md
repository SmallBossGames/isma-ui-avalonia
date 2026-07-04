# Task 05: BlueprintEditorViewModel Full Rewrite

**Scope:** `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs`
**Effort:** High
**Risk:** Medium

## Problem Description

The current `BlueprintEditorViewModel` is 655 lines mixing business logic, data management, and UI concerns. It has no `BlueprintCanvasViewModel` integration, uses the old enum-based mode system (replaced by Task 2), and has no support for state text sync or loop double-click. The ViewModel needs a complete rewrite to use the new types from Tasks 1-4, remove all UI framework coupling, and implement the missing features.

## Expectations

- `BlueprintEditorViewModel` is rewritten (~400 lines, down from 655)
- Uses `BlueprintCanvasViewModel` internally for editor-only tracking
- Uses `EditorMode` sealed class (from Task 2) instead of enum + booleans
- Implements state text sync when editor tab is opened/closed
- Implements loop double-click support (opens text editor tab for loop content)
- All interaction logic is in the ViewModel — no code-behind needed
- `ReloadViews()` populates both `ObservableCollection`s and `BlueprintCanvasViewModel`
- `SetBlueprintModel()` and `GetBlueprintModel()` work correctly with the new structure

## Implementation

### Key Design Decisions

- The ViewModel owns `BlueprintCanvasViewModel` as a private field.
- Mode management uses the sealed class from Task 2.
- State text sync: when opening a state text editor tab, register a callback on the project that updates `state.Text` when the text changes.
- Loop double-click: expose a `LoopTextEditorRequested` event that the View can handle.
- The ViewModel does NOT create UI elements (tabs, popovers) — it raises events that the View handles.

### Implementation Steps

1. Replace all enum references with `EditorMode` sealed class pattern matching.
2. Add `private readonly BlueprintCanvasViewModel _canvas = new();` field.
3. Update `ReloadViews()` to:
   - Clear `_canvas`, `_states`, `_transactions`, `_loopTransactions`
   - Populate `_states` from `_model.States`
   - Populate `_transactions` from `_model.Transactions`, looking up states from `_states`
   - Populate `_loopTransactions` from `_model.LoopTransactions`, looking up states from `_states`
   - Populate `_canvas` with the same data
4. Rewrite `AddState()` to also add to `_canvas`.
5. Rewrite `RemoveState()` to call `_canvas.RemoveState(stateName)` first (this is the cascade fix from Task 4).
6. Rewrite `RemoveTransition()` to call `_canvas.RemoveTransaction(...)`.
7. Rewrite `RemoveLoop()` to call `_canvas.RemoveLoop(...)`.
8. Rewrite `AddTransition()` to use pattern matching on `Mode`:
   - `Mode is EditorMode.AddTransition addMode`
   - If `addMode.SelectedStates.Count == 0`: add current `SelectedState` to list
   - If `addMode.SelectedStates.Count == 1`: create transition or loop, reset mode
9. Add `LoopTextEditorRequested` event: `event Action<BlueprintLoopTransactionViewModel>? LoopTextEditorRequested;`
10. Add `StateTextEditorRequested` event: `event Action<BlueprintStateViewModel>? StateTextEditorRequested;`
11. Add `OpenStateTextEditor(BlueprintStateViewModel state)` method that raises `StateTextEditorRequested`.
12. Add `OpenLoopTextEditor(BlueprintLoopTransactionViewModel loop)` method that raises `LoopTextEditorRequested`.
13. Add `UpdateStateName()` — rename state, update all transaction references (same logic as current but using `_canvas`).
14. Remove `IsAddTransitionMode`, `IsRemoveStateMode`, `IsRemoveTransitionMode`, `IsDefaultMode` properties — replace with pattern-matched computed properties.
15. Update `UpdateStateEditability()` to check `Mode is not EditorMode.AddTransition and not EditorMode.RemoveState`.
16. Update button content properties to use switch expression on `Mode`.

### High-Level Description

**Existing code:** 655-line ViewModel with enum-based modes, no canvas tracking, no text sync, no loop double-click. State removal doesn't cascade.

**Target approach:** Clean ViewModel that:
- Uses `EditorMode` sealed class for type-safe mode management
- Uses `BlueprintCanvasViewModel` for editor-only tracking with cascade removal
- Raises events for UI actions (open text editor tab, show popOver)
- Contains all business logic with zero UI framework coupling
- ~400 lines (38% reduction)

## Tests

### Test Scenarios

1. Add state → added to both ObservableCollection and CanvasViewModel
2. Remove state → removed from ObservableCollection AND CanvasViewModel (cascade)
3. Remove state with transactions → transactions also removed from CanvasViewModel
4. Remove state with loops → loops also removed from CanvasViewModel
5. Add transition via sealed mode → works correctly with pattern matching
6. Add loop via sealed mode (same state clicked twice) → works correctly
7. `LoopTextEditorRequested` event fires when `OpenLoopTextEditor()` is called
8. `StateTextEditorRequested` event fires when `OpenStateTextEditor()` is called
9. `ReloadViews()` correctly populates all collections and canvas
10. `SetBlueprintModel()` rebuilds everything from a saved model
11. `GetBlueprintModel()` returns correct model from current state
12. State rename updates all transaction references

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `wc -l src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` shows ~400 lines
- [ ] `grep -c "BlueprintEditorMode\." src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` returns 0
- [ ] `grep "BlueprintCanvasViewModel" src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` finds the field
- [ ] `grep "EditorMode\." src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` finds pattern matching

## Acceptance Criteria

- [ ] `BlueprintEditorViewModel` uses `EditorMode` sealed class (no enum)
- [ ] `BlueprintEditorViewModel` has `BlueprintCanvasViewModel` field
- [ ] `ReloadViews()` populates both collections and canvas
- [ ] `RemoveState()` cascades via `BlueprintCanvasViewModel`
- [ ] `AddTransition()` uses pattern matching on `Mode`
- [ ] `StateTextEditorRequested` event exists and fires
- [ ] `LoopTextEditorRequested` event exists and fires
- [ ] No UI framework references in ViewModel
- [ ] All existing tests pass
- [ ] File is ~400 lines (38% reduction from 655)
