# Blueprint Editor Refactoring

**Scope:** `ISMA.ViewModels` + `ISMA.App` (blueprint editor area)
**Effort:** High
**Risk:** Medium
**Dependencies:** None

## Problem

The JavaFX blueprint editor uses **5 layers**: `Model → CanvasViewModel → ViewModel → ViewAdapter → View`. The Avalonia version collapsed to **2 layers**: `Model → ViewModel → View`, losing the adapter and canvas tracking, and dumping 581 lines of logic into code-behind. This results in:

1. A **581-line code-behind file** that violates the project's "no XAML code-behind logic" rule
2. A **655-line ViewModel** that mixes business logic, UI concerns, and data management
3. **No adapter pattern** — the ViewModel and View are coupled through `DataContext` discovery
4. **No `CanvasViewModel` equivalent** — no tracking of editor-only runtime data, leading to stale references when states are removed
5. **State removal doesn't cascade** — removing a state leaves orphaned transaction/loop references
6. **StateBox is a plain Border** — all interaction logic (drag, click, inline edit) lives in code-behind
7. **Mode state uses enum + 4 parallel booleans** instead of a sealed class — redundant state
8. **State text edits are lost** on tab close (no sync back from text editor)
9. **Loop arrow double-click** not implemented
10. **Toolbar always visible** — no tab-based visibility

## Scope Analysis

### Files to Modify

| File | Change |
|------|--------|
| `src/ISMA.ViewModels/ViewModels/BlueprintEditorMode.cs` | Replace enum with sealed class hierarchy |
| `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` | Full rewrite — remove code-behind logic, fix cascade, use sealed modes |
| `src/ISMA.ViewModels/ViewModels/BlueprintStateViewModel.cs` | Minor updates |
| `src/ISMA.ViewModels/ViewModels/BlueprintTransactionViewModel.cs` | Minor updates |
| `src/ISMA.ViewModels/ViewModels/BlueprintLoopTransactionViewModel.cs` | Minor updates |
| `src/ISMA.ViewModels/ViewModels/BlueprintProjectViewModel.cs` | Update for new ViewModel structure |
| `src/ISMA.App/Views/BlueprintEditorView.axaml` | Simplify — remove code-behind event handlers |
| `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` | Delete (581 lines → 0) |
| `src/ISMA.App/Controls/ArrowLine.cs` | Minor updates (no breaking changes) |
| `src/ISMA.App/Controls/LoopArrow.cs` | Minor updates (no breaking changes) |
| `tests/ISMA.Tests.Integration/BlueprintEditorTests.cs` | Update for sealed mode class |
| `tests/ISMA.Tests.Integration/BlueprintCanvasUiTests.cs` | Update for new interaction model |
| `tests/ISMA.Tests.Integration/UseCases/Blueprint/BlueprintAuthoringTests.cs` | Update for sealed mode class |
| `tests/ISMA.Tests.Integration/InlineNameEditUiTests.cs` | Update for new inline edit approach |

### Files to Create

| File | Purpose |
|------|---------|
| `src/ISMA.ViewModels/ViewModels/BlueprintCanvasViewModel.cs` | Editor-only data tracking (new, replaces implicit tracking) |

### Files to Delete

| File | Reason |
|------|--------|
| `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` | All logic moved to ViewModel (581 lines) |

## Dependency Graph

```
Phase 0 (parallel, no breaking changes):
  Task 1 ──┐
  Task 2 ──┼─→ Phase 1
  Task 3 ──┘

Phase 1 (sequential, ViewModel rewrite):
  Task 4 → Task 5

Phase 2 (sequential, View cleanup):
  Task 6 → Task 7

Phase 3 (tests):
  Task 8 → Task 9
```

## Execution Phases

### Phase 0: Foundation — New types and controls (parallel)
- Task 1 — `BlueprintCanvasViewModel` — Medium
- Task 2 — `BlueprintEditorMode` sealed class — Low
- Task 3 — `StateBox` control — Medium

### Phase 1: ViewModel Rewrite
- Task 4 — Fix state removal cascade bug — Low
- Task 5 — `BlueprintEditorViewModel` full rewrite — High

### Phase 2: View Cleanup
- Task 6 — Move code-behind logic to ViewModel — High
- Task 7 — Delete code-behind + simplify AXAML — Medium

### Phase 3: Tests
- Task 8 — Update existing tests for new types — Medium
- Task 9 — New tests for fixed behaviors — Medium

## Recommended Commit Sequence

1. Commit Phase 0 tasks (all compile, no breaking changes to callers)
2. Commit Task 4 (fixes a real bug, small scope)
3. Commit Task 5 (big rewrite, but tests catch regressions)
4. Commit Task 6 + 7 together (view cleanup)
5. Commit Task 8 + 9 together (test updates)

## Parallelization

- Tasks 1, 2, 3 are fully independent — can be done in parallel
- Task 4 is a prerequisite for Task 5 (cascade fix is part of the rewrite)
- Task 6 depends on Task 5 (needs the new ViewModel to move logic into)
- Task 8 depends on Tasks 1, 2, 5 (test signatures change)
- Task 9 can run in parallel with Task 8

## Risk Assessment

| Phase | Risk | Mitigation |
|-------|------|------------|
| Phase 0 | Low | New types are additive — no existing callers break. Build fails immediately if any issue. |
| Phase 1 | Medium | Big ViewModel rewrite. Risk of introducing new bugs. Mitigation: existing tests catch regressions. |
| Phase 2 | Medium | Deleting 581-line code-behind. Risk of losing interaction logic. Mitigation: Task 6 moves logic first, then Task 7 deletes. |
| Phase 3 | Low | Test updates are mechanical. Build + test run validates everything. |

## Tests

**Existing tests that need updating:**
- `BlueprintEditorTests.cs` — mode enum → sealed class, cascade behavior
- `BlueprintCanvasUiTests.cs` — interaction model changes
- `BlueprintAuthoringTests.cs` — mode enum → sealed class, cascade behavior
- `InlineNameEditUiTests.cs` — new inline edit approach via StateBox control

**New tests needed:**
- State removal cascades to transactions (Integration)
- State removal cascades to loops (Integration)
- State text syncs back from editor tab (Integration)
- Loop arrow double-click opens editor tab (Integration)
- Toolbar visibility based on tab selection (Integration)
- StateBox control: drag, click, inline edit, double-click (Integration)
- Mode sealed class: type-safe transitions (Unit)

## Verification

```bash
# Build
dotnet build isma-ui-dotnet.slnx

# All tests
dotnet test isma-ui-dotnet.slnx

# Verify no code-behind in BlueprintEditorView
grep -c "private void" src/ISMA.App/Views/BlueprintEditorView.axaml.cs
# Should return 0 or file not found after Task 7

# Verify sealed class pattern
grep -r "sealed class.*BlueprintEditorMode" src/ISMA.ViewModels/

# Verify CanvasViewModel exists
grep -r "class BlueprintCanvasViewModel" src/ISMA.ViewModels/

# Verify no DataContext discovery in code-behind
grep -r "DataContext is BlueprintEditorViewModel" src/ISMA.App/Views/
# Should return 0 or file not found
```
