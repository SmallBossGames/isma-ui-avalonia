# Task 02: SettingsPanel Visibility Toggle

**Scope:** `src/ISMA.App/MainWindow.axaml`, `src/ISMA.App/MainWindowViewModel.cs`
**Effort:** Low
**Risk:** Very low

## Problem Description

The SettingsPanel is a fixed 240px right column always visible in MainWindow. The `MainWindowViewModel` has a `ShowSettings` property that is never bound to anything — it's dead code. The Kotlin spec shows the settings panel as a right-side region of the BorderPane, implying it should be toggleable to give more space to the editor area.

## Expectations

- SettingsPanel visibility is bound to `ShowSettings` property in MainWindowViewModel
- A UI control (toggle button in the toolbar or menu item) toggles the panel visibility
- When hidden, the editor area expands to fill the full width (no empty 240px gap)
- When shown, the SettingsPanel occupies 240px on the right as before

## Implementation

### Key Design Decisions

- Bind SettingsPanel `IsVisible` to `ShowSettings` using compiled binding with `Converter` for boolean-to-visibility
- Add a toggle button in the toolbar next to the existing controls, or use a menu item under a View menu
- Use Avalonia's built-in `BooleanToVisibilityConverter` or a custom `BoolToVisibilityConverter` (check if one already exists in Converters/)

### Implementation Steps

1. **Check for existing converter** — look in `src/ISMA.App/Converters/` for a boolean-to-visibility converter. If none exists, create `BoolToVisibilityConverter.cs` that maps `true` → `Visible`, `false` → `Collapsed`.

2. **Register converter in App.axaml** — add the converter as a static resource if newly created.

3. **Wire up `ShowSettings` in MainWindowViewModel** — ensure `ShowSettings` is an `[ObservableProperty]` with a toggle command. The property should default to `true` (panel visible).

4. **Add toggle control** — add a toggle button in `IsmaToolBarView.axaml` (e.g. a `ToggleButton` with a panel/eye icon and tooltip "Toggle Settings Panel") bound to `ShowSettings`.

5. **Bind SettingsPanel visibility** — in `MainWindow.axaml`, bind the SettingsPanel `IsVisible` property to `ShowSettings` using the converter.

### High-Level Description

**Existing code:** `MainWindowViewModel.ShowSettings` exists but is unbound. SettingsPanel in MainWindow.axaml has no visibility binding — always visible at 240px width.

**Target approach:** `ShowSettings` is toggled via a toolbar button. SettingsPanel `IsVisible` is compiled-bound to `ShowSettings` with a converter. When hidden, the Grid column for SettingsPanel collapses, giving full width to the editor.

## Tests

### Test Scenarios

1. **Default state** — SettingsPanel is visible on app start (`ShowSettings = true`)
2. **Toggle off** — clicking the toggle button hides the SettingsPanel; editor area expands
3. **Toggle on** — clicking again restores the SettingsPanel at 240px
4. **ViewModel state** — `ShowSettings` correctly reflects the panel visibility state

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test` passes
- [ ] `grep "ShowSettings" src/ISMA.App/MainWindow.axaml` returns the binding
- [ ] `grep "ShowSettings" src/ISMA.App/Views/IsmaToolBarView.axaml` returns the toggle button binding
- [ ] No unbound `ShowSettings` references remain in MainWindow.axaml

## Acceptance Criteria

- [ ] `ShowSettings` property in MainWindowViewModel is `[ObservableProperty]`
- [ ] SettingsPanel `IsVisible` is bound to `ShowSettings` with visibility converter
- [ ] Toggle button exists in toolbar with appropriate icon and tooltip
- [ ] Toggle button `IsChecked` is bound to `ShowSettings` (two-way)
- [ ] Default panel state is visible
- [ ] When hidden, editor area fills full width (no 240px gap)
- [ ] Build succeeds with no warnings
- [ ] All existing tests pass
