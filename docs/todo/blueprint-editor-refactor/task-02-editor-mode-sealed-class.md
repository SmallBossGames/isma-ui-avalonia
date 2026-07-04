# Task 02: BlueprintEditorMode Sealed Class

**Scope:** `src/ISMA.ViewModels/ViewModels/BlueprintEditorMode.cs`
**Effort:** Low
**Risk:** Low

## Problem Description

The current `BlueprintEditorMode` is an enum with 4 values, and `BlueprintEditorViewModel` maintains 4 parallel boolean flags (`IsAddTransitionMode`, `IsRemoveStateMode`, `IsRemoveTransitionMode`, `IsDefaultMode`) kept in sync via `partial void OnXxxChanged` hooks. This creates redundant state — all four booleans could theoretically be `true` simultaneously. The JavaFX version uses a sealed class hierarchy where only one mode is active at a time, and `AddTransition` carries state (the selected states list).

## Expectations

- `BlueprintEditorMode` becomes a sealed class hierarchy instead of an enum
- `EditorMode.Default` — idle state, no operation
- `EditorMode.AddTransition` — carries `IEnumerable<BlueprintStateViewModel> SelectedStates` (mutable list of clicked states)
- `EditorMode.RemoveState` — no additional state
- `EditorMode.RemoveTransition` — no additional state
- `BlueprintEditorViewModel` uses `EditorMode` property instead of 4 booleans
- `IsDefaultMode`, `IsAddTransitionMode`, etc. properties removed — replaced by pattern matching on `EditorMode`
- Button content properties (`AddTransitionButtonContent`, etc.) updated to use switch expression on `EditorMode`

## Implementation

### Key Design Decisions

- The sealed class hierarchy lives in the same file `BlueprintEditorMode.cs` for simplicity.
- `EditorMode.AddTransition` carries a `MutableList<BlueprintStateViewModel>` that tracks which states have been clicked during add-transition mode. This replaces the `_transitionSource` field.
- The ViewModel's `CurrentMode` property becomes `EditorMode Mode { get; private set; }`.
- All `partial void OnXxxChanged` hooks are removed — mode changes are explicit via `SetMode()`.

### Implementation Steps

1. Replace the enum content in `BlueprintEditorMode.cs` with a sealed class hierarchy:
   ```
   public abstract class EditorMode {
       public sealed class Default : EditorMode { }
       public sealed class AddTransition : EditorMode {
           public AddTransition(MutableList<BlueprintStateViewModel> selectedStates) { ... }
           public IList<BlueprintStateViewModel> SelectedStates { get; }
       }
       public sealed class RemoveState : EditorMode { }
       public sealed class RemoveTransition : EditorMode { }
   }
   ```
2. In `BlueprintEditorViewModel`, replace `BlueprintEditorMode _currentMode` with `EditorMode _mode = new EditorMode.Default()`.
3. Replace `IsAddTransitionMode`, `IsRemoveStateMode`, `IsRemoveTransitionMode`, `IsDefaultMode` properties with a single `Mode` property.
4. Keep `IsDefaultMode`, `IsAddTransitionMode`, etc. as computed properties that pattern-match on `Mode` (for AXAML bindings that still need bools).
5. Update `OnCurrentModeChanged` to `OnModeChanged(EditorMode value)` — update button content and state editability.
6. Update `ResetEditorMode()` to set `Mode = new EditorMode.Default()`.
7. Update `AddTransition()` to pattern-match: extract states from `Mode is EditorMode.AddTransition addMode`, handle single-click (first state) and double-click (second state).
8. Update `AddStateButtonContent`, `AddTransitionButtonContent`, etc. to use switch expression on `Mode`.
9. Update `SetTransitionSource()` and `GetTransitionSource()` to work with `Mode is EditorMode.AddTransition`.
10. Update `UpdateStateEditability()` to check `Mode is not EditorMode.AddTransition and not EditorMode.RemoveState`.

### High-Level Description

**Existing code:** `BlueprintEditorMode.cs` is a simple enum. `BlueprintEditorViewModel` has 4 boolean properties with partial method hooks that keep them in sync with `CurrentMode`. `_transitionSource` field tracks the first state clicked in add-transition mode.

**Target approach:** `BlueprintEditorMode.cs` contains a sealed class hierarchy. `EditorMode.AddTransition` carries the selected states list. `BlueprintEditorViewModel.Mode` is the single source of truth. Pattern matching replaces boolean checks. `_transitionSource` is removed — the selected states are inside the `AddTransition` mode object.

## Tests

### Test Scenarios

1. Initial mode is `Default`
2. Setting mode to `AddTransition` creates a new instance with empty selected states list
3. Clicking a state in `AddTransition` mode adds it to `SelectedStates`
4. Clicking a second state in `AddTransition` mode creates the transition and resets to `Default`
5. Clicking the same state twice in `AddTransition` mode creates a loop and resets to `Default`
6. `ResetEditorMode()` always returns to `Default`
7. `IsDefaultMode`, `IsAddTransitionMode`, etc. correctly reflect the current mode via pattern matching
8. Mutually exclusive: setting `RemoveState` mode clears `AddTransition` mode

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `grep -r "enum BlueprintEditorMode" src/ISMA.ViewModels/` returns 0 results
- [ ] `grep -r "sealed class.*EditorMode" src/ISMA.ViewModels/` finds the hierarchy
- [ ] `grep -r "BlueprintEditorMode\." src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` returns 0 results (no enum usage)

## Acceptance Criteria

- [ ] `BlueprintEditorMode.cs` contains sealed class hierarchy
- [ ] `EditorMode.Default`, `EditorMode.AddTransition`, `EditorMode.RemoveState`, `EditorMode.RemoveTransition` all exist
- [ ] `EditorMode.AddTransition` carries `SelectedStates` list
- [ ] `BlueprintEditorViewModel` uses `EditorMode` property, not enum
- [ ] 4 boolean properties replaced by pattern-matched computed properties
- [ ] `_transitionSource` field removed
- [ ] All `partial void OnXxxChanged` hooks removed
- [ ] Build succeeds with no warnings
