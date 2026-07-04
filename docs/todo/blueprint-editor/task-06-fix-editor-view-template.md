# Task 6 — Fix BlueprintEditorView.axaml template

## Problem

`BlueprintEditorView.axaml` deviates from the spec in several areas:

1. **State box corner radius**: Spec says `CORNER_RADIUS = 20` (arcWidth/arcHeight). Current uses `CornerRadius="10"`. In Avalonia, `CornerRadius` is the radius of the rounding arc, not the diameter. The JavaFX `RoundRect` arcWidth/arcHeight=20 means the arc bounding box is 20x20, so the radius is 10. So `CornerRadius="10"` is actually correct for Avalonia. However, the spec says "arc = 20px" which in JavaFX means the arc rectangle is 20px wide/tall, giving a 10px radius. This is already correct.

2. **State box fill colors**: Spec says Main=LIGHTGREEN (#90EE90), Init=LIGHTBLUE (#ADD8E6), User=CORAL (#F08080). Current binds to `FillColorHex` which is set correctly in ViewModel.

3. **State name font**: Spec says Arial 16pt. Current uses `FontSize="16"` but doesn't specify font family. Should use `FontFamily="Arial"`.

4. **Toolbar layout**: Spec shows toggle buttons with text that changes based on mode:
   - "New state" button (not toggle)
   - "New transition" / "Stop adding transaction" toggle
   - Separator
   - "Remove state" / "Stop remove state" toggle
   - "Remove transition" / "Stop remove transition" toggle
   
   Current toolbar has:
   - "+ State" button (should be "New state")
   - Mode indicator panels (not in spec — spec shows inline text on toggle buttons)
   - Toggle buttons with correct content binding
   - "Remove Selected" button (not in spec)
   - Two separators (spec shows one between add/remove groups)

5. **State height binding**: Current binds `Height="{Binding StateHeight}"` — correct.

6. **Canvas sizing**: Current has fixed 1200x800. Spec says infinite canvas with scroll. Current uses `ScrollPane` wrapper? No — the AXAML shows a `Grid` with `Canvas` directly, no `ScrollPane`. The spec says the canvas is wrapped in a `ScrollPane`.

## Expectations

- State name uses Arial font family at 16pt
- Toolbar matches spec layout: "New state" button, toggle buttons with dynamic text, one separator between add/remove groups
- Remove "Remove Selected" button (not in spec)
- Remove mode indicator panels (not in spec — spec uses toggle button text)
- Canvas is wrapped in a ScrollPane for infinite canvas behavior
- Toggle button content matches spec: "New transition" / "Stop adding transaction", etc.

## Implementation

1. In state template: add `FontFamily="Arial"` to the TextBlock
2. Rewrite toolbar to match spec:
   - Button with Content="New state", Command=AddStateCommand, visible in Default mode
   - ToggleButton for AddTransition with Content bound to AddTransitionButtonContent
   - Separator
   - ToggleButton for RemoveState with Content bound to RemoveStateButtonContent
   - ToggleButton for RemoveTransition with Content bound to RemoveTransitionButtonContent
   - Remove the mode indicator StackPanels
   - Remove "Remove Selected" button
3. Wrap Canvas in a ScrollPane:
   ```xml
   <ScrollContentPresenter>
       <Canvas ... />
   </ScrollContentPresenter>
   ```
   Or use `ScrollViewer` around the Canvas.

## Tests

- Integration: `BlueprintCanvasUiTests` — verify toolbar has correct button labels
- Integration: `BlueprintCanvasUiTests` — verify toggle button text changes with mode
- Integration: `BlueprintCanvasUiTests` — verify canvas is scrollable

## Acceptance Criteria

- [ ] State name uses Arial font family
- [ ] State name font size is 16pt
- [ ] "New state" button text matches spec
- [ ] Toggle buttons have correct dynamic content
- [ ] Mode indicator panels removed
- [ ] "Remove Selected" button removed
- [ ] Single separator between add/remove groups
- [ ] Canvas is wrapped in ScrollPane/ScrollViewer
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
