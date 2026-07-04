# Task 8 — Clean up unused code

## Problem

The codebase contains unused files and dead code that should be removed:

1. **`BlueprintEditorModeOperators.cs`**: Defines static comparison methods for `BlueprintEditorMode` enum but is not referenced anywhere in the codebase.
2. **Empty `OpenStateTextEditor` command**: `[RelayCommand] private void OpenStateTextEditor(BlueprintStateViewModel state) { }` — empty body, does nothing.
3. **`_editingTransaction` field in code-behind**: Used for tracking which transaction is being edited in the PopOver. This is functional but could be simplified.

## Expectations

- `BlueprintEditorModeOperators.cs` is deleted
- Empty `OpenStateTextEditor` command is removed from `BlueprintEditorViewModel`
- Code compiles without warnings about unused imports

## Implementation

1. Delete `src/ISMA.ViewModels/ViewModels/BlueprintEditorModeOperators.cs`
2. Remove `[RelayCommand] private void OpenStateTextEditor(BlueprintStateViewModel state) { }` from `BlueprintEditorViewModel.cs`
3. Verify no other files reference `BlueprintEditorModeOperators`

## Tests

- `grep -r "BlueprintEditorModeOperators" src/` — should return nothing
- `dotnet build isma-ui-dotnet.slnx` — should compile without warnings

## Acceptance Criteria

- [ ] `BlueprintEditorModeOperators.cs` file deleted
- [ ] No references to `BlueprintEditorModeOperators` remain
- [ ] Empty `OpenStateTextEditor` command removed
- [ ] `dotnet build isma-ui-dotnet.slnx` passes with no warnings
- [ ] `dotnet test --filter "Blueprint"` passes
