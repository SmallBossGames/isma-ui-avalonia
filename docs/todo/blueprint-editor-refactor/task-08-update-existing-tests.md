# Task 08: Update Existing Tests

**Scope:** `tests/ISMA.Tests.Integration/BlueprintEditorTests.cs`, `tests/ISMA.Tests.Integration/BlueprintCanvasUiTests.cs`, `tests/ISMA.Tests.Integration/UseCases/Blueprint/BlueprintAuthoringTests.cs`, `tests/ISMA.Tests.Integration/InlineNameEditUiTests.cs`
**Effort:** Medium
**Risk:** Low

## Problem Description

Existing tests reference `BlueprintEditorMode` enum values (e.g., `BlueprintEditorMode.AddTransition`), use the old interaction model (direct property manipulation), and don't test the cascade behavior. After Tasks 1-7, these tests will fail to compile or assert incorrect behavior.

## Expectations

- All tests compile and pass
- `BlueprintEditorMode.AddTransition` → `new EditorMode.AddTransition(new MutableList<BlueprintStateViewModel>())`
- `BlueprintEditorMode.RemoveState` → `new EditorMode.RemoveState()`
- `BlueprintEditorMode.RemoveTransition` → `new EditorMode.RemoveTransition()`
- `BlueprintEditorMode.Default` → `new EditorMode.Default()`
- `IsAddTransitionMode`, `IsRemoveStateMode`, etc. → pattern matching: `vm.Mode is EditorMode.AddTransition`
- Tests for cascade behavior added (or moved to Task 9)
- Inline name edit tests updated for new `StateBox` control approach

## Implementation

### Key Design Decisions

- The `EditorMode` sealed class constructors are internal or public — whichever is appropriate.
- Tests that directly set `CurrentMode` must be updated to create new mode instances.
- Tests that check `IsAddTransitionMode` must use `vm.Mode is EditorMode.AddTransition`.
- Inline name edit tests that interact with the UI directly need to use the `StateBox` control's events.

### Implementation Steps

1. **BlueprintEditorTests.cs:**
   - Replace `editorVm.CurrentMode = BlueprintEditorMode.AddTransition` with `editorVm.Mode = new EditorMode.AddTransition(new MutableList<BlueprintStateViewModel>())`
   - Replace `editorVm.CurrentMode = BlueprintEditorMode.RemoveState` with `editorVm.Mode = new EditorMode.RemoveState()`
   - Replace `editorVm.CurrentMode.Should().Be(BlueprintEditorMode.AddTransition)` with `editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>()`
   - Update `BlueprintProject_CanRemoveStates` — verify cascade (new assertion)
   - Update `BlueprintProject_CanRemoveTransitions` — verify cascade (new assertion)

2. **BlueprintCanvasUiTests.cs:**
   - Update mode-related tests to use sealed class
   - Update `BlueprintProject_TransitionCreation_CreatesTransition` to use new mode pattern
   - Update `BlueprintProject_EditorMode_ToggleButtonContent_UpdatesCorrectly` — check button content properties

3. **BlueprintAuthoringTests.cs:**
   - Same enum → sealed class replacements as above
   - Update `UC02_BlueprintEditor_RemovesState` — verify cascade
   - Update `UC02_NameChangingMonitor_*` tests — no changes needed (NameChangingMonitor is unchanged)

4. **InlineNameEditUiTests.cs:**
   - Update for new `StateBox` control approach
   - If `StateBox` handles inline editing internally, tests may need to trigger the control's events differently
   - May need to use `UiHelpers` to interact with the `StateBox` control

### High-Level Description

**Existing code:** Tests use `BlueprintEditorMode.AddTransition` enum values, `IsAddTransitionMode` boolean properties, and direct property manipulation for interactions.

**Target approach:** Tests create `EditorMode.AddTransition(...)` instances, use `Mode is EditorMode.AddTransition` pattern matching, and test through the ViewModel's public API.

## Tests

### Test Scenarios

1. All existing tests compile
2. All existing tests pass
3. Mode-related assertions use `BeOfType<EditorMode.Xxx>()` instead of `Be(BlueprintEditorMode.Xxx)`
4. Cascade tests verify transactions and loops are removed

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test isma-ui-dotnet.slnx` passes all tests
- [ ] `grep "BlueprintEditorMode\." tests/ISMA.Tests.Integration/BlueprintEditorTests.cs` returns 0
- [ ] `grep "BlueprintEditorMode\." tests/ISMA.Tests.Integration/UseCases/Blueprint/BlueprintAuthoringTests.cs` returns 0
- [ ] `grep "CurrentMode = BlueprintEditorMode" tests/` returns 0
- [ ] `grep "IsAddTransitionMode = true" tests/` returns 0 (replaced by mode assignment)

## Acceptance Criteria

- [ ] `BlueprintEditorTests.cs` compiles and all tests pass
- [ ] `BlueprintCanvasUiTests.cs` compiles and all tests pass
- [ ] `BlueprintAuthoringTests.cs` compiles and all tests pass
- [ ] `InlineNameEditUiTests.cs` compiles and all tests pass
- [ ] No enum references to `BlueprintEditorMode` in test files
- [ ] No `CurrentMode = BlueprintEditorMode.Xxx` assignments in test files
- [ ] No `IsAddTransitionMode = true` assignments in test files
- [ ] Cascade behavior is tested
