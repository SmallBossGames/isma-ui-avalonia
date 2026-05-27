# ISMA UI Avalonia — Gap Analysis & Implementation Plan

This document identifies what's missing from the original ISMA JavaFX application that hasn't been fully implemented in the Avalonia migration, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

| Status | Count | Details |
|--------|-------|---------|
| **Fully Implemented** | 22 | Multi-project tabs, blueprint canvas (states/arrows/loops), drag-and-drop, inline name editing, edit popover, blueprint-to-LISMA conversion, compile/validate/run, progress monitoring, result download, error list, settings panel, store/load settings, chart viewer, variable selection, window persistence, menu/toolbar/shortcuts, clipboard propagation |
| **Partially Implemented** | 9 | Syntax highlighting (tokens fetched but not rendered), simulation abort (stub), show/export/remove results (stubs), state content editing (no tab creation), CSV export (not wired), variable dialog OK (stub), parallel settings (not sent to server), result simplification (never applied) |
| **Not Implemented** | 0 | All 31 features from the original have at least skeleton code |

---

## Feature-by-Feature Status (Original 31 Features)

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 1 | Multi-project editing (tabs) | **FULLY IMPLEMENTED** | Full CRUD lifecycle, dirty tracking, last-opened-file restoration |
| 2 | LISMA text editing with syntax highlighting | **PARTIAL** | AvaloniaEdit with line numbers works. Server tokens fetched but **never rendered** as colored spans |
| 3 | Remote syntax highlighting (server-driven) | **PARTIAL** | gRPC call returns tokens, but no bridge to AvaloniaEdit `TextEditor` |
| 4 | Visual statechart (blueprint) editing | **FULLY IMPLEMENTED** | Canvas with states, transitions, loops, all interaction modes |
| 5 | State creation, drag, rename | **FULLY IMPLEMENTED** | Drag-and-drop, 200ms inline name editing, position clamping |
| 6 | Transition arrow creation and management | **FULLY IMPLEMENTED** | Two-click mode, duplicate prevention, removal |
| 7 | Loop transition arrows | **FULLY IMPLEMENTED** | Circle (r=40) with arrowhead, duplicate prevention |
| 8 | Edit arrow PopOver (alias/predicate) | **FULLY IMPLEMENTED** | Bidirectional binding, auto-dismiss on mouse exit |
| 9 | Inline state name editing | **FULLY IMPLEMENTED** | 200ms timer, drag disambiguation, uniqueness validation |
| 10 | Blueprint-to-LISMA conversion | **FULLY IMPLEMENTED** | Full converter with pseudo-state pattern for loops |
| 11 | Model compilation (via gRPC) | **FULLY IMPLEMENTED** | Full compile pipeline with error reporting |
| 12 | Model validation (Verify) | **FULLY IMPLEMENTED** | Full verify flow with error list display |
| 13 | Simulation execution (via gRPC) | **FULLY IMPLEMENTED** | Full pipeline: compile → run → monitor → download → commit |
| 14 | Real-time progress monitoring | **FULLY IMPLEMENTED** | gRPC streaming progress updates to Tasks PopOver |
| 15 | Simulation cancellation | **PARTIAL** | Server-side cancel exists (`CancelSimulationAsync`), but `InProgressSimulationViewModel.AbortCommand` is **empty stub** |
| 16 | Result download and caching | **FULLY IMPLEMENTED** | Binary download to temp cache, ISMR format reader |
| 17 | Error list display | **FULLY IMPLEMENTED** | DataGrid with Row/Position/Fragment/Message columns |
| 18 | Simulation parameters configuration | **FULLY IMPLEMENTED** | All 5 sections auto-generated via PropertiesGrid |
| 19 | Parameter presets (store/load JSON) | **FULLY IMPLEMENTED** | Full store/load with JSON persistence |
| 20 | Chart visualization (Grin process) | **FULLY IMPLEMENTED** | Grin launcher with axis selection |
| 21 | Variable axis selection dialog | **PARTIAL** | UI complete, but `OkCommand`/`CloseCommand` bodies are **empty stubs** |
| 22 | CSV export of results | **PARTIAL** | `ExportToFile()` exists but `CompletedSimulationViewModel.ExportCommand` is **empty stub** |
| 23 | Window state persistence | **FULLY IMPLEMENTED** | Geometry saved/restored via PreferencesProvider |
| 24 | Menu bar and toolbar commands | **FULLY IMPLEMENTED** | All 15 commands wired |
| 25 | Keyboard shortcuts | **FULLY IMPLEMENTED** | All shortcuts defined in MenuBar |
| 26 | Clipboard propagation (cut/copy/paste) | **FULLY IMPLEMENTED** | Window-level Ctrl+X/C/V handling |
| 27 | Tasks PopOver (in-progress + completed) | **PARTIAL** | UI exists, but Abort/Show/Export/Remove commands are **stubs**; no integration with SimulationService |
| 28 | State content editing (double-click → text tab) | **PARTIAL** | `OpenStateTextEditorTab()` doesn't create new tabs |
| 29 | Name uniqueness enforcement | **FULLY IMPLEMENTED** | NameChangingMonitor with auto-increment |
| 30 | Parallel execution settings | **PARTIAL** | UI complete, but Server/Port not sent in gRPC `RunSimulationParams` |
| 31 | Result simplification settings | **PARTIAL** | UI complete, but simplification algorithm never applied |

---

## Implementation Plan — Priority Order

### Task 1: LISMA Text Editor Syntax Highlighting Integration

**Priority:** High
**Phase:** Text Editor
**Complexity:** Medium
**Original Features:** #2, #3

#### What's Missing

1. **Server-highlighting tokens never rendered** — `SyntaxHighlighterService.HighlightSource()` returns `SyntaxTokenDto[]`, `LismaProjectViewModel.UpdateSyntaxHighlighting()` receives them, but `TextEditorFactory.SetSyntaxHighlighting()` is a stub that only sets `HighlightCurrentLine = true`
2. **No AvaloniaEdit tokenizer** — No implementation that converts `SyntaxTokenDto[]` to AvaloniaEdit `ISyntaxHighlighting`
3. **No debouncing** — Text changes trigger highlighting immediately without delay
4. **No client-side fallback** — If server highlighting fails, no fallback syntax definition

#### Implementation Plan

**Step 1: Create LISMA syntax highlighting tokenizer for AvaloniaEdit**

Create `LismaSyntaxHighlighting.cs` implementing `ISyntaxHighlighting`:
```csharp
public class LismaSyntaxHighlighting : ISyntaxHighlighting
{
    private readonly SyntaxTokenDto[] _tokens;
    
    public TextSpans GetSpans(int documentOffset, int documentLength)
    {
        // Map SyntaxTokenDto[] to TextSpans for AvaloniaEdit
        // KEYWORD → orange/bold, COMMENT → gray/italic, NUMBER → blue
    }
    
    public bool HasChangedSince(ITokenSequence sequence) => true;
    public ITokenSequence Sequence { get; } = new TokenSequence();
}
```

**Step 2: Fix `TextEditorFactory.SetSyntaxHighlighting()`**

Replace stub with actual token-to-UI bridge:
```csharp
public void SetSyntaxHighlighting(TextEditor editor, SyntaxTokenDto[] tokens)
{
    var highlighting = new LismaSyntaxHighlighting(tokens);
    editor.SyntaxHighlighting = highlighting;
}
```

**Step 3: Add debouncing in `LismaProjectViewModel.UpdateSyntaxHighlighting()`**

Add 100ms `DispatcherTimer` to debounce highlighting updates:
```csharp
private DispatcherTimer _highlightingTimer;

public void UpdateSyntaxHighlighting(SyntaxTokenDto[] tokens)
{
    _highlightingTimer?.Stop();
    _highlightingTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, (s, e) =>
    {
        _editorFactory.SetSyntaxHighlighting(_editorInstance, tokens);
    }, Application.Current.Dispatcher);
    _highlightingTimer.Start();
}
```

**Step 4: Add client-side fallback**

Create `LISMA.xshd` embedded resource as fallback:
```xml
<!-- LISMA.xshd embedded as AvaloniaResource -->
<SyntaxDefinition name="LISMA" extensions=".iscm2;.im">
    <RuleSet>
        <Keywords color="Orange" weight="bold">state from if else for while return import</Keywords>
        <Rule color="Gray" italic="true">/\*.*?\*/|--.*?$</Rule>
        <Rule color="Blue">\b\d+(\.\d+)?([eE][+-]?\d+)?\b</Rule>
    </RuleSet>
</SyntaxDefinition>
```

#### Acceptance Checklist

- [ ] `LismaSyntaxHighlighting` implements `ISyntaxHighlighting` correctly
- [ ] Server tokens are applied to AvaloniaEdit `TextEditor` via `SetSyntaxHighlighting()`
- [ ] Keywords appear orange and bold
- [ ] Comments appear gray and italic
- [ ] Numbers appear blue
- [ ] Highlighting updates with 100ms debounce (no jank during typing)
- [ ] Client-side fallback loads from embedded `LISMA.xshd` if server unavailable
- [ ] `ISMA.App.csproj` includes `LISMA.xshd` as `AvaloniaResource`

#### Tests

- [ ] `TextEditorTests_SyntaxHighlighting_TokensApplied` — Verify tokens render correctly
- [ ] `TextEditorTests_HighlightingDebounce` — Verify 100ms debounce
- [ ] `TextEditorTests_FallbackSyntax_Loads` — Verify fallback xshd loads
- [ ] `TextEditorTests_ServerHighlighting_Applied` — Verify server-driven highlighting

---

### Task 2: Tasks PopOver — Wire Abort/Show/Export/Remove Commands

**Priority:** High
**Phase:** Tasks & Results
**Complexity:** Medium
**Original Features:** #15, #20, #22, #27

#### What's Missing

1. **`InProgressSimulationViewModel.AbortCommand`** is empty (line 25-27) — clicking Abort does nothing
2. **`CompletedSimulationViewModel.ShowCommand`** is empty — clicking "Show" does nothing
3. **`CompletedSimulationViewModel.ExportCommand`** is empty — clicking "Export" does nothing
4. **`CompletedSimulationViewModel.RemoveCommand`** is empty — clicking "Remove" does nothing
5. **No integration** between `SimulationServiceViewModel` and `TasksPopOverViewModel` — simulation service manages its own `TrackingTasks` but never calls `TasksPopOverViewModel.AddInProgress()` or `AddCompleted()`
6. **`SelectVariablesDialogViewModel.OkCommand`** is empty — OK button doesn't close the dialog with a result

#### Implementation Plan

**Step 1: Wire `InProgressSimulationViewModel.AbortCommand`**

```csharp
[RelayCommand]
private async Task Abort()
{
    if (CanAbort && _simulationService != null)
    {
        await _simulationService.StopSimulationAsync(Id);
    }
}
```

Inject `ISimulationServerFacade` into `InProgressSimulationViewModel` (or use event-based approach via `SimulationServiceViewModel`).

**Step 2: Wire `CompletedSimulationViewModel` commands**

```csharp
[RelayCommand]
private async Task Show()
{
    if (_resultService != null && CachedFile != null)
    {
        await _resultService.ShowChart(CachedFile);
    }
}

[RelayCommand]
private async Task Export()
{
    if (_resultService != null && CachedFile != null)
    {
        await _resultService.ExportToFile(CachedFile);
    }
}

[RelayCommand]
private void Remove()
{
    if (_resultService != null && CachedFile != null)
    {
        _resultService.RemoveResult(CachedFile);
    }
}
```

**Step 3: Integrate `SimulationServiceViewModel` with `TasksPopOverViewModel`**

In `SimulationServiceViewModel.SimulateAsync()`, after creating `InProgressSimulationViewModel`:
```csharp
var inProgress = new InProgressSimulationViewModel(id, modelName, parameters);
_trackingTasks.Add(inProgress);
_tasksPopOver.AddInProgress(inProgress); // NEW: sync with TasksPopOver
```

After download completes:
```csharp
var completed = new CompletedSimulationViewModel(result);
_completedResults.Add(completed);
_tasksPopOver.AddCompleted(completed); // NEW: sync with TasksPopOver
_tasksPopOver.RemoveInProgress(id); // Remove from in-progress
```

**Step 4: Wire `SelectVariablesDialogViewModel.OkCommand`**

```csharp
[RelayCommand]
private async Task Ok()
{
    if (Window.GetWindow(this) is Window window)
    {
        window.DialogResult = true;
    }
}

[RelayCommand]
private async Task Close()
{
    if (Window.GetWindow(this) is Window window)
    {
        window.DialogResult = false;
    }
}
```

#### Acceptance Checklist

- [ ] Abort button stops running simulation and removes from InProgress list
- [ ] Show button opens variable selection dialog, then launches Grin
- [ ] Export button opens CSV save dialog, writes CSV file
- [ ] Remove button removes completed simulation from list
- [ ] Tasks PopOver InProgress list updates during simulation
- [ ] Tasks PopOver Completed list updates after download
- [ ] Select Variables dialog OK/Close buttons close the window with result
- [ ] All commands have proper error handling

#### Tests

- [ ] `SimulationWorkflowTests_Abort_StopsSimulation` — Verify abort flow
- [ ] `SimulationWorkflowTests_ShowChart_OpensPicker` — Verify chart flow
- [ ] `SimulationWorkflowTests_ExportCsv_WritesFile` — Verify CSV export
- [ ] `SimulationWorkflowTests_CompletedResult_InTasksPopOver` — Verify result appears in Tasks
- [ ] `SelectVariablesDialogTests_Ok_ClosesWithResult` — Verify dialog closure

---

### Task 3: State Content Editing — Create New Tabs on Double-Click

**Priority:** Medium
**Phase:** Blueprint Editor
**Complexity:** Medium
**Original Feature:** #28

#### What's Missing

1. **`OpenStateTextEditorTab()`** only sets content on an existing `LismaProjectViewModel` if the current project happens to be one — it does NOT create a new tab
2. **Loop content editing** — Double-click on loop arrowhead only opens the PopOver, not a text editor tab
3. **No tab naming** — Original creates tabs named after state name or `"{stateName} (loop)"`

#### Implementation Plan

**Step 1: Add tab creation to `BlueprintEditorView.OpenStateTextEditorTab()`**

```csharp
private void OpenStateTextEditorTab(BlueprintStateViewModel state)
{
    // Create a new LISMA project tab for this state's content
    var newProject = _projectService.CreateNewAsync(state.Name).Result;
    newProject.SetContent(state.Text);
    
    // Set as active project so user can edit
    _mainWindowViewModel.ActiveProject = newProject;
}
```

**Step 2: Add loop content editing**

In `BlueprintEditorView.OnLoopArrowHeadClicked()`, add double-click handling:
```csharp
private void OnLoopArrowHeadClicked(BlueprintLoopTransactionViewModel loop, PointerPressedEventArgs e)
{
    if (e.ClickCount > 1)
    {
        // Double-click: open text editor tab for loop content
        var newProject = _projectService.CreateNewAsync($"{loop.State.Name} (loop)").Result;
        newProject.SetContent(loop.Text);
        _mainWindowViewModel.ActiveProject = newProject;
    }
    else
    {
        // Single-click: open edit popover (existing behavior)
        OpenEditPopover(loop);
    }
}
```

#### Acceptance Checklist

- [ ] Double-click on state opens a new tab named after the state
- [ ] Tab content is the state's text
- [ ] Tab close disposes the project
- [ ] Double-click on loop arrowhead opens tab named `"{stateName} (loop)"`
- [ ] Loop tab content is the loop's text
- [ ] Editing loop tab content updates the loop model

#### Tests

- [ ] `BlueprintEditorTests_DoubleClickState_CreatesTab` — Verify tab creation
- [ ] `BlueprintEditorTests_DoubleClickLoop_CreatesTab` — Verify loop tab creation
- [ ] `BlueprintEditorTests_TabClose_DisposesProject` — Verify disposal

---

### Task 4: Simulation Parameters — Send Parallel Settings to Server

**Priority:** Medium
**Phase:** Infrastructure
**Complexity:** Low
**Original Feature:** #30

#### What's Missing

1. **`RunSimulationParams` DTO** has no `Server` or `Port` fields — parallel settings are captured in snapshot but never sent to server
2. **`GrpcSimulationClient.RunSimulationAsync()`** doesn't include parallel config in the gRPC request
3. **`RunSimulationRequest` protobuf** needs `server` and `port` fields

#### Implementation Plan

**Step 1: Update `RunSimulationParams` DTO**

```csharp
public record RunSimulationParams(
    // ... existing fields ...
    bool IsParallelInUse,
    string Server,
    int Port
);
```

**Step 2: Update `GrpcSimulationClient.RunSimulationAsync()`**

```csharp
public async Task<long> RunSimulationAsync(RunSimulationParams parameters)
{
    var request = new RunSimulationRequest
    {
        // ... existing fields ...
        IsParallelInUse = parameters.IsParallelInUse,
        Server = parameters.IsParallelInUse ? parameters.Server : "",
        Port = parameters.IsParallelInUse ? parameters.Port : 0
    };
    // ...
}
```

**Step 3: Update protobuf definition** (if protobuf files are available)

Add `server` and `port` fields to `RunSimulationRequest` message in the proto file.

#### Acceptance Checklist

- [ ] `RunSimulationParams` includes `IsParallelInUse`, `Server`, `Port`
- [ ] gRPC request includes parallel config when `IsParallelInUse` is true
- [ ] Server receives parallel settings and uses them for remote execution
- [ ] When `IsParallelInUse` is false, server/Port are not sent (or sent as empty/0)

#### Tests

- [ ] `SimulationServiceTests_ParallelSettings_IncludedInRequest` — Verify parallel config sent

---

### Task 5: Result Simplification — Implement Line Simplification Algorithm

**Priority:** Low
**Phase:** Results Processing
**Complexity:** Medium
**Original Feature:** #31

#### What's Missing

1. **No simplification algorithm** — Settings UI exists but `Radial-Distance` and `Douglas-Peucker` are never applied to simulation data
2. **No chart rendering integration** — Even if implemented, simplification would need to be applied before chart display

#### Implementation Plan

**Step 1: Create `ResultSimplifier` domain class**

```csharp
public static class ResultSimplifier
{
    public static IEnumerable<SimulationPoint> RadialDistance(
        IEnumerable<SimulationPoint> points, double tolerance)
    {
        // Radial-distance algorithm: remove points within tolerance of line segment
    }
    
    public static IEnumerable<SimulationPoint> DouglasPeucker(
        IEnumerable<SimulationPoint> points, double tolerance)
    {
        // Douglas-Peucker algorithm: recursive line simplification
    }
}
```

**Step 2: Apply simplification in `SimulationResultService.ShowChart()`**

Before launching Grin, apply simplification if enabled:
```csharp
public async Task ShowChart(CompletedSimulationModel result)
{
    var points = BinaryFilePointProvider.Read(result.CachedFile);
    
    if (result.Parameters.ResultProcessing.IsSimplifyInUse)
    {
        points = result.Parameters.ResultProcessing.SelectedSimplifyMethod switch
        {
            "Radial-Distance" => ResultSimplifier.RadialDistance(points, result.Parameters.ResultProcessing.Tolerance),
            "Douglas-Peucker" => ResultSimplifier.DouglasPeucker(points, result.Parameters.ResultProcessing.Tolerance),
            _ => points
        };
    }
    
    // Write simplified points to temp file for Grin
    // Launch Grin with simplified data
}
```

#### Acceptance Checklist

- [ ] `ResultSimplifier.RadialDistance()` correctly removes points within tolerance
- [ ] `ResultSimplifier.DouglasPeucker()` correctly simplifies with tolerance
- [ ] Simplification is applied before chart display when enabled
- [ ] Simplification is NOT applied when disabled
- [ ] Tolerance value is respected (lower = more points, higher = fewer points)

#### Tests

- [ ] `ResultSimplifierTests_RadialDistance_RemovesPoints` — Verify algorithm
- [ ] `ResultSimplifierTests_DouglasPeucker_RemovesPoints` — Verify algorithm
- [ ] `ResultSimplifierTests_Tolerance_AffectsOutput` — Verify tolerance impact

---

### Task 6: Settings Panel Integration Tests — Complete Coverage

**Priority:** Medium
**Phase:** Polish
**Complexity:** Low

#### What's Missing

Existing `SettingsPanelIntegrationTests` cover basic parameter editing but are missing:

1. **Enum property editing** (SavingTarget ComboBox) — No test verifies ComboBox interaction
2. **Store/Load settings round-trip** through the UI — No end-to-end test
3. **Method settings with server-provided methods** — ComboBox not tested
4. **Result processing with simplify methods** — Not tested

#### Implementation Plan

Add integration tests using headless Avalonia + `UiHelpers`:

```csharp
[Fact]
public async Task SavingTargetComboBox_ShowsMemoryAndFile()
{
    var window = new MainWindow();
    await window.Show();
    
    var comboBox = window.FindControl<ComboBox>("SavingTargetComboBox");
    comboBox.Should().NotBeNull();
    
    var items = comboBox.Items.Cast<string>().ToList();
    items.Should().Contain("MEMORY");
    items.Should().Contain("FILE");
}

[Fact]
public async Task StoreLoadSettings_RoundTrip_PreservesValues()
{
    var window = new MainWindow();
    await window.Show();
    
    // Edit all settings
    var startTimeBox = window.FindControl<TextBox>("StartTimeTextBox");
    await TypeText(startTimeBox, "5.0");
    
    // Store settings
    var storeMenu = window.FindControl<MenuItem>("MenuStoreSettings");
    await ClickMenuItem(storeMenu);
    
    // Simulate file save (mocked)
    
    // Load settings
    var loadMenu = window.FindControl<MenuItem>("MenuLoadSettings");
    await ClickMenuItem(loadMenu);
    
    // Verify values restored
    startTimeBox.Text.Should().Be("5.0");
}
```

#### Acceptance Checklist

- [ ] SavingTarget ComboBox shows MEMORY/FILE options
- [ ] ComboBox selection updates ViewModel via TwoWay binding
- [ ] Store/Load round-trip preserves all values
- [ ] All settings panel fields editable via UI
- [ ] Changes propagate to ViewModel immediately

#### Tests

- [ ] `SettingsTests_SavingTargetComboBox` — Verify enum ComboBox
- [ ] `SettingsTests_StoreLoadRoundTrip` — Verify save/load cycle
- [ ] `SettingsTests_AllFieldsEditable` — Verify all fields
- [ ] `SettingsTests_MethodsPopulatedFromServer` — Verify server integration

---

### Task 7: Simulation Workflow Integration Tests — Full Coverage

**Priority:** High
**Phase:** Tasks & Results
**Complexity:** Medium

#### What's Missing

The `SimulationWorkflowTests` exist but need more comprehensive coverage:

1. **Full simulation flow** (compile → run → monitor → download → complete)
2. **Simulation cancellation** mid-flight
3. **Error handling** — compile errors prevent simulation
4. **Result display** — completed results appear in Tasks PopOver

#### Implementation Plan

Add end-to-end integration tests using mock server facade:

```csharp
[Fact]
public async Task FullFlow_Success_CompletesEndToEnd()
{
    // Mock server returns compile success → run → progress → download
    var mockFacade = new Mock<ISimulationServerFacade>();
    mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
        .ReturnsAsync(new CompileResult("model-1", new List<CompilationError>(), new List<string>()));
    mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
        .ReturnsAsync(1L);
    mockFacade.Setup(f => f.MonitorSimulation(1L))
        .ReturnsAsyncProgress(); // Mock streaming progress
    mockFacade.Setup(f => f.DownloadResult(1L))
        .ReturnsAsync(new CachedSimulationResult("cache.bin", new List<string>()));
    
    var service = new SimulationServiceViewModel(mockFacade.Object, ...);
    await service.SimulateAsync();
    
    // Verify completed result appears in tracking
    service.TrackingTasks.Should().HaveCount(0); // All removed
    // Verify completed results list updated
}

[Fact]
public async Task FullFlow_CompileError_BlocksSimulation()
{
    var mockFacade = new Mock<ISimulationServerFacade>();
    mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
        .ReturnsAsync(new CompileResult("", new List<CompilationError> { ... }, new List<string>()));
    
    var service = new SimulationServiceViewModel(mockFacade.Object, ...);
    await service.SimulateAsync();
    
    // Verify error list populated
    errorList.Errors.Should().NotBeEmpty();
    // Verify no simulation run attempted
    mockFacade.Verify(f => f.RunSimulation(It.IsAny<RunSimulationParams>()), Times.Never);
}
```

#### Acceptance Checklist

- [ ] Successful simulation flow completes end-to-end
- [ ] Compilation errors displayed in ErrorList, simulation blocked
- [ ] Running simulation can be cancelled
- [ ] Cancelled simulation removed from InProgress list
- [ ] Completed simulation appears in Completed list
- [ ] Completed result shows correct model name and point count
- [ ] Show button opens axis picker for completed results
- [ ] Export button opens CSV save dialog

#### Tests

- [ ] `SimulationWorkflowTests_FullFlow_Success` — End-to-end successful simulation
- [ ] `SimulationWorkflowTests_FullFlow_CompileError` — Error blocks simulation
- [ ] `SimulationWorkflowTests_FullFlow_Cancellation` — Cancel mid-flight
- [ ] `SimulationWorkflowTests_CompletedResult_AppearInTasks` — Result in tasks list
- [ ] `SimulationWorkflowTests_ShowChart_OpensPicker` — Chart viewer flow
- [ ] `SimulationWorkflowTests_ExportCsv_WritesFile` — CSV export

---

### Task 8: Error Handling & User Feedback

**Priority:** Medium
**Phase:** Polish
**Complexity:** Medium

#### What's Missing

1. **No error dialogs** for server connection failures
2. **No progress feedback** during long operations (CSV export, file save)
3. **No toast/notification** for save success/failure
4. **No loading indicators** during simulation

#### Implementation Plan

**Step 1: Add MessageBox for server connection errors**

```csharp
public async Task<bool> ConnectAsync()
{
    try
    {
        await _serverManager.StartAsync();
        return true;
    }
    catch (Exception ex)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show($"Failed to connect to ISMA server:\n{ex.Message}",
                "Server Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        });
        return false;
    }
}
```

**Step 2: Add progress indicator during CSV export**

```csharp
public async Task ExportToFile(CompletedSimulationModel result)
{
    var dialog = new SaveFileDialog();
    dialog.Filters.Add(new FileFilter("CSV Files", "*.csv"));
    
    if (await dialog.ShowAsync() != null)
    {
        // Show progress in status bar or toast
        _statusService.ShowProgress("Exporting results...");
        
        await Task.Run(() =>
        {
            // Export logic
        });
        
        _statusService.HideProgress();
        _statusService.ShowToast("Export completed successfully");
    }
}
```

**Step 3: Add loading indicator during simulation**

In `MainWindowViewModel.Run()`:
```csharp
[RelayCommand]
private async Task Run()
{
    if (ActiveProject == null) return;
    
    _statusService.ShowLoading("Compiling and running simulation...");
    try
    {
        await _simulationService.SimulateAsync();
    }
    finally
    {
        _statusService.HideLoading();
    }
}
```

#### Acceptance Checklist

- [ ] Server connection error shows user-friendly dialog
- [ ] gRPC errors displayed in ErrorList
- [ ] File I/O errors show error dialog
- [ ] CSV export shows progress feedback
- [ ] Simulation compilation shows loading state
- [ ] Save operations show success notification

#### Tests

- [ ] `ErrorHandlingTests_ServerError_ShowsDialog` — Verify error dialog
- [ ] `ErrorHandlingTests_FileError_ShowsDialog` — Verify file error dialog
- [ ] `ErrorHandlingTests_ExportProgress_Shown` — Verify progress feedback

---

### Task 9: Select All Command

**Priority:** Low
**Phase:** Menu Bar
**Complexity:** Low

#### What's Missing

1. **Menu item exists** in `IsmaTextEditorView.axaml` context menu but `SelectAllCommand` not wired in `MainWindowViewModel`

#### Implementation Plan

Add `SelectAllCommand` to `MainWindowViewModel`:
```csharp
[RelayCommand]
private async Task SelectAll()
{
    if (ActiveProject is LismaProjectViewModel lisma)
    {
        lisma.TriggerSelectAll();
    }
}
```

Add `TriggerSelectAll()` to `IProjectViewModel` and `LismaProjectViewModel`:
```csharp
void TriggerSelectAll();
```

#### Acceptance Checklist

- [ ] Select All command exists in MainWindowViewModel
- [ ] Select All selects all text in focused editor
- [ ] Works via context menu and keyboard shortcut (Ctrl+A)

#### Tests

- [ ] `MenuBarTests_SelectAllCommand_Exists` — Verify command exists
- [ ] `TextEditorTests_SelectAll_SelectsAllText` — Verify selection

---

### Task 10: Pseudo-class Styles

**Priority:** Low
**Phase:** Polish
**Complexity:** Low

#### What's Missing

Global styles exist in `GlobalStyles.axaml` but are missing pseudo-class styles for:

1. **Button hover/pressed states** — `:pointerover`, `:pressed`
2. **MenuItem hover state** — `:pointerover`
3. **TabItem selected state** — custom styling
4. **DataGrid row hover/selected** — custom styling

#### Implementation Plan

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
<Style Selector="TabControl TabItem:selected">
    <Setter Property="Background" Value="#F0F0F0"/>
</Style>
<Style Selector="DataGridRow:pointerover">
    <Setter Property="Background" Value="#F5F5F5"/>
</Style>
<Style Selector="DataGridRow:selected">
    <Setter Property="Background" Value="#E0E0FF"/>
</Style>
```

#### Acceptance Checklist

- [ ] Buttons show hover effect (lighter background)
- [ ] Buttons show pressed effect (darker background)
- [ ] Menu items show hover highlight
- [ ] Tab items have distinct selected state
- [ ] DataGrid rows show hover and selected states
- [ ] All pseudo-classes work across all platforms

#### Tests

- [ ] `StyleTests_ButtonHover_VisualFeedback` — Verify hover style
- [ ] `StyleTests_MenuItemHover_VisualFeedback` — Verify menu hover

---

## Implementation Priority Order

| # | Task | Priority | Estimated Effort | Original Features |
|---|------|----------|-----------------|-------------------|
| 1 | LISMA Syntax Highlighting | High | 1-2 days | #2, #3 |
| 2 | Tasks PopOver Commands | High | 1-2 days | #15, #20, #22, #27 |
| 3 | Simulation Workflow Tests | High | 1-2 days | #13, #14, #15, #16 |
| 4 | State Content Editing | Medium | 0.5-1 day | #28 |
| 5 | Parallel Settings to Server | Medium | 0.5 day | #30 |
| 6 | Settings Panel Test Coverage | Medium | 0.5-1 day | All settings |
| 7 | Error Handling & Feedback | Medium | 1 day | All features |
| 8 | Result Simplification | Low | 1-2 days | #31 |
| 9 | Select All Command | Low | 0.25 day | #26 |
| 10 | Pseudo-class Styles | Low | 0.5 day | All UI |

---

## Testing Strategy

All new features should be covered with:

1. **Unit tests** (ISMA.Tests) — ViewModel logic, domain logic, service logic
2. **Integration tests** (ISMA.Tests.Integration) — End-to-end business flows with real UI components
3. **Headless Avalonia tests** — Using `Avalonia.Headless.XUnit` for UI interaction testing

Integration test patterns to follow:
- Use `IntegrationTestBase` for headless Avalonia setup with mocked server
- Use `UiHelpers` for UI interactions (click buttons, set text, find controls)
- Test through the actual UI control tree, not just ViewModels
- Use `window.Flush()` to force layout updates in headless mode
- Use `AutomationId` properties for reliable control identification

---

## Files to Create/Modify

### New Files
- `LismaSyntaxHighlighting.cs` — AvaloniaEdit tokenizer
- `LISMA.xshd` — Embedded syntax definition (fallback)
- `ResultSimplifier.cs` — Line simplification algorithms
- `StatusService.cs` — Progress/toast/status bar service
- `Tests/TextEditorTests_SyntaxHighlighting.cs`
- `Tests/SimulationWorkflowTests_FullCoverage.cs`
- `Tests/SettingsTests_CompleteCoverage.cs`
- `Tests/ErrorHandlingTests.cs`

### Modified Files
- `TextEditorFactory.cs` — Wire syntax highlighting
- `LismaProjectViewModel.cs` — Add debouncing
- `InProgressSimulationViewModel.cs` — Wire AbortCommand
- `CompletedSimulationViewModel.cs` — Wire Show/Export/Remove commands
- `SimulationServiceViewModel.cs` — Integrate with TasksPopOverViewModel
- `SelectVariablesDialogViewModel.cs` — Wire Ok/Close commands
- `RunSimulationParams.cs` — Add parallel fields
- `GrpcSimulationClient.cs` — Send parallel config
- `GlobalStyles.axaml` — Add pseudo-class styles
- `MainWindowViewModel.cs` — Add SelectAllCommand
