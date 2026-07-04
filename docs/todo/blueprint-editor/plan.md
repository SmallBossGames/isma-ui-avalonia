# Blueprint Editor — Re-implementation Plan

## Problem

The current Avalonia blueprint editor deviates significantly from the Kotlin/JavaFX reference specification in multiple areas: visual appearance (colors, dimensions, fonts, arrow geometry), interaction behavior (editability bindings, state positioning, toolbar layout), and architecture (reflection-based tab creation, empty relay commands, unbound properties). The result is a visually and behaviorally inconsistent editor that does not match the documented UX spec.

## Scope

| Category | Files Affected | Action |
|----------|---------------|--------|
| **Domain** | `BlueprintStateViewModel.cs` | Add `IsEnabled` property for editability binding |
| **ViewModel** | `BlueprintEditorViewModel.cs` | Fix state positioning, editability bindings, AddTransition logic, remove empty command |
| **ViewModel** | `BlueprintEditorModeOperators.cs` | Delete (unused) |
| **App View** | `BlueprintEditorView.axaml` | Fix colors, dimensions, corner radius, toolbar layout, state template |
| **App View** | `BlueprintEditorView.axaml.cs` | Fix drag logic, inline editing, reflection-based tab opening, hit testing |
| **App Control** | `ArrowLine.cs` | Fix arrow geometry (atan2-based), stroke widths, label font, arrowhead shape |
| **App Control** | `LoopArrow.cs` | Fix circle positioning, arrowhead shape, label positioning |
| **App View** | `EditArrowPopOverView.axaml` | Fix background, shadow, layout to match spec |
| **App View** | `EditArrowPopOverView.axaml.cs` | Fix positioning, binding approach |
| **Integration Tests** | `BlueprintCanvasUiTests.cs` | Update tests for new behavior |
| **Unit Tests** | `BlueprintEditorViewModelTests.cs` | Update tests for new behavior |

## Dependency Graph

```
Phase 1 (no build break):
  [Fix Domain/ViewModel properties] ──┐
  [Delete unused file]               ─┤
                                       ├──► Build passes
  [Fix ViewModel logic]              ─┤
                                       └──► Tests pass

Phase 2 (no build break):
  [Fix ArrowLine.cs geometry]        ─┐
  [Fix LoopArrow.cs geometry]        ─┤
                                       ├──► Build passes
  [Fix PopOver styling]              ─┤
                                       └──► Tests pass

Phase 3 (no build break):
  [Fix BlueprintEditorView.axaml]    ─┐
  [Fix BlueprintEditorView.axaml.cs] ─┤
                                       ├──► Build passes
  [Fix integration tests]            ─┤
                                       └──► All tests pass
```

## Execution Phases

### Phase 1 — ViewModel & Domain Foundation

Fix the data layer that drives the UI. These changes are purely ViewModel/domain and don't touch any AXAML or rendering code.

1. Add `IsEnabled` to `BlueprintStateViewModel` for editability binding (spec: `isEditable = !(isRemoveStateMode OR isAddTransitionMode)`)
2. Fix `AddState` default positioning: user states at (10, 200) per spec, not offset by count
3. Fix `AddTransition` command: it currently creates a loop when source==target but the code-behind handles two-state transitions; restructure so AddTransition only creates inter-state transitions
4. Fix `MapState` to set Main at (20, 10) and Init at (10, 100) per spec
5. Wire editability binding in ViewModel — update `IsEnabled` on mode changes
6. Remove empty `OpenStateTextEditor` relay command
7. Delete `BlueprintEditorModeOperators.cs` (unused)

### Phase 2 — Arrow Rendering & PopOver Styling

Fix the custom controls and PopOver to match spec geometry and styling.

1. Fix `ArrowLine.cs`: atan2-based perpendicular offset geometry, stroke=3, arrowhead=14x14 polygon, label font=16pt
2. Fix `LoopArrow.cs`: circle center at (stateCenterX + 60, stateCenterY - 40), arrowhead at (100, 0), label at (120, -10), correct arrowhead shape
3. Fix `EditArrowPopOverView.axaml`: white background, DropShadow LIGHTGRAY, VBox layout, padding=10
4. Fix `EditArrowPopOverView.axaml.cs`: proper positioning logic

### Phase 3 — View Template & Interaction

Fix the main AXAML template, toolbar layout, and code-behind interactions.

1. Fix state box template: CornerRadius=10 (arc 20 means diameter, so radius=10 is actually correct in Avalonia), fill colors (#90EE90, #ADD8E6, #F08080), font=16pt
2. Fix toolbar layout: toggle buttons at bottom, mode indicator panels, per spec
3. Fix code-behind: remove reflection-based tab creation, use proper service injection
4. Fix inline name editing: use proper text editing approach
5. Update integration tests for new behavior

## Risk Assessment

| Phase | Risk | Mitigation |
|-------|------|------------|
| 1 | AddTransition logic change breaks code-behind interaction | Code-behind already handles two-state clicks; ensure transition creation flow is consistent |
| 2 | Arrow geometry changes affect visual overlap with states | Test with various state positions; keep ArrowOffset=10 |
| 2 | Loop arrow circle positioning may overlap states | Circle is offset above-right; verify with 65px state height |
| 3 | Reflection removal requires service injection pattern | Use a mediator/service locator approach or pass ProjectService via ViewModel constructor |

## Test Strategy

**Unit tests**: Update `BlueprintEditorViewModelTests.cs` for new state positioning, AddTransition behavior, and editability.

**Integration tests**: Update `BlueprintCanvasUiTests.cs` for new toggle button labels, state heights, and positioning. Add tests for:
- Editability binding (states non-editable in AddTransition/RemoveState mode)
- Main/Init state default positions
- Arrow stroke widths (visual regression)

## Verification Commands

```bash
dotnet build isma-ui-dotnet.slnx
dotnet test --filter "Blueprint"
grep -r "BlueprintEditorModeOperators" src/  # should return nothing
grep -r "OpenStateTextEditor" src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs  # should show empty command removed
```
