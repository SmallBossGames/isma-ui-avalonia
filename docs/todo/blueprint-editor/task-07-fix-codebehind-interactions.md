# Task 7 — Fix BlueprintEditorView.axaml.cs interactions

## Problem

`BlueprintEditorView.axaml.cs` has several issues:

1. **Reflection-based tab creation**: `OpenStateTextEditorTab` and `OnLoopArrowHeadClicked` use reflection (`GetField("_projectService", ...)`) to access `ProjectService` from `MainWindowViewModel`. This bypasses MVVM, is fragile, and breaks in tests.
2. **Empty relay command**: `BlueprintEditorViewModel.OpenStateTextEditor` is an empty `[RelayCommand]` method that should either be removed or implemented properly.
3. **Inline name editing**: Uses `TextBox` overlay. Spec says `TextArea` for inline name editing.
4. **Double-click threshold**: Current uses `e.ClickCount > 1` which is correct for Avalonia double-click detection.
5. **Drag threshold**: Current uses 3.0px threshold — matches spec.
6. **Single-click delay**: Current uses 200ms — matches spec.
7. **PopOver positioning**: PopOver is positioned using `Popup.Placement="Pointer"` with fixed offsets. Should be manually positioned based on click coordinates.
8. **Canvas auto-sizing**: `RecalculateCanvasSize` correctly calculates bounds and adds padding. However, it doesn't account for the ScrollPane wrapping.

## Expectations

- Tab creation uses a proper service injection pattern (not reflection)
- Inline name editing uses a proper text editing approach
- PopOver positioning is based on click coordinates
- All interaction behaviors match spec

## Implementation

1. **Remove reflection**: Create a `IStateTextEditorFactory` interface (or reuse existing `ITextEditorFactory` from spec) and inject it into `BlueprintEditorViewModel`. The ViewModel exposes an `OpenStateTextEditor` command that takes a state and a callback. The code-behind subscribes to this and creates tabs.

   Better approach: The ViewModel raises an event `StateTextEditorRequested(BlueprintStateViewModel state)` that the code-behind handles. The code-behind then uses a service locator or the window's DataContext to find the ProjectService.

   Best approach: Pass `ProjectService` to `BlueprintEditorViewModel` via constructor. The ViewModel calls `projectService.CreateNewTextProject(state.Name)` directly. No reflection needed.

2. **Inline name editing**: The current `TextBox` approach is functional. The spec mentions `TextArea` but for single-line name editing, `TextBox` is more appropriate. Keep `TextBox` but ensure it works correctly.

3. **PopOver positioning**: In `OnArrowHeadClicked`, calculate the click position relative to the canvas, then set:
   ```csharp
   EditArrowPopup.PlacementTarget = sender as Control;
   EditArrowPopup.PlacementMode = PlacementMode.Pointer;
   EditArrowPopup.HorizontalOffset = 0; // centered
   EditArrowPopup.VerticalOffset = -2; // 2px above
   EditArrowPopup.IsOpen = true;
   ```

4. **Remove empty command**: Delete the empty `OpenStateTextEditor` relay command from `BlueprintEditorViewModel.cs` since the tab creation is handled in the code-behind.

## Tests

- Integration: `BlueprintCanvasUiTests` — verify state text editor tab opens on double-click
- Integration: `BlueprintCanvasUiTests` — verify loop text editor tab opens on loop arrow double-click
- Unit: `BlueprintEditorViewModelTests` — verify no reflection usage
- Integration: `BlueprintCanvasUiTests` — verify PopOver appears at correct position

## Acceptance Criteria

- [ ] No reflection usage in code-behind
- [ ] Tab creation uses proper service injection or event-based pattern
- [ ] Inline name editing works correctly with TextBox
- [ ] PopOver positioned correctly on arrowhead click
- [ ] Empty `OpenStateTextEditor` command removed
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
