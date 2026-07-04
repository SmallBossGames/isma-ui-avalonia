# Task 04: MainWindow BorderPane Layout Rewrite

**Scope:** `src/ISMA.App/MainWindow.axaml`, `src/ISMA.App/MainWindow.axaml.cs`
**Effort:** Medium
**Risk:** Low

## Problem Description

MainWindow uses a `Grid` with `RowDefinitions="Auto,*,Auto"` and an inner `Grid ColumnDefinitions="*,240"` for the main area. The Kotlin spec uses a `BorderPane` with four distinct regions: `top` (menu + toolbar), `center` (editor), `right` (settings panel), `bottom` (nested BorderPane with error list + process bar). The Grid approach works but is architecturally divergent from the spec and makes it harder to reason about region sizing. Converting to BorderPane matches the spec and makes the layout more maintainable.

## Expectations

- MainWindow layout uses `BorderPane` instead of `Grid`
- Four regions: `top`, `center`, `right`, `bottom`
- `top`: VBox with MenuBar + ToolBar
- `center`: EditorTabPaneView (fills remaining space)
- `right`: SettingsPanelView in ScrollViewer (240px, toggleable via Task 02)
- `bottom`: Nested BorderPane with ErrorListDrawer (Top) + ProcessBar (Bottom)
- All AutomationProperties.AutomationId values preserved for integration tests
- All existing bindings preserved

## Implementation

### Key Design Decisions

- BorderPane is a native Avalonia layout panel — no additional packages needed
- The nested BorderPane in the bottom region matches the Kotlin spec exactly
- SettingsPanel binding to `ShowSettings` (from Task 02) is integrated here
- ErrorListDrawer (from Task 01) is placed in `BorderPane.Top` of the bottom region
- ProcessBar is placed in `BorderPane.Bottom` of the bottom region

### Implementation Steps

1. **Rewrite MainWindow.axaml root** — change from `<Grid RowDefinitions="Auto,*,Auto">` to `<BorderPane>`:

```xml
<BorderPane xmlns="https://github.com/avaloniaui"
            ...>
    <!-- Top: MenuBar + ToolBar -->
    <BorderPane.Top>
        <StackPanel>
            <views:IsmaMenuBarView x:Name="MenuBar" />
            <views:IsmaToolBarView x:Name="ToolBar" />
        </StackPanel>
    </BorderPane.Top>

    <!-- Center: Editor -->
    <BorderPane.Center>
        <views:EditorTabPaneView x:Name="EditorTabPane" AutomationProperties.AutomationId="EditorTabPane" />
    </BorderPane.Center>

    <!-- Right: Settings Panel -->
    <BorderPane.Right>
        <ScrollViewer x:Name="SettingsPanel"
                      IsVisible="{Binding ShowSettings}"
                      VerticalScrollBarVisibility="Auto"
                      HorizontalScrollBarVisibility="Disabled">
            <ContentControl Content="{Binding SimulationParameters}">
                <ContentControl.DataTemplates>
                    <DataTemplate x:DataType="vm:SimulationParametersViewModel">
                        <views:SettingsPanelView />
                    </DataTemplate>
                </ContentControl.DataTemplates>
            </ContentControl>
        </ScrollViewer>
    </BorderPane.Right>

    <!-- Bottom: ErrorList + ProcessBar -->
    <BorderPane.Bottom>
        <BorderPane Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
                    BorderThickness="0,1,0,0"
                    BorderBrush="{DynamicResource SystemControlForegroundBaseMediumLowBrush}">
            <BorderPane.Top>
                <views:IsmaErrorListTableView x:Name="ErrorList" AutomationProperties.AutomationId="ErrorList" />
            </BorderPane.Top>
            <BorderPane.Bottom>
                <views:SimulationProcessBarView x:Name="ProcessBar" />
            </BorderPane.Bottom>
        </BorderPane>
    </BorderPane.Bottom>
</BorderPane>
```

2. **Update MainWindow.axaml.cs** — check that all `x:Name` references in the code-behind still match the new control names. The names (`MenuBar`, `ToolBar`, `EditorTabPane`, `SettingsPanel`, `ErrorList`, `ProcessBar`) remain the same, so no changes should be needed.

3. **Preserve window properties** — ensure `Title`, `MinWidth`, `MinHeight`, `KeyBindings`, and all other window-level properties are preserved.

### High-Level Description

**Existing code:** Grid with 3 rows. Row 0 = menu/toolbar. Row 1 = inner Grid with editor (column 0) + settings (column 1, 240px). Row 2 = Border with fixed 180px containing inline DataGrid (150px) + ProcessBar.

**Target approach:** BorderPane with 4 regions. Top = menu/toolbar. Center = editor (fills space). Right = settings (240px, toggleable). Bottom = nested BorderPane with ErrorListDrawer (collapsible, Top) + ProcessBar (Auto, Bottom). No fixed heights — everything sizes to content.

## Tests

### Test Scenarios

1. **Layout regions** — all 4 BorderPane regions are present and correctly assigned
2. **Editor fills space** — editor area expands to fill available space when settings panel is hidden
3. **Settings toggle** — settings panel appears/disappears correctly (integration with Task 02)
4. **Error list collapse** — error list collapses/expands without affecting process bar (integration with Task 01)
5. **Window minimum size** — window respects MinWidth=500, MinHeight=600
6. **Automation IDs** — `EditorTabPane` and `ErrorList` AutomationIds preserved for integration tests

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test` passes
- [ ] `grep -c "BorderPane" src/ISMA.App/MainWindow.axaml` returns >= 5 (root + nested)
- [ ] `grep -c "RowDefinitions\|ColumnDefinitions" src/ISMA.App/MainWindow.axaml` returns 0
- [ ] `grep "AutomationProperties.AutomationId" src/ISMA.App/MainWindow.axaml` returns EditorTabPane and ErrorList
- [ ] `grep "x:Name" src/ISMA.App/MainWindow.axaml` returns all 6 named controls

## Acceptance Criteria

- [ ] MainWindow uses BorderPane as the root layout panel
- [ ] BorderPane.Top contains MenuBar + ToolBar in a StackPanel
- [ ] BorderPane.Center contains EditorTabPaneView
- [ ] BorderPane.Right contains SettingsPanel ScrollViewer with IsVisible binding
- [ ] BorderPane.Bottom contains nested BorderPane
- [ ] Nested BorderPane.Top contains IsmaErrorListTableView
- [ ] Nested BorderPane.Bottom contains SimulationProcessBarView
- [ ] No Grid RowDefinitions/ColumnDefinitions in MainWindow.axaml
- [ ] All 6 x:Name references preserved (MenuBar, ToolBar, EditorTabPane, SettingsPanel, ErrorList, ProcessBar)
- [ ] AutomationProperties.AutomationId values preserved for EditorTabPane and ErrorList
- [ ] All keybindings preserved
- [ ] Build succeeds with no warnings
- [ ] All existing tests pass
