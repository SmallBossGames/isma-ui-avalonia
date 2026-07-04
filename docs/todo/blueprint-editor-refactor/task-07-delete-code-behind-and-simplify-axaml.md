# Task 07: Delete Code-Behind and Simplify AXAML

**Scope:** `src/ISMA.App/Views/BlueprintEditorView.axaml.cs`, `src/ISMA.App/Views/BlueprintEditorView.axaml`
**Effort:** Medium
**Risk:** Medium

## Problem Description

After Task 6 moves all logic to the ViewModel, the code-behind file is mostly event wire-ups. The AXAML still has event handler attributes that reference deleted code-behind methods. The toolbar is always visible (should only show on "Diagram" tab). The PopOver Popup needs to bind to ViewModel properties instead of code-behind fields.

## Expectations

- `BlueprintEditorView.axaml.cs` is deleted (or reduced to a minimal constructor that wires PopOver)
- `BlueprintEditorView.axaml` is simplified:
  - All `x:Name` references to code-behind fields removed
  - Event handlers replaced with ViewModel command bindings or event-to-command patterns
  - Toolbar visibility bound to ViewModel mode
  - PopOver binds to ViewModel properties
- No compilation errors

## Implementation

### Key Design Decisions

- The PopOver (`EditArrowPopup`) needs to remain in AXAML as a `Popup` element. Its child (`EditArrowPopOverView`) is created and positioned by code-behind.
- Since we're deleting the code-behind, the PopOver wire-up must move to the App's DI layer or be handled via a behavior/trigger.
- Alternative: keep a tiny code-behind (~20 lines) that only initializes the PopOver and sets it as the Popup's child. No event handlers, no logic.
- Toolbar visibility: bind `IsVisible` to `Mode is EditorMode.Default or EditorMode.RemoveTransition` (only show toolbar when not in add-transition or remove-state mode).

### Implementation Steps

1. **AXAML simplification:**
   - Remove `PointerMoved`, `PointerReleased`, `PointerExited` from Canvas element
   - Remove `PointerPressed` from Border in DataTemplate
   - Remove `ArrowHeadClicked`, `ArrowBodyClicked` from ArrowLine in DataTemplate
   - Remove `LoopArrowHeadClickedEvent`, `LoopBodyClicked` from LoopArrow in DataTemplate
   - Update PopOver: remove `x:Name="EditArrowPopup"`, use `{Binding}` for positioning
   - Add toolbar visibility binding: `IsVisible="{Binding IsToolbarVisible}"` (computed property on ViewModel)

2. **ViewModel toolbar visibility:**
   - Add `IsToolbarVisible` computed property: `Mode is EditorMode.Default or EditorMode.RemoveTransition`

3. **PopOver initialization:**
   - Option A: Keep minimal code-behind (~20 lines) that creates `EditArrowPopOverView`, sets `DataContext`, and assigns to Popup child
   - Option B: Move PopOver initialization to `BlueprintEditorView` constructor via DI (requires registering `EditArrowPopOverView` in DI)
   - Recommended: Option A (minimal code-behind) — less invasive, matches the pattern used elsewhere

4. **Delete code-behind:**
   - Remove all event handler methods
   - Remove all interaction logic fields
   - Keep only: constructor that initializes PopOver, `OnDataContextChanged` for ViewModel property subscriptions (if needed)

5. **StateBox DataTemplate:**
   - Replace `PointerPressed` with event binding (Avalonia doesn't support direct event-to-ViewModel binding in DataTemplates)
   - Use `EventToCommand` behavior or keep event handlers on `StateBox` that the ViewModel subscribes to
   - Actually: `StateBox` raises C# events. The code-behind (or a behavior) subscribes to them and calls ViewModel methods.

### High-Level Description

**Existing code:** AXAML has 157 lines with event handler attributes referencing code-behind methods. Code-behind has 581 lines. Toolbar always visible.

**Target approach:** AXAML has ~120 lines, no event handler attributes (except PopOver wire-up). Code-behind has ~20 lines (PopOver init only). Toolbar visibility bound to ViewModel.

## Tests

### Test Scenarios

1. Application builds successfully
2. Blueprint editor opens and renders correctly
3. Toolbar visibility changes based on mode
4. PopOver still works (shows on arrow head click, dismisses)
5. All interactions work through ViewModel
6. No runtime errors from missing event handlers

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `BlueprintEditorView.axaml.cs` does not exist OR has < 30 lines
- [ ] `grep "OnStatePointerPressed" src/ISMA.App/Views/BlueprintEditorView.axaml` returns 0
- [ ] `grep "OnCanvasPointerMoved" src/ISMA.App/Views/BlueprintEditorView.axaml` returns 0
- [ ] `grep "OnArrowHeadClicked" src/ISMA.App/Views/BlueprintEditorView.axaml` returns 0 (except PopOver)
- [ ] `grep "IsVisible" src/ISMA.App/Views/BlueprintEditorView.axaml` shows toolbar visibility binding

## Acceptance Criteria

- [ ] `BlueprintEditorView.axaml.cs` is deleted or < 30 lines
- [ ] AXAML has no event handler attributes for state/arrow interactions
- [ ] Toolbar visibility is data-bound to ViewModel
- [ ] PopOver still functions correctly
- [ ] Build succeeds
- [ ] No compilation errors or warnings
- [ ] All existing tests pass
