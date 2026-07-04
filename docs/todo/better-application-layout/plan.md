# Better Application Layout

**Scope:** `src/ISMA.App` — MainWindow layout, views, styles
**Effort:** Medium
**Risk:** Low
**Dependencies:** None

## Problem

The current MainWindow uses a `Grid` layout with a fixed, non-collapsible structure. Compared to the Kotlin/JavaFX reference specification (`docs/isma-ui/ui-components/03-ui-layout.md`), the Avalonia implementation has several layout deficiencies:

1. **ErrorList is not collapsible** — it's a fixed 150px DataGrid embedded in the bottom border. The spec defines `ErrorListDrawer` as a `TitledPane` with `isCollapsible = true`, `isExpanded = false` by default. A standalone `IsmaErrorListTableView.axaml` exists but is unused.
2. **SettingsPanel has no toggle** — it's a fixed 240px right column always visible. The `MainWindowViewModel` has an unbound `ShowSettings` property. The spec shows the settings panel as a right-side region that should be toggleable.
3. **ToolBar uses text buttons** — the spec defines Material Design glyphs (e.g. `add_circle_outline`, `folder_open`). Current buttons show text labels like "New Text", "Open".
4. **Grid instead of BorderPane** — functionally equivalent but architecturally diverges from the spec's `BorderPane` with `top`/`center`/`right`/`bottom` regions.
5. **Bottom area uses fixed-height Border** — the spec uses a nested `BorderPane` with `ErrorListDrawer` (top) + `SimulationProcessBar` (bottom), allowing the error list to expand/shrink independently.

## Scope Analysis

### Files to Modify

| File | Change |
|------|--------|
| `src/ISMA.App/MainWindow.axaml` | Rewrite layout from Grid to BorderPane; integrate ErrorListDrawer; add SettingsPanel toggle; change bottom to nested BorderPane |
| `src/ISMA.App/MainWindow.axaml.cs` | Update name references for new controls |
| `src/ISMA.App/MainWindowViewModel.cs` | Wire `ShowSettings` property to SettingsPanel visibility; ensure ErrorListDrawer state binding |
| `src/ISMA.App/Views/IsmaToolBarView.axaml` | Replace text buttons with icon buttons using Material Design glyphs |
| `src/ISMA.App/Views/IsmaErrorListTableView.axaml` | Wrap in TitledPane to create ErrorListDrawer behavior; make it reusable |
| `src/ISMA.App/Styles/GlobalStyles.axaml` | Add styles for TitledPane drawer, icon button states, settings panel visibility transitions |

### Files to Delete

| File | Reason |
|------|--------|
| *(none)* | The inline DataGrid in MainWindow.axaml will be replaced, but no standalone file deletion needed |

### Files to Update (callers)

| File | Change |
|------|--------|
| `src/ISMA.App/ServiceCollectionExtensions.cs` | No changes needed — DI is layout-agnostic |
| `tests/ISMA.Tests.Integration/` | Integration tests reference `MainWindow` via `AutomationProperties.AutomationId`; update IDs if changed |

## Dependency Graph

```
Phase 0 (parallel):
  Task 1 — ErrorListDrawer (collapsible) ──┐
  Task 2 — SettingsPanel toggle ───────────┼─→ Phase 1
  Task 3 — Icon toolbar buttons ───────────┘

Phase 1 (sequential):
  Task 4 — MainWindow BorderPane rewrite (integrates Tasks 1-3)
```

## Execution Phases

### Phase 0: Build independent UI components
- Task 1 — ErrorListDrawer (collapsible TitledPane wrapper) — Low
- Task 2 — SettingsPanel toggle (binding + visibility) — Low
- Task 3 — Icon toolbar buttons (Material Design glyphs) — Low

### Phase 1: Integrate into MainWindow
- Task 4 — Rewrite MainWindow to BorderPane layout with all components — Medium

## Recommended Commit Sequence

1. `git commit -m "feat: make ErrorList a collapsible TitledPane drawer"`
2. `git commit -m "feat: add SettingsPanel visibility toggle binding"`
3. `git commit -m "feat: replace toolbar text buttons with Material Design icon buttons"`
4. `git commit -m "refactor: rewrite MainWindow as BorderPane layout matching Kotlin spec"`

## Parallelization

- Tasks 1, 2, 3 can run in parallel (independent view changes)
- Task 4 depends on Tasks 1-3 (integrates all components)

## Risk Assessment

| Phase | Risk | Mitigation |
|-------|------|------------|
| Phase 0 | Low | Each task modifies a single view file; build fails immediately if any issue |
| Phase 1 | Low | BorderPane is a direct 1:1 replacement for the Grid; no logic changes, only layout restructure |

## Tests

Integration tests in `ISMA.Tests.Integration` use `AutomationProperties.AutomationId` to find UI elements (`EditorTabPane`, `ErrorList`). After Task 4, these IDs may need updating if control names change. No new tests required — layout changes are visual, not behavioral.

## Verification

```bash
# Build command
dotnet build isma-ui-dotnet.slnx

# Test command
dotnet test

# Pattern checks to verify no regressions
grep -r "AutomationProperties.AutomationId" src/ISMA.App/
grep -r "RowDefinitions" src/ISMA.App/MainWindow.axaml  # should return 0 after Task 4
grep -r "ColumnDefinitions" src/ISMA.App/MainWindow.axaml  # should return 0 after Task 4
```
