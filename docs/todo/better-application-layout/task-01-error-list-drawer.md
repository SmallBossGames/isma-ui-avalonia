# Task 01: ErrorList as Collapsible Drawer

**Scope:** `src/ISMA.App/Views/IsmaErrorListTableView.axaml`, `src/ISMA.App/MainWindow.axaml`
**Effort:** Low
**Risk:** Very low

## Problem Description

The error list is currently a fixed 150px DataGrid embedded inline in `MainWindow.axaml` (lines 51-86). The Kotlin spec defines `ErrorListDrawer` as a `TitledPane("Error list", ismaErrorListTable)` with `isCollapsible = true` and `isExpanded = false` by default. A standalone `IsmaErrorListTableView.axaml` exists but is unused — errors are rendered inline instead. Users cannot collapse the error list to gain editor space.

## Expectations

- `IsmaErrorListTableView.axaml` becomes a reusable control that wraps its DataGrid content in a `TitledPane`
- The control is **collapsed by default** (`IsExpanded = false`)
- The control is **collapsible** (`IsCollapsible = true`)
- `MainWindow.axaml` uses the new `IsmaErrorListTableView` control instead of the inline DataGrid
- The bottom area uses a nested `BorderPane` with ErrorListDrawer on top and SimulationProcessBar below

## Implementation

### Key Design Decisions

- Use Avalonia `TitledPane` (not `Expander`) — it matches the Kotlin spec's `TitledPane` semantics and provides a header bar that can be clicked to toggle
- Keep the DataGrid content inside the TitledPane's content area
- Default state: collapsed (`IsExpanded = false`) — matches spec
- The TitledPane should have a sensible minimum height so it doesn't collapse to zero when expanded

### Implementation Steps

1. **Rewrite `IsmaErrorListTableView.axaml`** — wrap existing DataGrid in a `TitledPane` with header "Error List", set `IsCollapsible="True"`, `IsExpanded="False"`. Remove the outer `Border` wrapper since TitledPane provides its own chrome.

2. **Update `MainWindow.axaml`** — remove the inline DataGrid (lines 51-86). Replace with `<views:IsmaErrorListTableView x:Name="ErrorList" />`.

3. **Restructure bottom area** — change the `Border` with fixed `Height="180"` and `StackPanel` to a nested `BorderPane`:
   - `BorderPane.Top` = `IsmaErrorListTableView` (takes available space, can collapse)
   - `BorderPane.Bottom` = `SimulationProcessBarView` (Auto height, always visible)
   - Remove the fixed 180px height — the bottom area should size to content

### High-Level Description

**Existing code:** Inline DataGrid in MainWindow.axaml with fixed 150px height, wrapped in a 180px Border. The standalone `IsmaErrorListTableView.axaml` is unused.

**Target approach:** `IsmaErrorListTableView` becomes a TitledPane-wrapped control (collapsed by default). MainWindow bottom area uses BorderPane: ErrorListDrawer on top (collapsible, takes remaining space), ProcessBar on bottom (Auto height). This matches the Kotlin spec's nested BorderPane pattern.

## Tests

### Test Scenarios

1. **Default state** — ErrorListDrawer starts collapsed (IsExpanded = false)
2. **Toggle behavior** — clicking the TitledPane header expands/collapses the error list
3. **ProcessBar visibility** — SimulationProcessBar remains visible and accessible regardless of ErrorListDrawer state
4. **Layout adaptation** — editor area expands when ErrorListDrawer is collapsed

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test` passes
- [ ] `grep -c "TitledPane" src/ISMA.App/Views/IsmaErrorListTableView.axaml` returns >= 1
- [ ] `grep -c "IsCollapsible" src/ISMA.App/Views/IsmaErrorListTableView.axaml` returns >= 1
- [ ] No inline DataGrid remains in MainWindow.axaml (grep for `DataGrid` returns 0 in MainWindow.axaml)

## Acceptance Criteria

- [ ] ErrorListDrawer is a TitledPane with header "Error List"
- [ ] ErrorListDrawer is collapsed by default (`IsExpanded="False"`)
- [ ] ErrorListDrawer is collapsible (`IsCollapsible="True"`)
- [ ] MainWindow uses `<views:IsmaErrorListTableView>` instead of inline DataGrid
- [ ] Bottom area uses BorderPane with ErrorListDrawer on Top and ProcessBar on Bottom
- [ ] No fixed 180px height on bottom area — sizes to content
- [ ] Build succeeds with no warnings
- [ ] All existing tests pass
