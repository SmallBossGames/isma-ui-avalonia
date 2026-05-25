# Missing Features & Implementation Plan

This document identifies what's missing from the original ISMA JavaFX application that hasn't been implemented in the Avalonia migration yet, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

| Status | Count |
|--------|-------|
| Fully Implemented | 17 |
| Partially Implemented | 9 |
| Not Implemented | 2 |

---

## Task 1: Blueprint Canvas Rendering (Arrows, Drag, Inline Editing)

**Priority:** Critical
**Phase:** 9 (Blueprint Editor)
**Complexity:** High

### What's Missing

The `BlueprintEditorView.axaml` has the skeleton but is missing the core visual interactions:

1. **Transition arrow rendering** - States are rendered but arrows/lines between states are NOT drawn on the canvas
2. **Loop arrow rendering** - Circle arrows for loop transactions are missing
3. **State drag support** - States cannot be dragged (no `PointerPressed`/`PointerMoved` handlers)
4. **Arrow click detection** - Cannot click on arrows to select them or open the edit popover
5. **Edit popover wiring** - `EditArrowPopup` exists but has no `Content` set; never displays
6. **Inline state name editing** - State names are plain `TextBlock`, no double-click to rename
7. **State selection visual feedback** - No highlight/selection indicator on selected states

### Implementation Plan

#### 1.1 Custom Canvas with DrawingContext rendering

Create a custom `BlueprintCanvas` control (extends `Panel`) that overrides `OnRender(DrawingContext)` to draw:
- Arrow lines between states (straight lines with perpendicular offset)
- Arrowheads (14x14 isosceles triangles rotated to match line angle)
- Loop arrows (circles with radius 40, arrowheads pointing back)
- Labels (alias if present, else predicate)

#### 1.2 State drag implementation

Add pointer event handlers to state borders:
- `PointerPressed` - Start drag, record offset from state center
- `PointerMoved` - Update state position (clamp to >= 0)
- `PointerReleased` - End drag, trigger canvas re-render

#### 1.3 Arrow hit testing

Implement `HitTestArrow(Point point)` to detect clicks on arrow lines:
- Calculate perpendicular distance from point to line segment
- If distance < threshold (e.g., 8px), return the transaction
- Distinguish between arrow body clicks (select/remove) and arrowhead clicks (open popover)

#### 1.4 Edit popover wiring

Set `EditArrowPopup.Content` to an instance of `EditArrowPopOverView` with proper DataContext binding to `EditArrowPopOverViewModel`.

#### 1.5 Inline name editing

Replace state name `TextBlock` with a `TextBox` overlay that appears on single-click (with 200ms `DispatcherTimer` delay to disambiguate from drag).

### Acceptance Checklist

- [ ] Arrows render as straight lines between state centers with 10px perpendicular offset
- [ ] Arrowheads are 14x14 triangles rotated to match line angle
- [ ] Loop arrows render as circles (radius 40) with arrowheads pointing back to state
- [ ] Arrow labels display alias (if present) or predicate text
- [ ] States are draggable with mouse (press → move → release)
- [ ] Dragging updates arrow positions in real-time
- [ ] Arrow body click selects the arrow (highlighted)
- [ ] Arrowhead click opens EditArrowPopOver at click position
- [ ] EditArrowPopOver displays with Alias and Predicate fields
- [ ] Popover dismisses on PointerExited or Cancel button
- [ ] Single-click on state name (200ms delay) opens inline TextBox
- [ ] Duplicate names are prevented by NameChangingMonitor
- [ ] Duplicate names rollback to previous value
- [ ] No state can have negative coordinates after drag
- [ ] Arrows update position when dragged states move
- [ ] Loop arrow text editor opens with "(loop)" suffix on double-click arrowhead

### Tests

- [ ] `BlueprintCanvasTests_ArrowsRenderCorrectly` - Verify arrow geometry between states
- [ ] `BlueprintCanvasTests_LoopArrowsRenderCorrectly` - Verify loop arrow circle + arrowhead
- [ ] `BlueprintCanvasTests_StateDragUpdatesPosition` - Verify position changes on drag
- [ ] `BlueprintCanvasTests_ArrowHitTest` - Verify arrow body vs arrowhead detection
- [ ] `BlueprintCanvasTests_InlineNameEdit` - Verify name editing flow

---

## Task 2: PropertiesGrid Enum/ComboBox Support

**Priority:** Medium
**Phase:** 7 (Settings Panel)
**Complexity:** Low

### What's Missing

The `PropertiesGrid` control auto-detects properties via reflection and creates appropriate controls. It handles `bool` → CheckBox, `string`/`int`/`double` → TextBox, but:

1. **Enum types fall through to TextBox** - No ComboBox for enum properties
2. **No explicit TwoWay binding mode** - TextBox defaults to OneWay for Text property
3. **No numeric validation** - Plain TextBox accepts any text

### Implementation Plan

Modify `PropertiesGrid.axaml.cs` to:
1. Detect `Type.IsEnum` and create a `ComboBox` with enum values
2. Set `Binding.Mode = BindingMode.TwoWay` for all bindings
3. Add `TextBox.InputRejected` handler for numeric fields to reject non-numeric input

### Acceptance Checklist

- [ ] Enum properties (e.g., `SavingTarget`) render as ComboBox
- [ ] ComboBox shows all enum values as selectable items
- [ ] ComboBox selection updates ViewModel via TwoWay binding
- [ ] All TextBox bindings are explicitly TwoWay
- [ ] Numeric TextBox rejects non-numeric input
- [ ] Settings panel correctly displays and edits `SavingTarget` enum

### Tests

- [ ] `PropertiesGridTests_EnumProperty_CreatesComboBox` - Verify enum → ComboBox
- [ ] `PropertiesGridTests_TwoWayBinding_UpdatesViewModel` - Verify TwoWay mode
- [ ] `PropertiesGridTests_NumericInput_RejectsInvalid` - Verify numeric validation

---

## Task 3: MenuBar SaveAs + Keyboard Shortcut Fix

**Priority:** Medium
**Phase:** 7 (Menu Bar)
**Complexity:** Low

### What's Missing

1. **SaveAs menu item** - `MainWindowViewModel` has no `SaveAsCommand`; menu item is missing
2. **Ctrl+W conflict** - Both `Close` and `Exit` use `Ctrl+W`

### Implementation Plan

1. Add `SaveAsCommand` to `MainWindowViewModel`:
   ```csharp
   [RelayCommand]
   private async Task SaveAs()
   {
       if (ActiveProject is not null)
           await _projectFileService.SaveAsAsync(ActiveProject);
   }
   ```
2. Add "Save As..." menu item to `IsmaMenuBarView.axaml` with `Ctrl+Shift+S`
3. Change `Exit` keyboard shortcut from `Ctrl+W` to `Ctrl+Q` (or remove explicit shortcut, rely on Alt+F4)

### Acceptance Checklist

- [ ] SaveAs command exists in MainWindowViewModel
- [ ] SaveAs menu item appears in File menu with Ctrl+Shift+S
- [ ] SaveAs opens file dialog and saves current project
- [ ] Exit no longer conflicts with Close (Ctrl+W → Ctrl+Q)
- [ ] All menu commands execute correctly

### Tests

- [ ] `MenuBarTests_SaveAsCommand_Exists` - Verify command exists
- [ ] `MainWindowIntegrationTests_SaveAs_SavesWithDialog` - End-to-end save as flow

---

## Task 4: Window State Persistence

**Priority:** High
**Phase:** 8 (Window Management)
**Complexity:** Low

### What's Missing

`WindowPreferences` model and `PreferencesProvider.CommitWindow()` exist but are never called:

1. **No geometry save on window close** - `MainWindow.axaml.cs` doesn't override `OnClosing`
2. **No geometry restore on startup** - Window opens with default position/size

### Implementation Plan

1. In `MainWindow.axaml.cs`, override `OnClosing` to save geometry:
   ```csharp
   protected override void OnClosing(CancelEventArgs e)
   {
       var prefs = new WindowPreferences
       {
           X = Position.X,
           Y = Position.Y,
           Width = Bounds.Width,
           Height = Bounds.Height,
           IsMaximized = IsMaximized
       };
       _preferencesProvider.CommitWindow(prefs);
       base.OnClosing(e);
   }
   ```
2. In `MainWindow` constructor, restore geometry from `PreferencesProvider` before showing
3. Inject `IPreferencesProvider` into `MainWindow` via constructor

### Acceptance Checklist

- [ ] Window position/size saved on close
- [ ] Window restored to last position/size on next launch
- [ ] Maximized state persisted and restored
- [ ] Window minimum size (500x600) still enforced
- [ ] Window geometry works across multiple sessions

### Tests

- [ ] `WindowPersistenceTests_GeometrySavedOnClose` - Verify save
- [ ] `WindowPersistenceTests_GeometryRestoredOnStartup` - Verify restore

---

## Task 5: Last Opened Files Restoration

**Priority:** High
**Phase:** 8 (File Operations)
**Complexity:** Low

### What's Missing

`DefaultFilesPreferences.LastOpenedProjectPath` exists but is never read/written:

1. **No file history tracking** - `ProjectService` doesn't track opened files
2. **No restoration on startup** - `MainWindowViewModel` doesn't load last opened paths

### Implementation Plan

1. In `ProjectService.Open()`, call `_preferencesProvider.CommitFiles()` with updated path list
2. In `MainWindowViewModel` constructor, load `LastOpenedProjectPath` and auto-open each file
3. Limit to last 5 files to avoid excessive storage

### Acceptance Checklist

- [ ] Opened file paths tracked in preferences
- [ ] Last opened files restored on app startup
- [ ] Maximum 5 files in history
- [ ] Deleted files are removed from history
- [ ] Auto-open doesn't block app startup

### Tests

- [ ] `FileHistoryTests_PathsSavedOnOpen` - Verify tracking
- [ ] `FileHistoryTests_PathsRestoredOnStartup` - Verify restoration

---

## Task 6: EditorPlatformService AttachToWindow

**Priority:** Medium
**Phase:** 8 (Text Editor)
**Complexity:** Low

### What's Missing

`EditorPlatformService` exists with `AttachToWindow()` method but it's never called:

1. **Key bindings not attached** - Ctrl+X/C/V at window level don't propagate to focused editor
2. **`SetFocusedEditor` never called** - Service doesn't know which editor is active

### Implementation Plan

1. In `MainWindow.axaml.cs` constructor, after setting DataContext:
   ```csharp
   _editorPlatformService.AttachToWindow(this);
   ```
2. In `EditorTabPaneView`, detect active tab change and call `_editorPlatformService.SetFocusedEditor(editor)`
3. In `IsmaTextEditorView`, register itself with `EditorPlatformService` on load and unregister on unload

### Acceptance Checklist

- [ ] Ctrl+X/C/V work from any focus (not just editor focus)
- [ ] Cut/Copy/Paste menu items work when editor has focus
- [ ] Toolbar cut/copy/paste buttons work
- [ ] Switching tabs updates the focused editor reference

### Tests

- [ ] `EditorPlatformTests_CutCopyPasteFromWindow` - Verify key propagation
- [ ] `EditorPlatformTests_TabSwitch_UpdatesFocusedEditor` - Verify tab change handling

---

## Task 7: NumericCell Custom DataGrid Cell

**Priority:** Low
**Phase:** 7 (Error List)
**Complexity:** Low

### What's Missing

The ErrorList DataGrid uses plain `TextBlock` for Row/Position columns. No custom numeric cell exists:

1. **No `NumericCell` control** - Should format as integer with validation
2. **ErrorList uses inline DataGrid** - The `IsmaErrorListTableView` control exists but is orphaned

### Implementation Plan

1. Create `NumericCell` control (extends `TextBox`) for DataGrid template columns
2. Format as integer, reject non-numeric input
3. Replace inline DataGrid in `MainWindow.axaml` with `IsmaErrorListTableView` control

### Acceptance Checklist

- [ ] Row and Position columns display as formatted integers
- [ ] NumericCell rejects non-numeric input in editable mode
- [ ] IsmaErrorListTableView used in MainWindow
- [ ] ErrorList columns: Row (5%), Position (5%), Fragment (10%), Message (80%)

### Tests

- [ ] `NumericCellTests_FormatsAsInteger` - Verify formatting
- [ ] `NumericCellTests_RejectsNonNumeric` - Verify validation

---

## Task 8: SimulationParametersService Commit/Snapshot Fix

**Priority:** High
**Phase:** 5 (App Services)
**Complexity:** Medium

### What's Missing

The App layer `SimulationParametersService` has critical gaps:

1. **`Commit()` is a no-op** - Doesn't apply loaded parameters to ViewModels
2. **`Snapshot()` returns hardcoded defaults** - Doesn't reflect actual current settings
3. **No bridge to `SimulationParametersViewModel`** - ViewModel has its own snapshot/commit that's separate

### Implementation Plan

1. Fix `Snapshot()` to read from `SimulationParametersViewModel`:
   ```csharp
   public SimulationParameters Snapshot()
   {
       return new SimulationParameters
       {
           CauchyInitials = new CauchyInitials
           {
               StartTime = _parametersVm.CauchyInitials.StartTime,
               EndTime = _parametersVm.CauchyInitials.EndTime,
               InitialStep = _parametersVm.CauchyInitials.InitialStep
           },
           // ... other sections
       };
   }
   ```
2. Implement `Commit()` to apply loaded parameters to `SimulationParametersViewModel`:
   ```csharp
   public void Commit(SimulationParameters parameters)
   {
       _parametersVm.Commit(parameters);
   }
   ```
3. Wire `StoreSettings` and `LoadSettings` commands in `MainWindowViewModel` to use the service properly

### Acceptance Checklist

- [ ] Snapshot() returns current ViewModel values (not defaults)
- [ ] Commit() applies loaded parameters to all ViewModel sections
- [ ] Store Settings saves current values to JSON file
- [ ] Load Settings loads values from JSON file and applies to UI
- [ ] All 5 parameter sections (Cauchy, Integration, Event Detection, Result Saving, Result Processing) are saved/restored

### Tests

- [ ] `SimulationParametersServiceTests_Snapshot_ReturnsCurrentValues` - Verify snapshot
- [ ] `SimulationParametersServiceTests_Commit_AppiesToViewModel` - Verify commit
- [ ] `IntegrationTests_StoreLoadSettings_RoundTrip` - End-to-end store/load flow

---

## Task 9: ViewLocator Pattern (Optional but Recommended)

**Priority:** Low
**Phase:** 0 (Foundation)
**Complexity:** Low

### What's Missing

No `ViewLocator` exists. The app uses manual DI construction for `MainWindow` but other views are resolved via `ContentControl` with `DataContext` binding.

### Implementation Plan

Create a simple `ViewLocator` that resolves views from ViewModel types:
```csharp
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        var typeName = param?.GetType().FullName ?? "";
        var viewType = typeName.Replace("ViewModel", "View");
        return Type.GetType(viewType)?.CreateInstance() as Control;
    }
    
    public Control? CreateContent() => Build(null);
}
```

Add to `App.axaml` resources and use `ContentControl.ContentTemplate` with `DataTemplate` binding.

### Acceptance Checklist

- [ ] ViewLocator resolves `MainWindowViewModel` → `MainWindowView`
- [ ] ViewLocator resolves `SettingsPanelViewModel` → `SettingsPanelView`
- [ ] All ViewModels have matching Views
- [ ] Fallback to empty control for unmatched types

### Tests

- [ ] `ViewLocatorTests_ResolvesKnownTypes` - Verify resolution

---

## Task 10: Duplication Prevention for Transitions and Loops

**Priority:** Medium
**Phase:** 9 (Blueprint Editor)
**Complexity:** Medium

### What's Missing

State name uniqueness is enforced, but:

1. **No duplicate transition prevention** - Multiple transitions with same start/end/predicate can be created
2. **No duplicate loop prevention** - Multiple loop transactions on the same state can be created

### Implementation Plan

1. In `BlueprintEditorViewModel.AddTransition()`, check for existing transition with same start/end/predicate
2. In `BlueprintEditorViewModel.AddLoop()`, check for existing loop on the same state
3. Show user-friendly message when duplicate is detected
4. Update `BlueprintToLismaConverter` to handle duplicates gracefully (merge or skip)

### Acceptance Checklist

- [ ] Adding duplicate transition (same start, end, predicate) shows error message
- [ ] Adding duplicate loop on same state shows error message
- [ ] Converter handles existing duplicates in loaded blueprints
- [ ] Error messages are user-friendly

### Tests

- [ ] `BlueprintEditorTests_DuplicateTransition_Rejected` - Verify prevention
- [ ] `BlueprintEditorTests_DuplicateLoop_Rejected` - Verify prevention
- [ ] `BlueprintToLismaConverterTests_DuplicateTransitions_Merged` - Verify graceful handling

---

## Task 11: Settings Panel Integration Tests - Complete Coverage

**Priority:** Medium
**Phase:** 11 (Polish)
**Complexity:** Low

### What's Missing

Existing `SettingsPanelIntegrationTests` cover basic parameter editing but are missing:

1. **Enum property editing** (SavingTarget ComboBox)
2. **Store/Load settings round-trip** through the UI
3. **Method settings with server-provided methods**
4. **Result processing with simplify methods**

### Implementation Plan

Add integration tests that:
1. Open settings panel, edit all fields, verify ViewModel updates
2. Store settings to file, close app, reopen, load settings, verify values
3. Verify ComboBox for SavingTarget shows MEMORY/FILE options
4. Verify Integration method ComboBox populated from server

### Acceptance Checklist

- [ ] All settings panel fields editable via UI
- [ ] SavingTarget ComboBox shows MEMORY/FILE
- [ ] Integration method ComboBox shows server methods
- [ ] Store/Load round-trip preserves all values
- [ ] All changes propagate to ViewModel immediately

### Tests

- [ ] `SettingsTests_AllFieldsEditable` - Verify all fields
- [ ] `SettingsTests_SavingTargetComboBox` - Verify enum ComboBox
- [ ] `SettingsTests_StoreLoadRoundTrip` - Verify save/load cycle
- [ ] `SettingsTests_MethodsPopulatedFromServer` - Verify server integration

---

## Task 12: Simulation Workflow Integration Tests

**Priority:** High
**Phase:** 10 (Tasks & Results)
**Complexity:** Medium

### What's Missing

The `SimulationWorkflowTests` exist but need more comprehensive coverage:

1. **Full simulation flow** (compile → run → monitor → download → complete)
2. **Simulation cancellation** mid-flight
3. **Error handling** - compile errors prevent simulation
4. **Result display** - completed results appear in Tasks PopOver

### Implementation Plan

Add end-to-end integration tests using the mock server facade:
1. Simulate a successful compilation + run + download flow
2. Simulate compilation errors blocking simulation
3. Simulate mid-flight cancellation
4. Verify completed results appear in Tasks PopOver with correct metadata

### Acceptance Checklist

- [ ] Successful simulation flow completes end-to-end
- [ ] Compilation errors displayed in ErrorList, simulation blocked
- [ ] Running simulation can be cancelled
- [ ] Cancelled simulation removed from InProgress list
- [ ] Completed simulation appears in Completed list
- [ ] Completed result shows correct model name and point count
- [ ] Show button opens axis picker for completed results
- [ ] Export button opens CSV save dialog

### Tests

- [ ] `SimulationWorkflowTests_FullFlow_Success` - End-to-end successful simulation
- [ ] `SimulationWorkflowTests_FullFlow_CompileError` - Error blocks simulation
- [ ] `SimulationWorkflowTests_FullFlow_Cancellation` - Cancel mid-flight
- [ ] `SimulationWorkflowTests_CompletedResult_AppearInTasks` - Result in tasks list
- [ ] `SimulationWorkflowTests_ShowChart_OpensPicker` - Chart viewer flow
- [ ] `SimulationWorkflowTests_ExportCsv_WritesFile` - CSV export

---

## Task 13: Text Editor Syntax Highlighting Integration

**Priority:** Medium
**Phase:** 8 (Text Editor)
**Complexity:** Medium

### What's Missing

1. **LISMA.xshd syntax file exists** but is not embedded as an Avalonia resource
2. **Server-side highlighting** is implemented but debouncing is missing
3. **No client-side fallback** if server highlighting fails

### Implementation Plan

1. Embed `LISMA.xshd` as `AvaloniaResource` in `ISMA.App.csproj`
2. Load syntax definition from embedded resource
3. Add debouncing (100ms) to syntax highlighting updates in `LismaProjectViewModel`
4. Add fallback to basic highlighting if server highlight fails

### Acceptance Checklist

- [ ] LISMA.xshd embedded as Avalonia resource
- [ ] Syntax highlighting loads from embedded resource
- [ ] Server-side highlighting applied with 100ms debounce
- [ ] Keywords appear orange and bold
- [ ] Comments appear gray and italic
- [ ] Numbers appear blue
- [ ] Fallback highlighting if server unavailable

### Tests

- [ ] `TextEditorTests_SyntaxHighlighting_Loads` - Verify xshd loads
- [ ] `TextEditorTests_HighlightingDebounce` - Verify 100ms debounce
- [ ] `TextEditorTests_ServerHighlighting_Applied` - Verify server-driven highlighting

---

## Task 14: Error Handling & User Feedback

**Priority:** Medium
**Phase:** 11 (Polish)
**Complexity:** Medium

### What's Missing

1. **No error dialogs** for server connection failures
2. **No progress feedback** during long operations (CSV export, file save)
3. **No toast/notification** for save success/failure
4. **No loading indicators** during simulation

### Implementation Plan

1. Add `MessageBox` for server connection errors in `SimulationService`
2. Add progress indicator during CSV export (status bar or toast)
3. Add success toast for file save operations
4. Add loading indicator during simulation compilation/run

### Acceptance Checklist

- [ ] Server connection error shows user-friendly dialog
- [ ] gRPC errors displayed in ErrorList
- [ ] File I/O errors show error dialog
- [ ] CSV export shows progress feedback
- [ ] Simulation compilation shows loading state
- [ ] Save operations show success notification

### Tests

- [ ] `ErrorHandlingTests_ServerError_ShowsDialog` - Verify error dialog
- [ ] `ErrorHandlingTests_FileError_ShowsDialog` - Verify file error dialog
- [ ] `ErrorHandlingTests_ExportProgress_Shown` - Verify progress feedback

---

## Task 15: GlobalStyles.axaml - Pseudo-class Styles

**Priority:** Low
**Phase:** 11 (Polish)
**Complexity:** Low

### What's Missing

Global styles exist but are missing pseudo-class styles for:

1. **Button hover/pressed states** - `:pointerover`, `:pressed`
2. **MenuItem hover state** - `:pointerover`
3. **TabItem selected state** - custom styling
4. **DataGrid row hover/selected** - custom styling

### Implementation Plan

Add pseudo-class styles to `GlobalStyles.axaml`:
```axaml
<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="#E0E0E0"/>
</Style>
<Style Selector="Button:pressed">
    <Setter Property="Background" Value="#C0C0C0"/>
</Style>
<Style Selector="MenuItem:pointerover">
    <Setter Property="Background" Value="#E8E8E8"/>
</Style>
```

### Acceptance Checklist

- [ ] Buttons show hover effect (lighter background)
- [ ] Buttons show pressed effect (darker background)
- [ ] Menu items show hover highlight
- [ ] Tab items have distinct selected state
- [ ] DataGrid rows show hover and selected states
- [ ] All pseudo-classes work across all platforms

### Tests

- [ ] `StyleTests_ButtonHover_VisualFeedback` - Verify hover style
- [ ] `StyleTests_MenuItemHover_VisualFeedback` - Verify menu hover

---

## Implementation Priority Order

| # | Task | Priority | Estimated Effort |
|---|------|----------|-----------------|
| 1 | Blueprint Canvas Rendering | Critical | 3-5 days |
| 8 | SimulationParametersService Fix | High | 0.5-1 day |
| 4 | Window State Persistence | High | 0.5 day |
| 5 | Last Opened Files | High | 0.5 day |
| 12 | Simulation Workflow Tests | High | 1-2 days |
| 2 | PropertiesGrid Enum Support | Medium | 0.5 day |
| 3 | MenuBar SaveAs + Shortcut Fix | Medium | 0.25 day |
| 6 | EditorPlatformService Attach | Medium | 0.5 day |
| 10 | Duplication Prevention | Medium | 0.5-1 day |
| 11 | Settings Panel Test Coverage | Medium | 0.5-1 day |
| 13 | Text Editor Syntax Highlighting | Medium | 0.5-1 day |
| 14 | Error Handling & Feedback | Medium | 1 day |
| 7 | NumericCell Control | Low | 0.25 day |
| 9 | ViewLocator Pattern | Low | 0.25 day |
| 15 | Pseudo-class Styles | Low | 0.5 day |

---

## Testing Strategy

All new features should be covered with:

1. **Unit tests** (ISMA.Tests) - ViewModel logic, domain logic, service logic
2. **Integration tests** (ISMA.Tests.Integration) - End-to-end business flows with real UI components
3. **Headless Avalonia tests** - Using `Avalonia.Headless.XUnit` for UI interaction testing

Integration test patterns to follow:
- Use `IntegrationTestBase` for headless Avalonia setup with mocked server
- Use `UiHelpers` for UI interactions (click buttons, set text, find controls)
- Test through the actual UI control tree, not just ViewModels
- Use `window.Flush()` to force layout updates in headless mode
- Use `AutomationId` properties for reliable control identification
