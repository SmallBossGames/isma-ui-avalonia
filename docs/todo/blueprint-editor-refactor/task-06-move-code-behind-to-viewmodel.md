# Task 06: Move Code-Behind Logic to ViewModel

**Scope:** `src/ISMA.App/Views/BlueprintEditorView.axaml.cs`
**Effort:** High
**Risk:** Medium

## Problem Description

`BlueprintEditorView.axaml.cs` is 581 lines containing all interaction logic: drag handling, click disambiguation, inline name editing, PopOver positioning, tab creation, and loop arrow double-click. This violates the project's "no XAML code-behind logic" rule. All this logic must be moved to `BlueprintEditorViewModel` (from Task 5) as methods and events.

## Expectations

- All interaction logic is moved from code-behind to `BlueprintEditorViewModel`
- Code-behind becomes a thin glue layer: wires ViewModel events to View elements
- `BlueprintEditorView.axaml.cs` reduced to ~50-80 lines (event wire-ups only)
- The following methods are moved to ViewModel:
  - Drag logic → `OnStateDrag(BlueprintStateViewModel, Point)`
  - Click/disambiguation → handled by `StateBox` control (Task 3)
  - Inline name edit → `OnStateNameCommitted(BlueprintStateViewModel, string)`
  - PopOver positioning → `EditArrowRequested(BlueprintTransactionViewModel, Point)` event
  - Tab creation → `StateTextEditorRequested`, `LoopTextEditorRequested` events (Task 5)
  - Loop arrow double-click → `LoopTextEditorRequested` event (Task 5)

## Implementation

### Key Design Decisions

- The `StateBox` control (Task 3) will raise `StateClicked`, `StateDoubleClicked`, `StatePressed`, `StateReleased`, `NameCommitted` events.
- The `ArrowLine` and `LoopArrow` controls will raise events that the code-behind wires to ViewModel methods.
- Code-behind becomes a passive wire-up layer — no logic, just event subscription.
- PopOver positioning: the ViewModel raises `EditArrowRequested(transaction, position)`. The code-behind creates the PopOver, positions it, and shows it. When the PopOver is dismissed, it calls `ViewModel.OnEditArrowDismissed()`.
- PopOver data: the ViewModel exposes `EditingTransaction` property. The PopOver binds to it.

### Implementation Steps

1. In `BlueprintEditorViewModel`, add methods for each interaction:
   - `OnStateDrag(BlueprintStateViewModel state, Point position)` — updates `CanvasPositionX/Y`
   - `OnStateNameCommitted(BlueprintStateViewModel state, string newName)` — calls `UpdateStateName`
   - `EditArrowRequested` event — raised when arrow head is clicked
   - `OnEditArrowDismissed()` — clears editing transaction
   - `OnLoopArrowClicked(BlueprintLoopTransactionViewModel loop)` — raises `LoopTextEditorRequested`
2. In code-behind, remove all interaction logic methods:
   - Delete: `OnStatePointerPressed`, `OnCanvasPointerMoved`, `OnCanvasPointerReleased`, `OnCanvasPointerExited`
   - Delete: `InitializeSingleClickTimer`, `OpenInlineNameEditor`, `CommitInlineName`, `CancelInlineName`, `StopInlineNameEditor`
   - Delete: `_draggingState`, `_dragStartPoint`, `_dragOffset`, `_isDragging`, `_singleClickTimer`, `_editingBorder`, `_editingTextBox`, `_previousName`, `_editingState`, `_lastPressedPoint`, `_projectService`
3. In code-behind, keep only:
   - PopOver initialization and wire-up
   - Event subscriptions (ViewModel events → View elements)
   - `OnArrowHeadClicked` — raises ViewModel event
   - `OnArrowBodyClicked` — raises ViewModel event
   - `OnLoopArrowHeadClicked` — raises ViewModel event
   - `OnLoopBodyClicked` — raises ViewModel event
4. Update `StateBox` DataTemplate to wire events to ViewModel:
   - `StateClicked` → `ViewModel.OnStateClicked(state)`
   - `StateDoubleClicked` → `ViewModel.OnStateDoubleClicked(state)`
   - `StatePressed` → `ViewModel.OnStatePressed(state)`
   - `StateReleased` → `ViewModel.OnStateReleased(state)`
   - `NameCommitted` → `ViewModel.OnStateNameCommitted(state, newName)`
5. Update ArrowDataTemplate to wire events:
   - `ArrowHeadClicked` → `ViewModel.OnArrowHeadClicked(transaction, position)`
   - `ArrowBodyClicked` → `ViewModel.OnArrowBodyClicked(transaction)`
6. Update LoopArrow DataTemplate to wire events:
   - `LoopArrowHeadClicked` → `ViewModel.OnLoopArrowHeadClicked(loop)`
   - `LoopBodyClicked` → `ViewModel.OnLoopBodyClicked(loop)`

### High-Level Description

**Existing code:** 581 lines of code-behind with all interaction logic. Finds ViewModel from DataContext, manipulates UI directly, creates tabs, handles popovers, manages drag state.

**Target approach:** Code-behind is ~50-80 lines of event wire-ups. ViewModel owns all logic. `StateBox` control encapsulates its own interactions. Arrow controls raise events. PopOver is the only UI element created in code-behind (it's a child of the Popup control).

## Tests

### Test Scenarios

1. Dragging a state updates its position via ViewModel method
2. Inline name edit commits via ViewModel method
3. Arrow head click raises ViewModel event
4. Arrow body click in remove mode removes via ViewModel
5. Loop arrow head click raises ViewModel event
6. PopOver shows when arrow head is clicked
7. PopOver dismiss clears editing state
8. No logic methods remain in code-behind

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `wc -l src/ISMA.App/Views/BlueprintEditorView.axaml.cs` shows < 100 lines
- [ ] `grep -c "private void" src/ISMA.App/Views/BlueprintEditorView.axaml.cs` shows < 5
- [ ] No `OnStatePointerPressed`, `OnCanvasPointerMoved`, `OnCanvasPointerReleased` in code-behind
- [ ] No `OpenInlineNameEditor`, `CommitInlineName`, `CancelInlineName` in code-behind
- [ ] No `_draggingState`, `_singleClickTimer`, `_editingBorder` fields in code-behind

## Acceptance Criteria

- [ ] Code-behind < 100 lines
- [ ] All interaction logic moved to ViewModel
- [ ] `StateBox` control raises all required events
- [ ] Arrow controls raise `ArrowHeadClicked`, `ArrowBodyClicked`, `LoopArrowHeadClicked`, `LoopBodyClicked`
- [ ] PopOver initialization and wire-up remains in code-behind
- [ ] No DataContext discovery patterns in code-behind
- [ ] No UI manipulation in code-behind (except PopOver)
- [ ] Build succeeds with no warnings
- [ ] All existing tests pass
