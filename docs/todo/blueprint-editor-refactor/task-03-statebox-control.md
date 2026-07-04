# Task 03: StateBox Control

**Scope:** `src/ISMA.App/Controls/StateBox.cs` (new file), `src/ISMA.App/Views/BlueprintEditorView.axaml`
**Effort:** Medium
**Risk:** Low

## Problem Description

In the JavaFX version, `StateBox` is a rich custom control (118 lines) that encapsulates: drag detection, click/drag disambiguation via 200ms delayed coroutine, inline name editing (TextArea ↔ Label swap), editability binding, center position calculations, and layered rendering with `viewOrder`. In the Avalonia version, state boxes are plain `Border` elements in a DataTemplate — all interaction logic lives in the 581-line code-behind file.

## Expectations

- A new `StateBox` control exists at `src/ISMA.App/Controls/StateBox.cs`
- It renders a rounded rectangle with a text label, matching the JavaFX visual spec
- It encapsulates: pointer press, drag detection (3px threshold), click/disambiguation, inline name editing
- It exposes events/commands for: `StatePressed`, `StateReleased`, `StateClicked`, `StateDoubleClicked`, `NameCommitted`
- It has properties: `Name`, `Text`, `FillColor`, `IsEditable`, `IsEnabled`, `StateHeight`
- It calculates `CenterX` and `CenterY` as bindable properties
- The AXAML DataTemplate uses `<controls:StateBox>` instead of `<Border>`
- Color palette matches spec: Main=#90EE90, Init=#ADD8E6, User=#F08080

## Implementation

### Key Design Decisions

- `StateBox` extends `ContentControl` (not `Control`) to allow custom template parts.
- Inline editing uses a `TextBox` that replaces the `TextBlock` when edit mode is active.
- Click/drag disambiguation uses a `DispatcherTimer` (Avalonia equivalent of Kotlin coroutine delay) — 200ms threshold.
- Events are raised as routed events or C# events — not commands, since the control is used in a DataTemplate.
- The control handles its own pointer events internally; the code-behind doesn't need to know about state box interactions.

### Implementation Steps

1. Create `StateBox.cs` in `src/ISMA.App/Controls/`
2. Define `StyledProperty` registrations for: `Name`, `Text`, `FillColor`, `IsEditable`, `IsEnabled`, `StateHeight`, `CanvasPositionX`, `CanvasPositionY`
3. Define C# events: `StatePressed`, `StateReleased`, `StateClicked`, `StateDoubleClicked`, `NameCommitted`
4. Implement `OnPointerPressed` — record start position, start timer, raise `StatePressed`
5. Implement `OnPointerMoved` — if distance > 3px, stop timer, set `_isDragging = true`
6. Implement `OnPointerReleased` — if not dragging, stop timer and handle click; if dragging, raise `StateReleased`
7. Implement timer tick — if not dragging, enter inline edit mode (swap TextBlock for TextBox)
8. Implement inline edit — TextBox with Enter/Escape/LostFocus handlers that call `NameCommitted` or cancel
9. Implement `CenterX`/`CenterY` as computed properties or bindings
10. Update `BlueprintEditorView.axaml` DataTemplate to use `<controls:StateBox>` with bound properties
11. Remove `PointerPressed` handler from the Border in AXAML (StateBox handles it internally)

### High-Level Description

**Existing code:** State boxes are `Border` elements in a DataTemplate with `PointerPressed="OnStatePointerPressed"`. All interaction logic (drag, click, inline edit, double-click) is in `BlueprintEditorView.axaml.cs` via `OnStatePointerPressed`, `OnCanvasPointerMoved`, `OnCanvasPointerReleased`, `InitializeSingleClickTimer`, `OpenInlineNameEditor`, `CommitInlineName`, `CancelInlineName`.

**Target approach:** `StateBox` is a self-contained control. The DataTemplate instantiates it with bound properties. The control internally handles pointer events, manages its own timer, swaps its own content for inline editing, and raises events that the ViewModel can handle. The code-behind no longer needs any state box interaction logic.

## Tests

### Test Scenarios

1. StateBox renders with correct fill color (CORAL for user, LIGHTGREEN for main, LIGHTBLUE for init)
2. Single-click on editable state opens inline name editor (200ms delay)
3. Dragging state (3px+ movement) does NOT trigger inline edit
4. Pressing Enter in inline editor commits the name
5. Pressing Escape in inline editor cancels the edit
6. LostFocus in inline editor commits the name
7. Double-click raises `StateDoubleClicked` event
8. Non-editable state (Main/Init) does NOT open inline editor on click
9. `CenterX` and `CenterY` return correct values based on position and size

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `grep -r "StateBox" src/ISMA.App/Controls/` finds the new file
- [ ] `BlueprintEditorView.axaml` uses `<controls:StateBox>` in the DataTemplate
- [ ] `BlueprintEditorView.axaml.cs` no longer has `OnStatePointerPressed`, `OnCanvasPointerMoved`, `OnCanvasPointerReleased`, `InitializeSingleClickTimer`, `OpenInlineNameEditor`, `CommitInlineName`, `CancelInlineName`

## Acceptance Criteria

- [ ] `StateBox.cs` exists in `src/ISMA.App/Controls/`
- [ ] StateBox renders a rounded rectangle with text label
- [ ] Drag detection works (3px threshold)
- [ ] Click/drag disambiguation works (200ms timer)
- [ ] Inline name editing works (TextBox ↔ TextBlock swap)
- [ ] Enter/Escape/LostFocus handlers work
- [ ] `CenterX`/`CenterY` computed correctly
- [ ] AXAML DataTemplate uses `StateBox` control
- [ ] Events raised: `StatePressed`, `StateReleased`, `StateClicked`, `StateDoubleClicked`, `NameCommitted`
- [ ] Build succeeds with no warnings
