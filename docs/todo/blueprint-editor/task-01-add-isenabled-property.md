# Task 1 — Add IsEnabled to BlueprintStateViewModel

## Problem

The spec defines that user state `isEditable` is dynamically bound: `isEditable = !(isRemoveStateMode OR isAddTransitionMode)`. The `BlueprintStateViewModel` has an `IsEditable` property but it is never updated when editor mode changes. This means states remain editable even when the user is in AddTransition or RemoveState mode, allowing unintended name edits during transition drawing or state removal.

## Expectations

- `BlueprintStateViewModel` has an `IsEnabled` property (ObservableProperty) that controls whether the state can be interacted with for name editing
- The property is updated in `BlueprintEditorViewModel` whenever `CurrentMode` changes
- Main and Init states always have `IsEnabled = false`
- User states have `IsEnabled = !(IsRemoveStateMode OR IsAddTransitionMode)`

## Implementation

1. Add `[ObservableProperty] private bool _isEnabled;` to `BlueprintStateViewModel`
2. In `BlueprintEditorViewModel`, add a partial method `OnCurrentModeChanged` that updates all user states' `IsEnabled` property
3. In `MapState`, set `IsEnabled = !(isMain || isInit)` for initial creation
4. In `OnIsAddTransitionModeChanged` and `OnIsRemoveStateModeChanged`, trigger a refresh of all state `IsEnabled` values
5. Alternatively: bind `IsEnabled` directly to a computed expression in the ViewModel using a single method that iterates all states

## Tests

- Unit: `BlueprintEditorViewModelTests` — verify states are disabled in AddTransition mode
- Unit: `BlueprintEditorViewModelTests` — verify states are disabled in RemoveState mode
- Unit: `BlueprintEditorViewModelTests` — verify Main/Init states are always disabled
- Integration: `BlueprintCanvasUiTests` — verify inline name editor does not open in AddTransition mode

## Acceptance Criteria

- [ ] `BlueprintStateViewModel` has `IsEnabled` property
- [ ] User states are disabled when `IsAddTransitionMode == true`
- [ ] User states are disabled when `IsRemoveStateMode == true`
- [ ] Main/Init states always have `IsEnabled = false`
- [ ] User states are enabled when mode is Default
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
