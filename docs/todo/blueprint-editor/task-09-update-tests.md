# Task 9 — Update integration and unit tests

## Problem

Existing tests were written for the broken implementation and will fail or be inaccurate after the re-implementation:

1. **`BlueprintCanvasUiTests.cs`**:
   - `StateDrag_ClampsToNonNegative` — test is a no-op, doesn't verify clamping
   - `TransitionCreation_CreatesTransition` — comment says "AddTransition creates a loop when source == end" which is the old broken behavior
   - `EditorMode_ToggleButtonContent_UpdatesCorrectly` — verifies old button content strings

2. **`BlueprintEditorViewModelTests.cs`**: Tests may reference old positioning, old AddTransition behavior, and missing editability tests.

## Expectations

- All existing tests pass with the new implementation
- New tests cover the spec-defined behaviors
- Tests are meaningful (not no-ops)

## Implementation

1. Update `BlueprintCanvasUiTests.StateDrag_ClampsToNonNegative` to actually verify clamping
2. Update `BlueprintCanvasUiTests.TransitionCreation_CreatesTransition` to match new AddTransition behavior
3. Update `BlueprintCanvasUiTests.EditorMode_ToggleButtonContent_UpdatesCorrectly` with new button content strings
4. Add test: `BlueprintEditorViewModelTests` — verify new state position is (10, 200)
5. Add test: `BlueprintEditorViewModelTests` — verify Main position is (20, 10)
6. Add test: `BlueprintEditorViewModelTests` — verify Init position is (10, 100)
7. Add test: `BlueprintEditorViewModelTests` — verify editability in different modes
8. Add test: `BlueprintCanvasUiTests` — verify AddTransition creates inter-state transition
9. Add test: `BlueprintCanvasUiTests` — verify AddTransition creates loop when source == target

## Tests

- Run `dotnet test --filter "Blueprint"` — all tests must pass
- Run `dotnet build isma-ui-dotnet.slnx` — no warnings

## Acceptance Criteria

- [ ] All existing Blueprint tests updated and passing
- [ ] New tests added for state positioning
- [ ] New tests added for editability binding
- [ ] New tests added for AddTransition behavior
- [ ] No-op tests removed or made meaningful
- [ ] `dotnet test --filter "Blueprint"` passes with 100% success
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
