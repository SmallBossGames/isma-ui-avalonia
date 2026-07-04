# Task 09: New Tests for Fixed Behaviors

**Scope:** `tests/ISMA.Tests.Integration/` (new test files)
**Effort:** Medium
**Risk:** Low

## Problem Description

Several critical behaviors that were broken or missing in the original implementation need test coverage: state removal cascade, state text sync, loop double-click, toolbar visibility, and StateBox control interactions.

## Expectations

- New integration tests verify all fixed behaviors
- Tests cover the use cases documented in `docs/isma-ui/blueprint-editor/03-ux-spec.md`
- Tests follow the existing patterns: `IntegrationTestBase`, `AvaloniaFact`, `FluentAssertions`
- Tests are headless-compatible (use `Avalonia.Headless.XUnit`)

## Implementation

### Key Design Decisions

- Tests follow the existing naming convention: `Blueprint<Behavior>Tests.cs`
- Tests use `UiHelpers` for UI interactions (from `IntegrationTestBase`)
- Tests verify both ViewModel logic and UI behavior

### Implementation Steps

1. **`BlueprintCascadeTests.cs`** — State removal cascade:
   - `RemoveState_RemovesAssociatedTransactions`
   - `RemoveState_RemovesAssociatedLoops`
   - `RemoveState_WithMultipleTransactions_RemovesAll`
   - `RemoveMainState_IsNoOp`
   - `RemoveInitState_IsNoOp`

2. **`BlueprintStateTextSyncTests.cs`** — State text editor tab sync:
   - `OpenStateTextEditor_RaisesStateTextEditorRequestedEvent`
   - `StateText_ChangesInEditor_UpdatesViewModel`
   - `LoopText_ChangesInEditor_UpdatesViewModel`

3. **`BlueprintLoopDoubleClickTests.cs`** — Loop arrow double-click:
   - `LoopArrowHeadDoubleClicked_RaisesLoopTextEditorRequestedEvent`
   - `LoopTextEditorTab_CreatedWithCorrectName`

4. **`BlueprintToolbarVisibilityTests.cs`** — Toolbar visibility:
   - `ToolbarVisibleInDefaultMode`
   - `ToolbarVisibleInRemoveTransitionMode`
   - `ToolbarHiddenInAddTransitionMode`
   - `ToolbarHiddenInRemoveStateMode`

5. **`BlueprintEditorModeTests.cs`** — Mode sealed class:
   - `InitialModeIsDefault`
   - `AddTransitionMode_CarriesSelectedStates`
   - `ResetMode_ReturnsToDefault`
   - `ModeChange_UpdatesButtonContent`
   - `ModesAreMutuallyExclusive`

6. **`BlueprintStateBoxControlTests.cs`** — StateBox control:
   - `StateBox_RendersWithCorrectColor`
   - `StateBox_SingleClick_OpenInlineEditor`
   - `StateBox_DragDoesNotTriggerInlineEdit`
   - `StateBox_DoubleClick_RaisesDoubleClickedEvent`
   - `StateBox_NonEditable_DoesNotOpenInlineEditor`
   - `StateBox_CenterX_CenterY_ReturnCorrectValues`

### High-Level Description

**Existing code:** Tests cover basic functionality (add state, add transition, remove state, convert to LISMA) but don't test cascade, text sync, loop double-click, toolbar visibility, or StateBox control.

**Target approach:** Comprehensive test suite covering all fixed behaviors. Tests verify both the ViewModel logic and the UI interactions through headless Avalonia tests.

## Tests

### Test Scenarios (detailed)

1. **Cascade:**
   - Create blueprint, add state, add transaction from state, remove state → 0 transactions
   - Create blueprint, add state, add loop on state, remove state → 0 loops
   - Create blueprint, add state, add 2 transactions from state, remove state → 0 transactions
   - Try to remove Main state → still 2 states (Main + Init)
   - Try to remove Init state → still 2 states (Main + Init)

2. **Text sync:**
   - Open state text editor → event fires with correct state
   - Change text in editor → ViewModel state.Text updates
   - Open loop text editor → event fires with correct loop
   - Change text in loop editor → ViewModel loop.Text updates

3. **Loop double-click:**
   - Create loop arrow → double-click arrowhead → event fires
   - Event includes correct loop ViewModel

4. **Toolbar visibility:**
   - Default mode → toolbar visible
   - RemoveTransition mode → toolbar visible
   - AddTransition mode → toolbar hidden
   - RemoveState mode → toolbar hidden

5. **Mode sealed class:**
   - Initial mode is Default
   - AddTransition mode carries empty selected states list
   - Adding state to selected states works
   - Reset returns to Default
   - Button content matches mode

6. **StateBox control:**
   - Renders with correct fill color
   - Single-click opens inline editor after 200ms
   - Drag (3px+) prevents inline editor
   - Double-click raises event
   - Non-editable state doesn't open inline editor
   - CenterX = CanvasPositionX + 55, CenterY = CanvasPositionY + StateHeight/2

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test isma-ui-dotnet.slnx` passes all tests (existing + new)
- [ ] New test files exist: `BlueprintCascadeTests.cs`, `BlueprintStateTextSyncTests.cs`, `BlueprintLoopDoubleClickTests.cs`, `BlueprintToolbarVisibilityTests.cs`, `BlueprintEditorModeTests.cs`, `BlueprintStateBoxControlTests.cs`
- [ ] New tests cover all scenarios listed above

## Acceptance Criteria

- [ ] `BlueprintCascadeTests.cs` exists with 5+ tests
- [ ] `BlueprintStateTextSyncTests.cs` exists with 3+ tests
- [ ] `BlueprintLoopDoubleClickTests.cs` exists with 2+ tests
- [ ] `BlueprintToolbarVisibilityTests.cs` exists with 4 tests
- [ ] `BlueprintEditorModeTests.cs` exists with 5+ tests
- [ ] `BlueprintStateBoxControlTests.cs` exists with 6 tests
- [ ] All new tests pass
- [ ] All existing tests still pass
- [ ] Total test count increased (not decreased)
