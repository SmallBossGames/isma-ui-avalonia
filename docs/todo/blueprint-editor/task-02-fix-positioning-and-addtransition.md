# Task 2 — Fix state positioning and AddTransition logic

## Problem

Multiple positioning and transition creation issues deviate from the Kotlin spec:

1. **User state default position**: Spec says new states appear at (10, 200). Current code uses `(100 + count*30, 100 + count*30)` — offset by state count, not at the spec position.
2. **Main/Init default positions**: Spec says Main at (20, 10) and Init at (10, 100). Current `BlueprintModel.Empty` does not enforce these positions.
3. **AddTransition command**: The relay command `AddTransition()` always creates a loop (startState == endState == SelectedState). But the code-behind already handles the two-state transition flow in `OnStatePointerPressed`. This means inter-state transitions are created by the code-behind setting `SelectedState` to the target and then the code-behind calls `AddTransitionCommand` — but `AddTransition` ignores the source and creates a loop anyway.

## Expectations

- New user states are created at position (10, 200) per spec
- Main state defaults to position (20, 10)
- Init state defaults to position (10, 100)
- `AddTransition` command creates an inter-state transition from `SelectedState` to the previously selected state (not a loop)
- The code-behind flow for add-transition mode is preserved: first click sets source, second click sets target and calls AddTransition

## Implementation

1. In `BlueprintEditorViewModel.AddState()`, change positioning to `CanvasPositionX = 100`, `CanvasPositionY = 100` (spec says 10, 200 but the coordinate system may differ — use the spec values: X=10, Y=200)
2. In `MapState()`, pass position overrides for Main (20, 10) and Init (10, 100)
3. In `BlueprintModel.Empty`, set Main position to (20, 10) and Init position to (10, 100)
4. Restructure `AddTransition()`: use `SelectedState` as the target, look up the previous source from a new `_transitionSource` field, and create a transition between them
5. In code-behind `OnStatePointerPressed`, store the first-clicked state in `vm._transitionSource` (or a public property) and the second click creates the transition
6. When source == target on second click, create a loop instead

## Tests

- Unit: `BlueprintEditorViewModelTests` — verify new state position is (10, 200)
- Unit: `BlueprintEditorViewModelTests` — verify Main state position is (20, 10)
- Unit: `BlueprintEditorViewModelTests` — verify Init state position is (10, 100)
- Unit: `BlueprintEditorViewModelTests` — verify AddTransition creates inter-state transition (not loop)
- Unit: `BlueprintEditorViewModelTests` — verify AddTransition creates loop when source == target
- Integration: `BlueprintCanvasUiTests` — verify transition creation flow with two different states

## Acceptance Criteria

- [ ] New user states appear at (10, 200)
- [ ] Main state defaults to (20, 10)
- [ ] Init state defaults to (10, 100)
- [ ] AddTransition creates inter-state transition when source != target
- [ ] AddTransition creates loop when source == target
- [ ] Code-behind add-transition flow works correctly
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
