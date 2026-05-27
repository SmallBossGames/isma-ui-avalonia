# ISMA UI Avalonia — Gap Analysis & Implementation Plan

This document identifies what's missing from the original ISMA JavaFX application that hasn't been fully implemented in the Avalonia migration, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

| Status | Count | Details |
|--------|-------|---------|
| **Fully Implemented** | 29 | Multi-project tabs, blueprint canvas (states/arrows/loops), drag-and-drop, inline name editing, edit popover, blueprint-to-LISMA conversion, compile/validate/run, progress monitoring, result download, error list, settings panel, store/load settings, chart viewer UI, variable selection dialog UI, window persistence, menu/toolbar/shortcuts, clipboard propagation, blueprint editor modes, simulation abort, show/export/remove results, state content editing, variable dialog OK/Close, parallel settings to server, result simplification, Tasks PopOver integration, preferences persistence, error handling & feedback, Select All command, result download column names |
| **Partially Implemented** | 1 | Syntax highlighting (tokens fetched but not rendered — AvaloniaEdit API mismatch) |
| **Not Implemented** | 0 | All 31 features from the original have at least skeleton code |

---

## Feature-by-Feature Status (Original 31 Features)

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 1 | Multi-project editing (tabs) | **FULLY IMPLEMENTED** | Full CRUD lifecycle, dirty tracking, last-opened-file restoration |
| 2 | LISMA text editing with syntax highlighting | **PARTIAL** | AvaloniaEdit with line numbers works. Server tokens fetched but **never rendered** as colored spans |
| 3 | Remote syntax highlighting (server-driven) | **PARTIAL** | gRPC call returns tokens, but no bridge to AvaloniaEdit `TextEditor` for rendering |
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
| 15 | Simulation cancellation | **FULLY IMPLEMENTED** | `InProgressSimulationViewModel.AbortCommand` wired to `CancelSimulation` |
| 16 | Result download and caching | **FULLY IMPLEMENTED** | Binary download to temp cache. Column names populated from gRPC response. |
| 17 | Error list display | **FULLY IMPLEMENTED** | DataGrid with Row/Position/Fragment/Message columns |
| 18 | Simulation parameters configuration | **FULLY IMPLEMENTED** | All 5 sections auto-generated via PropertiesGrid |
| 19 | Parameter presets (store/load JSON) | **FULLY IMPLEMENTED** | Full store/load with JSON persistence |
| 20 | Chart visualization (Grin process) | **PARTIAL** | Grin launcher exists, but `CompletedSimulationViewModel.ShowCommand` is **empty stub** — never triggered from UI |
| 21 | Variable axis selection dialog | **PARTIAL** | UI complete, but `OkCommand`/`CloseCommand` bodies are **empty stubs** — dialog never closes with result |
| 22 | CSV export of results | **PARTIAL** | `ExportToFile()` exists but `CompletedSimulationViewModel.ExportCommand` is **empty stub** |
| 23 | Window state persistence | **FULLY IMPLEMENTED** | Geometry saved/restored via PreferencesProvider |
| 24 | Menu bar and toolbar commands | **FULLY IMPLEMENTED** | All 15 commands wired. **BUT**: `OnMenuItemClick` code-behind is empty stub |
| 25 | Keyboard shortcuts | **FULLY IMPLEMENTED** | All shortcuts defined in MenuBar via InputBindings |
| 26 | Clipboard propagation (cut/copy/paste) | **PARTIAL** | Cut/Copy/Paste commands exist. `TriggerCut/Copy/Paste` on BlueprintProjectViewModel are **no-op** (intentional) |
| 27 | Tasks PopOver (in-progress + completed) | **PARTIAL** | UI exists, but Abort/Show/Export/Remove commands are **stubs**; no integration with SimulationService |
| 28 | State content editing (double-click → text tab) | **PARTIAL** | `OpenStateTextEditorTab()` doesn't create new tabs — just sets content on existing project |
| 29 | Name uniqueness enforcement | **FULLY IMPLEMENTED** | NameChangingMonitor with auto-increment |
| 30 | Parallel execution settings | **PARTIAL** | UI complete, but `RunSimulationParams` DTO has no `Server`/`Port` fields — parallel config never sent |
| 31 | Result simplification settings | **PARTIAL** | UI complete, but simplification algorithm never applied to data |

---

## Additional Gaps Identified

Beyond the 31 original features, the following issues were found:

| # | Issue | Severity | Notes |
|---|-------|----------|-------|
| B | SearchPanel commented out in TextEditorFactory | Low | `SearchPanel.Install(te)` commented out |
| C | ArrowLine/LoopArrow pointer handlers compute distances but never act | Low | Dead code after arrowhead distance calculation |
| G | No pseudo-class styles for buttons/menu items | Low | Missing `:pointerover`, `:pressed` styles |

**Fixed gaps:** A (PreferencesProvider), D (Error dialogs), E (Loading indicators — partial via StatusText), F (Select All), H (TasksPopOver sync)

---

## Implementation Plan — Priority Order

### Task 1: LISMA Text Editor Syntax Highlighting Integration

**Priority:** High
**Phase:** Text Editor
**Complexity:** Medium
**Original Features:** #2, #3
**Status:** ⏸ DEFERRED — AvaloniaEdit API mismatch (see notes below)
**Files to Create/Modify:**
- Create: `src/ISMA.App/Services/LismaSyntaxHighlighting.cs`
- Modify: `src/ISMA.App/Services/TextEditorFactory.cs`
- Modify: `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs`

#### What's Missing

1. **Server-highlighting tokens never rendered** — `SyntaxHighlighterService.HighlightSource()` returns `SyntaxTokenDto[]`, `LismaProjectViewModel.UpdateSyntaxHighlighting()` receives them, but `TextEditorFactory.SetSyntaxHighlighting()` is a stub that only sets `HighlightCurrentLine = true`
2. **No AvaloniaEdit tokenizer** — No implementation that converts `SyntaxTokenDto[]` to AvaloniaEdit `ISyntaxHighlighting`
3. **No debouncing** — Text changes trigger highlighting immediately without delay
4. **No client-side fallback** — If server highlighting fails, no fallback syntax definition

#### Deferral Reason

Implementation was deferred due to AvaloniaEdit API mismatches:
- `VisualLineLine`, `TextRunProperties`, `IVisualLineTransformer.Transform`, and `VisualLineElement.Offset/TextLength` don't exist in the AvaloniaEdit version used by this project
- The AvaloniaEdit API surface differs from the WPF AvalonEdit API
- Requires further investigation to find the correct AvaloniaEdit API for syntax highlighting

**Next steps:** Investigate Avalonia 12's text editing capabilities. Consider using `TextEditor` with a custom `ISyntaxHighlightingDefinition` or explore alternative approaches (e.g., styled text boxes, custom rendering).

#### Implementation Plan

**Step 1: Create `LismaSyntaxHighlighting.cs` implementing `ISyntaxHighlighting`**

```csharp
// src/ISMA.App/Services/LismaSyntaxHighlighting.cs
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ISMA.Domain.DtoModels;
using System.Collections.Immutable;

namespace ISMA.App.Services;

public class LismaSyntaxHighlighting : ISyntaxHighlighting
{
    private readonly ImmutableArray<SyntaxTokenDto> _tokens;
    private readonly ImmutableArray<int> _tokenStarts;
    private readonly ImmutableArray<int> _tokenLengths;
    private readonly ImmutableArray<string> _tokenColors;
    private readonly ImmutableArray<bool> _tokenBold;
    private readonly ImmutableArray<bool> _tokenItalic;

    public LismaSyntaxHighlighting(SyntaxTokenDto[] tokens)
    {
        var count = tokens.Length;
        _tokenStarts = ImmutableArray.CreateBuilder<int>(count).ToImmutable();
        _tokenLengths = ImmutableArray.CreateBuilder<int>(count).ToImmutable();
        _tokenColors = ImmutableArray.CreateBuilder<string>(count).ToImmutable();
        _tokenBold = ImmutableArray.CreateBuilder<bool>(count).ToImmutable();
        _tokenItalic = ImmutableArray.CreateBuilder<bool>(count).ToImmutable();

        var starts = new int[count];
        var lengths = new int[count];
        var colors = new string[count];
        var bolds = new bool[count];
        var italics = new bool[count];

        for (int i = 0; i < count; i++)
        {
            starts[i] = tokens[i].Start;
            lengths[i] = tokens[i].Length;
            colors[i] = GetColorForKind(tokens[i].Kind);
            bolds[i] = tokens[i].Kind == SyntaxTokenKind.Keyword;
            italics[i] = tokens[i].Kind == SyntaxTokenKind.Comment;
        }

        _tokenStarts = starts.ToImmutableArray();
        _tokenLengths = lengths.ToImmutableArray();
        _tokenColors = colors.ToImmutableArray();
        _tokenBold = bolds.ToImmutableArray();
        _tokenItalic = italics.ToImmutableArray();
    }

    private static string GetColorForKind(SyntaxTokenKind kind) => kind switch
    {
        SyntaxTokenKind.Keyword => "#CC7A32",    // Orange (JetBrains)
        SyntaxTokenKind.Comment => "#808080",    // Gray
        SyntaxTokenKind.Number => "#3DAEE9",     // Blue
        SyntaxTokenKind.Text => "#6A8759",       // Green
        _ => "#A9B7C6",                           // Default (JetBrains light)
    };

    public TextSpans GetSpans(int documentOffset, int documentLength)
    {
        var spans = new TextSpans();
        for (int i = 0; i < _tokenStarts.Length; i++)
        {
            int tokenStart = _tokenStarts[i];
            int tokenLength = _tokenLengths[i];
            if (tokenStart < documentOffset + documentLength && tokenStart + tokenLength > documentOffset)
            {
                int relativeStart = Math.Max(0, tokenStart - documentOffset);
                int relativeLength = Math.Min(tokenLength, documentOffset + documentLength - (tokenStart - documentOffset));
                spans.Add(relativeStart, relativeLength, new HighlightingColorDefinition(_tokenColors[i], bold: _tokenBold[i], italic: _tokenItalic[i]));
            }
        }
        return spans;
    }

    public bool HasChangedSince(ITokenSequence sequence) => true;
    public ITokenSequence Sequence { get; } = new TokenSequence();

    private class TokenSequence : ITokenSequence
    {
        public int Version => 0;
        public int Count => 0;
        public int GetStart(int index) => 0;
        public int GetLength(int index) => 0;
        public HighlightingDefinition GetDefinition(int index) => null!;
    }
}
```

**Step 2: Fix `TextEditorFactory.SetSyntaxHighlighting()`**

Replace stub with actual token-to-UI bridge:
```csharp
// In TextEditorFactory.cs
public void SetSyntaxHighlighting(TextEditor editor, SyntaxTokenDto[] tokens)
{
    var highlighting = new LismaSyntaxHighlighting(tokens);
    editor.SyntaxHighlighting = highlighting;
}
```

**Step 3: Add debouncing in `LismaProjectViewModel.UpdateSyntaxHighlighting()`**

Add 100ms `DispatcherTimer` to debounce highlighting updates:
```csharp
private DispatcherTimer? _highlightingTimer;

public void UpdateSyntaxHighlighting(SyntaxTokenDto[] tokens)
{
    _highlightingTimer?.Stop();
    _highlightingTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), Application.Current.Dispatcher!)
    {
        IsEnabled = true
    };
    _highlightingTimer.Tick += (s, e) =>
    {
        _highlightingTimer!.IsEnabled = false;
        _editorFactory.SetSyntaxHighlighting(_editorInstance!, tokens);
    };
}
```

**Step 4: Add client-side fallback**

The existing `LISMA.xshd` embedded resource should be loaded as fallback when server highlighting fails.

#### Acceptance Checklist

- [ ] `LismaSyntaxHighlighting` implements `ISyntaxHighlighting` correctly
- [ ] Server tokens are applied to AvaloniaEdit `TextEditor` via `SetSyntaxHighlighting()`
- [ ] Keywords appear orange and bold (`#CC7A32`)
- [ ] Comments appear gray and italic (`#808080`)
- [ ] Numbers appear blue (`#3DAEE9`)
- [ ] Text appears green (`#6A8759`)
- [ ] Highlighting updates with 100ms debounce (no jank during typing)
- [ ] Client-side fallback loads from embedded `LISMA.xshd` if server unavailable
- [ ] `ISMA.App.csproj` includes `LISMA.xshd` as `AvaloniaResource`

#### Tests

- [ ] `TextEditorIntegrationTests_SyntaxHighlighting_TokensApplied` — Verify tokens render correctly
- [ ] `TextEditorIntegrationTests_HighlightingDebounce` — Verify debouncing
- [ ] `TextEditorIntegrationTests_FallbackSyntax_Loads` — Verify fallback xshd loads
- [ ] `ViewModelTests_LismaProjectViewModel_HighlightingApplied` — Verify VM calls factory

---

### ✅ Task 2: Tasks PopOver — Wire Abort/Show/Export/Remove Commands

**Status:** ✅ COMPLETED
**Files Modified:**
- `src/ISMA.ViewModels/ViewModels/InProgressSimulationViewModel.cs` — AbortCommand wired to `CancelSimulation`
- `src/ISMA.ViewModels/ViewModels/CompletedSimulationViewModel.cs` — Show/Export/Remove commands wired
- `src/ISMA.ViewModels/ViewModels/SelectVariablesDialogViewModel.cs` — OkPressed/ClosePressed flags
- `src/ISMA.ViewModels/ViewModels/SimulationServiceViewModel.cs` — TasksPopOver sync via `AddInProgress`/`RemoveInProgress`/`AddCompleted`
- `src/ISMA.App/Views/SelectVariablesDialogWindow.axaml.cs` — Dialog result handling

**Tests:** `TasksPopOverCommandTests` (4 tests) + `SelectAllCommandTests` (3 tests)

---

### ✅ Task 3: State Content Editing — Create New Tabs on Double-Click

**Status:** ✅ COMPLETED
**Files Modified:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — `OpenStateTextEditorTab` creates new tab
- `src/ISMA.ViewModels/Services/ProjectService.cs` — `CreateNewTextProject` method
- `src/ISMA.ViewModels/ViewModels/IProjectViewModel.cs` — `SetContent`/`TriggerSelectAll` interface methods
- `src/ISMA.ViewModels/ViewModels/BlueprintProjectViewModel.cs` — Stub implementations

---

### ✅ Task 4: Simulation Parameters — Send Parallel Settings to Server

**Status:** ✅ COMPLETED
**Files Modified:**
- `src/ISMA.Domain/Dtos/DtoModels.cs` — `RunSimulationParams` includes `IsParallelInUse`, `Server`, `Port`
- `src/ISMA.ViewModels/Services/SimulationService.cs` — Parallel settings passed to gRPC
- `src/ISMA.Infrastructure/run_simulation_request.proto` — Added `is_parallel_in_use`, `server`, `port` fields

---

### ✅ Task 5: Result Simplification — Implement Line Simplification Algorithm

**Status:** ✅ COMPLETED
**Files Created:**
- `src/ISMA.Domain/Conversion/ResultSimplifier.cs` — Douglas-Peucker & Radial-Distance algorithms
**Tests:** `ResultSimplifierTests` (9 tests)

---

### ✅ Task 6: PreferencesProvider — Wire LoadFromPreferences/SaveToPreferences

**Status:** ✅ COMPLETED
**Files Modified:**
- `src/ISMA.Domain/Models/PreferencesModels.cs` — `SavedParameters` field added
- `src/ISMA.ViewModels/Services/SimulationParametersService.cs` — `LoadFromPreferences`/`SaveToPreferences` wired

---

### ✅ Task 7: Error Handling & User Feedback

**Status:** ✅ COMPLETED
**Files Created:**
- `src/ISMA.App/Services/DialogService.cs` — `IDialogService` with `ShowErrorAsync`, `ShowSuccessAsync`, `ShowConfirmationAsync`
**Files Modified:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — Uses `DialogService` for state editing errors
- `src/ISMA.App/Views/SelectVariablesDialogWindow.axaml.cs` — Uses `DialogService` for variable selection errors

---

### ✅ Task 8: Select All Command

**Status:** ✅ COMPLETED
**Files Modified:**
- `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` — `SelectAllCommand` added
- `src/ISMA.ViewModels/ViewModels/IProjectViewModel.cs` — `TriggerSelectAll` method
- `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` — `TriggerSelectAll` implementation
**Tests:** `SelectAllCommandTests` (3 tests)

---

### ✅ Task 10: Result Download — Fix Column Names

**Status:** ✅ COMPLETED
**Files Modified:**
- `src/ISMA.Infrastructure/get_simulation_result_response.proto` — Added `column_names` field
- `src/ISMA.Infrastructure/Server/GrpcSimulationClient.cs` — Column names parsed from response

---

### Task 9: Pseudo-class Styles

#### What's Missing

1. **`InProgressSimulationViewModel.AbortCommand`** is empty (line 25-27) — clicking Abort does nothing
2. **`CompletedSimulationViewModel.ShowCommand`** is empty — clicking "Show" does nothing
3. **`CompletedSimulationViewModel.ExportCommand`** is empty — clicking "Export" does nothing
4. **`CompletedSimulationViewModel.RemoveCommand`** is empty — clicking "Remove" does nothing
5. **No integration** between `SimulationServiceViewModel` and `TasksPopOverViewModel` — simulation service manages its own `TrackingTasks` but never calls `TasksPopOverViewModel.AddInProgress()` or `AddCompleted()`
6. **`SelectVariablesDialogViewModel.OkCommand`** is empty — OK button doesn't close the dialog with a result
7. **`SelectVariablesDialogViewModel.CloseCommand`** is empty — Close button doesn't close the dialog

#### Implementation Plan

**Step 1: Wire `InProgressSimulationViewModel.AbortCommand`**

Inject `ISimulationServerFacade` into `InProgressSimulationViewModel`:
```csharp
public partial class InProgressSimulationViewModel : ObservableObject, IDisposable
{
    private readonly ISimulationServerFacade? _serverFacade;

    public InProgressSimulationViewModel(int id, string modelName, SimulationParameters parameters, ISimulationServerFacade? serverFacade = null)
    {
        Id = id;
        ModelName = modelName;
        Parameters = parameters;
        _serverFacade = serverFacade;
        Progress = 0.0;
        CanAbort = true;
    }

    [RelayCommand]
    private async Task Abort()
    {
        if (CanAbort && _serverFacade != null)
        {
            CanAbort = false;
            try
            {
                await _serverFacade.CancelSimulationAsync(Id);
            }
            catch
            {
                // Silently ignore — server may already be stopped
            }
        }
    }
}
```

**Step 2: Wire `CompletedSimulationViewModel` commands**

```csharp
public partial class CompletedSimulationViewModel : ObservableObject
{
    private readonly ISimulationResultService? _resultService;

    public CompletedSimulationViewModel(CompletedSimulation completed, ISimulationResultService? resultService = null)
    {
        Completed = completed;
        _resultService = resultService;
        ModelName = completed.ModelName;
        CachedFile = completed.CachedFile;
    }

    [RelayCommand]
    private async Task Show()
    {
        if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
        {
            await _resultService.ShowChartAsync(new CompletedSimulation(
                Completed.Id, ModelName, null, default, Completed.Parameters,
                CachedFile, Completed.CachedColumnNames));
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
        {
            await _resultService.ExportToFileAsync(new CompletedSimulation(
                Completed.Id, ModelName, null, default, Completed.Parameters,
                CachedFile, Completed.CachedColumnNames));
        }
    }

    [RelayCommand]
    private void Remove()
    {
        if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
        {
            _resultService.RemoveResult(new CompletedSimulation(
                Completed.Id, ModelName, null, default, Completed.Parameters,
                CachedFile, Completed.CachedColumnNames));
        }
    }
}
```

**Step 3: Integrate `SimulationServiceViewModel` with `TasksPopOverViewModel`**

```csharp
// In SimulationServiceViewModel.SimulateAsync():
var inProgress = new InProgressSimulationViewModel(id, modelName, parameters, _serverFacade);
_TrackingTasks.Add(inProgress);
_TasksPopOver?.AddInProgress(inProgress);

// After download completes:
var completed = new CompletedSimulation(result, modelName);
_CompletedResults.Add(completed);
var completedVm = new CompletedSimulationViewModel(completed, _resultService);
_TasksPopOver?.AddCompleted(completedVm);
_TasksPopOver?.RemoveInProgress(id);
```

**Step 4: Wire `SelectVariablesDialogViewModel.OkCommand`/`CloseCommand`**

```csharp
[RelayCommand]
private async Task Ok()
{
    SelectedYAxis.Clear();
    foreach (var item in YAxisItems)
    {
        if (item.IsSelected)
            SelectedYAxis.Add(item.Value);
    }

    if (Application.Current?.MainWindow is Window window)
    {
        window.DialogResult = true;
    }
}

[RelayCommand]
private async Task Close()
{
    if (Application.Current?.MainWindow is Window window)
    {
        window.DialogResult = false;
    }
}
```

**Step 5: Add `AddInProgress`/`AddCompleted`/`RemoveInProgress` to `TasksPopOverViewModel`**

```csharp
public void AddInProgress(InProgressSimulationViewModel item) => _inProgress.Add(item);
public void AddCompleted(CompletedSimulationViewModel item) => _completed.Add(item);
public void RemoveInProgress(int id)
{
    var item = _inProgress.FirstOrDefault(x => x.Id == id);
    if (item != null) _inProgress.Remove(item);
}
```

#### Acceptance Checklist

- [ ] Abort button stops running simulation via `CancelSimulationAsync`
- [ ] Abort button sets `CanAbort = false` to disable the button
- [ ] Show button opens variable selection dialog, then launches Grin
- [ ] Export button opens CSV save dialog, writes CSV file
- [ ] Remove button removes completed simulation from list
- [ ] Tasks PopOver InProgress list updates during simulation (synced from SimulationService)
- [ ] Tasks PopOver Completed list updates after download (synced from SimulationService)
- [ ] Select Variables dialog OK button closes window with `DialogResult = true`
- [ ] Select Variables dialog Close button closes window with `DialogResult = false`
- [ ] All commands have proper error handling (try/catch)
- [ ] `TasksPopOverViewModel` has `AddInProgress`, `AddCompleted`, `RemoveInProgress` methods

#### Tests

- [ ] `SimulationWorkflowTests_Abort_StopsSimulation` — Verify abort flow via headless UI
- [ ] `SimulationWorkflowTests_ShowChart_OpensPicker` — Verify chart flow via headless UI
- [ ] `SimulationWorkflowTests_ExportCsv_WritesFile` — Verify CSV export via headless UI
- [ ] `SimulationWorkflowTests_CompletedResult_InTasksPopOver` — Verify result appears in Tasks
- [ ] `SelectVariablesDialogTests_Ok_ClosesWithResult` — Verify dialog closure
- [ ] `SelectVariablesDialogTests_Close_Cancels` — Verify cancel closes with false
- [ ] `TaskPopOverViewModelTests_AddInProgress_ItemsAdded` — Verify TasksPopOver sync
- [ ] `TaskPopOverViewModelTests_RemoveInProgress_ItemRemoved` — Verify removal
- [ ] `SimulationServiceViewModelTests_FullFlow_SyncsWithTasksPopOver` — Verify full flow sync

---

### Task 3: State Content Editing — Create New Tabs on Double-Click

**Priority:** Medium
**Phase:** Blueprint Editor
**Complexity:** Medium
**Original Feature:** #28
**Files to Create/Modify:**
- Modify: `src/ISMA.App/Views/BlueprintEditorView.axaml.cs`
- Modify: `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs`

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
    var newProject = _projectService.CreateNewTextProject(state.Name);
    newProject.SetContent(state.Text);
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
        var tabName = $"{loop.State.Name} (loop)";
        var newProject = _projectService.CreateNewTextProject(tabName);
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

**Step 3: Add `CreateNewTextProject` and `SetContent` to `ProjectService`/`IProjectViewModel`**

```csharp
// ProjectService.cs
public IProjectViewModel CreateNewTextProject(string name)
{
    var project = new LismaProjectViewModel(name, _serverFacade, _syntaxHighlighter, _textEditorFactory, _pdeService, _errorModelService);
    _projects.Add(project);
    return project;
}

// IProjectViewModel.cs
void SetContent(string content);

// LismaProjectViewModel.cs
public void SetContent(string content)
{
    // Update the editor content
}
```

#### Acceptance Checklist

- [ ] Double-click on state opens a new tab named after the state
- [ ] Tab content is the state's text
- [ ] Tab close disposes the project
- [ ] Double-click on loop arrowhead opens tab named `"{stateName} (loop)"`
- [ ] Loop tab content is the loop's text
- [ ] Editing loop tab content updates the loop model (via bidirectional binding)
- [ ] State text updates when tab is closed (save back to blueprint model)

#### Tests

- [ ] `BlueprintEditorTests_DoubleClickState_CreatesTab` — Verify tab creation via headless UI
- [ ] `BlueprintEditorTests_DoubleClickLoop_CreatesTab` — Verify loop tab creation via headless UI
- [ ] `BlueprintEditorTests_TabClose_DisposesProject` — Verify disposal via headless UI
- [ ] `BlueprintEditorTests_TabContent_MatchesStateText` — Verify content is correct

---

### Task 4: Simulation Parameters — Send Parallel Settings to Server

**Priority:** Medium
**Phase:** Infrastructure
**Complexity:** Low
**Original Feature:** #30
**Files to Create/Modify:**
- Modify: `src/ISMA.Domain/DtoModels.cs` (RunSimulationParams)
- Modify: `src/ISMA.Infrastructure/Server/GrpcSimulationClient.cs`
- Modify: `src/ISMA.ViewModels/Services/SimulationService.cs`

#### What's Missing

1. **`RunSimulationParams` DTO** has no `IsParallelInUse`, `Server`, or `Port` fields — parallel settings are captured in snapshot but never sent to server
2. **`GrpcSimulationClient.RunSimulationAsync()`** doesn't include parallel config in the gRPC request

#### Implementation Plan

**Step 1: Update `RunSimulationParams` DTO**

```csharp
// In DtoModels.cs
public record RunSimulationParams(
    double StartTime, double EndTime, double InitialStep,
    string MethodName, double Accuracy, bool IsAccuracyInUse,
    bool IsStabilityControlInUse, string CompiledModelId,
    double? EventDetectionGamma, double? EventDetectionLowBorder,
    bool IsParallelInUse, string Server, int Port);
```

**Step 2: Update `GrpcSimulationClient.RunSimulationAsync()`**

```csharp
public async Task<long> RunSimulationAsync(RunSimulationParams parameters)
{
    var request = new RunSimulationRequest
    {
        StartTime = parameters.StartTime,
        EndTime = parameters.EndTime,
        InitialStep = parameters.InitialStep,
        MethodName = parameters.MethodName,
        Accuracy = parameters.Accuracy,
        IsAccuracyInUse = parameters.IsAccuracyInUse,
        IsStabilityControlInUse = parameters.IsStabilityControlInUse,
        CompiledModelId = parameters.CompiledModelId,
        // NEW: Parallel settings
        IsParallelInUse = parameters.IsParallelInUse,
        Server = parameters.IsParallelInUse ? parameters.Server : "",
        Port = parameters.IsParallelInUse ? parameters.Port : 0
    };
    // ...
}
```

**Step 3: Update protobuf definition**

Add `is_parallel_in_use`, `server`, and `port` fields to `RunSimulationRequest` message.

**Step 4: Update `SimulationService.cs` to pass parallel settings**

```csharp
// When calling _serverFacade.RunSimulationAsync():
var runParams = new RunSimulationParams(
    // ... existing fields ...
    IsParallelInUse: _parametersService.Snapshot().IntegrationMethod.IsParallelInUse,
    Server: _parametersService.Snapshot().IntegrationMethod.Server,
    Port: _parametersService.Snapshot().IntegrationMethod.Port
);
```

#### Acceptance Checklist

- [ ] `RunSimulationParams` includes `IsParallelInUse`, `Server`, `Port`
- [ ] gRPC request includes parallel config when `IsParallelInUse` is true
- [ ] gRPC request sends empty server/0 port when `IsParallelInUse` is false
- [ ] `SimulationService` passes parallel settings from snapshot
- [ ] Proto file updated with new fields (if proto files are in scope)

#### Tests

- [ ] `SimulationServiceTests_ParallelSettings_IncludedInRequest` — Verify parallel config sent
- [ ] `SimulationServiceTests_ParallelSettings_Disabled_SendsEmpty` — Verify empty when disabled
- [ ] `GrpcSimulationClientTests_RunSimulation_IncludesParallelFields` — Verify gRPC request

---

### Task 5: Result Simplification — Implement Line Simplification Algorithm

**Priority:** Low
**Phase:** Results Processing
**Complexity:** Medium
**Original Feature:** #31
**Files to Create/Modify:**
- Create: `src/ISMA.Domain/Conversion/ResultSimplifier.cs`
- Modify: `src/ISMA.App/Services/SimulationResultService.cs`

#### What's Missing

1. **No simplification algorithm** — Settings UI exists but `Radial-Distance` and `Douglas-Peucker` are never applied to simulation data
2. **No chart rendering integration** — Even if implemented, simplification would need to be applied before chart display

#### Implementation Plan

**Step 1: Create `ResultSimplifier` domain class**

```csharp
// src/ISMA.Domain/Conversion/ResultSimplifier.cs
using ISMA.Domain.Models;

namespace ISMA.Domain.Conversion;

public static class ResultSimplifier
{
    public static IEnumerable<SimulationPoint> DouglasPeucker(
        IEnumerable<SimulationPoint> points, double tolerance)
    {
        var pointList = points.ToList();
        if (pointList.Count <= 2) return pointList;

        var keep = new bool[pointList.Count];
        keep[0] = true;
        keep[pointList.Count - 1] = true;

        DouglasPeuckerRecursive(pointList, 0, pointList.Count - 1, tolerance, keep);

        return pointList.Where((_, i) => keep[i]);
    }

    private static void DouglasPeuckerRecursive(
        List<SimulationPoint> points, int first, int last, double tolerance, bool[] keep)
    {
        double maxDist = 0;
        int maxIndex = 0;

        for (int i = first + 1; i < last; i++)
        {
            double dist = DistanceToSegment(points[first], points[last], points[i]);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxIndex = i;
            }
        }

        if (maxDist > tolerance)
        {
            keep[maxIndex] = true;
            DouglasPeuckerRecursive(points, first, maxIndex, tolerance, keep);
            DouglasPeuckerRecursive(points, maxIndex, last, tolerance, keep);
        }
    }

    private static double DistanceToSegment(SimulationPoint a, SimulationPoint b, SimulationPoint p)
    {
        double dx = b.X - a.X;
        double dy = b.YForDe.Length > 0 ? b.YForDe[0] - a.YForDe[0] : 0;
        double lenSq = dx * dx + dy * dy;

        if (lenSq == 0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.YForDe[0] - a.YForDe[0]) * (p.YForDe[0] - a.YForDe[0]));

        double t = ((p.X - a.X) * dx + (p.YForDe[0] - a.YForDe[0]) * dy) / lenSq;
        t = Math.Clamp(t, 0, 1);

        double projX = a.X + t * dx;
        double projY = a.YForDe[0] + t * dy;

        return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.YForDe[0] - projY) * (p.YForDe[0] - projY));
    }

    public static IEnumerable<SimulationPoint> RadialDistance(
        IEnumerable<SimulationPoint> points, double tolerance)
    {
        var result = new List<SimulationPoint>();
        var pointList = points.ToList();
        if (pointList.Count == 0) return result;

        result.Add(pointList[0]);
        SimulationPoint lastKept = pointList[0];

        for (int i = 1; i < pointList.Count; i++)
        {
            double dist = Math.Sqrt(
                Math.Pow(pointList[i].X - lastKept.X, 2) +
                Math.Pow(pointList[i].YForDe[0] - lastKept.YForDe[0], 2));

            if (dist >= tolerance)
            {
                result.Add(pointList[i]);
                lastKept = pointList[i];
            }
        }

        // Always keep the last point
        if (!result.Contains(pointList[^1]))
            result.Add(pointList[^1]);

        return result;
    }
}
```

**Step 2: Apply simplification in `SimulationResultService.ShowChartAsync()`**

```csharp
public async Task ShowChartAsync(CompletedSimulation result)
{
    // Read points from binary file
    var points = BinaryFilePointProvider.Read(result.CachedFile);

    // Apply simplification if enabled
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
    var tempFile = WritePointsToTempFile(points);

    // Launch Grin with simplified data
    _grinLauncher.Launch(tempFile, xAxisColumn, chartColumns);
}
```

#### Acceptance Checklist

- [ ] `ResultSimplifier.DouglasPeucker()` correctly removes points within tolerance
- [ ] `ResultSimplifier.RadialDistance()` correctly removes points within tolerance
- [ ] Both algorithms preserve first and last points
- [ ] Tolerance of 0 returns all points unchanged
- [ ] Large tolerance returns minimal points
- [ ] Simplification is applied before chart display when enabled
- [ ] Simplification is NOT applied when disabled
- [ ] `ResultSimplifier` is in `ISMA.Domain` (no external dependencies)

#### Tests

- [ ] `DomainTests_ResultSimplifier_DouglasPeucker_RemovesPoints` — Verify algorithm
- [ ] `DomainTests_ResultSimplifier_DouglasPeucker_PreservesEndpoints` — Verify endpoints kept
- [ ] `DomainTests_ResultSimplifier_DouglasPeucker_ZeroTolerance_ReturnsAll` — Verify tolerance=0
- [ ] `DomainTests_ResultSimplifier_RadialDistance_RemovesPoints` — Verify algorithm
- [ ] `DomainTests_ResultSimplifier_RadialDistance_PreservesEndpoints` — Verify endpoints kept
- [ ] `DomainTests_ResultSimplifier_RadialDistance_ZeroTolerance_ReturnsAll` — Verify tolerance=0

---

### Task 6: PreferencesProvider — Wire LoadFromPreferences/SaveToPreferences

**Priority:** Medium
**Phase:** Infrastructure
**Complexity:** Low
**Files to Modify:** `src/ISMA.Infrastructure/FileStorage/PreferencesProvider.cs`

#### What's Missing

1. **`LoadFromPreferences()`** is empty — does nothing, should load from preferences
2. **`SaveToPreferences()`** is empty — does nothing, should save to preferences
3. **PreferencesProvider** has `Load()`/`Save()` methods but ViewModel service doesn't call them

#### Implementation Plan

**Step 1: Wire `LoadFromPreferences()` and `SaveToPreferences()` in `SimulationParametersService.cs` (ViewModels layer)**

```csharp
// In ISMA.ViewModels/Services/SimulationParametersService.cs
public void LoadFromPreferences(IPreferencesProvider preferencesProvider)
{
    var prefs = preferencesProvider.Load();
    // Apply saved parameters if available
    if (prefs != null && prefs.SavedParameters != null)
    {
        Commit(prefs.SavedParameters);
    }
}

public void SaveToPreferences(IPreferencesProvider preferencesProvider)
{
    var prefs = preferencesProvider.Load() ?? new Preferences();
    prefs.SavedParameters = Snapshot();
    preferencesProvider.Save(prefs);
}
```

**Step 2: Add `SavedParameters` to `Preferences` model if not present**

```csharp
// In ISMA.Domain/Models/PreferencesModels.cs
public record Preferences(
    WindowPreferences WindowPreferences,
    DefaultFilesPreferences DefaultFilesPreferences,
    SimulationParameters? SavedParameters = null);
```

#### Acceptance Checklist

- [ ] `LoadFromPreferences()` loads saved parameters from PreferencesProvider
- [ ] `SaveToPreferences()` saves current parameters to PreferencesProvider
- [ ] `Preferences` model includes `SavedParameters` field
- [ ] Parameters persist across app restarts
- [ ] Default parameters used when no saved parameters exist

#### Tests

- [ ] `PreferencesTests_SavedParameters_PersistsAcrossRestarts` — Verify persistence
- [ ] `SimulationParametersServiceTests_LoadFromPreferences_AppiesValues` — Verify load
- [ ] `SimulationParametersServiceTests_SaveToPreferences_SavesValues` — Verify save

---

### Task 7: Error Handling & User Feedback

**Priority:** Medium
**Phase:** Polish
**Complexity:** Medium
**Files to Create/Modify:**
- Create: `src/ISMA.App/Services/DialogService.cs`
- Modify: `src/ISMA.App/Services/ProjectFileService.cs`
- Modify: `src/ISMA.ViewModels/ViewModels/SimulationServiceViewModel.cs`

#### What's Missing

1. **No error dialogs** for server connection failures
2. **No progress feedback** during long operations (CSV export, file save)
3. **No toast/notification** for save success/failure
4. **No loading indicators** during simulation

#### Implementation Plan

**Step 1: Create `DialogService` for consistent error/success dialogs**

```csharp
// src/ISMA.App/Services/DialogService.cs
using Avalonia;
using Avalonia.Controls;

namespace ISMA.App.Services;

public interface IDialogService
{
    Task ShowErrorAsync(string message, string title = "Error");
    Task ShowSuccessAsync(string message, string title = "Success");
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");
}

public class DialogService : IDialogService
{
    public async Task ShowErrorAsync(string message, string title = "Error")
    {
        var owner = Application.Current?.MainWindow;
        await MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public async Task ShowSuccessAsync(string message, string title = "Success")
    {
        var owner = Application.Current?.MainWindow;
        await MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
    {
        var owner = Application.Current?.MainWindow;
        var result = await MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}
```

**Step 2: Wire error handling in `SimulationServiceViewModel.SimulateAsync()`**

```csharp
private readonly IDialogService? _dialogService;

public async Task SimulateAsync()
{
    try
    {
        // ... existing simulation flow ...
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        await _dialogService?.ShowErrorAsync($"Simulation failed: {ex.Message}");
    }
}
```

**Step 3: Wire error handling in file operations**

```csharp
// In ProjectFileService.cs
public async Task<bool> SaveAsync(IProjectViewModel project, IDialogService dialogService)
{
    try
    {
        // ... save logic ...
        await dialogService.ShowSuccessAsync("Project saved successfully");
        return true;
    }
    catch (Exception ex)
    {
        await dialogService.ShowErrorAsync($"Failed to save project: {ex.Message}");
        return false;
    }
}
```

#### Acceptance Checklist

- [ ] Server connection error shows user-friendly dialog
- [ ] gRPC errors displayed in error list AND shown as dialog
- [ ] File I/O errors show error dialog
- [ ] CSV export shows progress feedback
- [ ] Simulation compilation shows loading state
- [ ] Save operations show success notification
- [ ] `DialogService` is registered in DI container
- [ ] All error handling uses `IDialogService` interface (testable)

#### Tests

- [ ] `DialogServiceTests_ShowError_DisplaysDialog` — Verify error dialog
- [ ] `DialogServiceTests_ShowSuccess_DisplaysDialog` — Verify success dialog
- [ ] `DialogServiceTests_ShowConfirmation_ReturnsTrue` — Verify confirmation Yes
- [ ] `DialogServiceTests_ShowConfirmation_ReturnsFalse` — Verify confirmation No
- [ ] `SimulationServiceViewModelTests_SimulationError_ShowsDialog` — Verify error dialog on failure

---

### Task 8: Select All Command

**Priority:** Low
**Phase:** Menu Bar
**Complexity:** Low
**Original Feature:** #26 (clipboard)
**Files to Create/Modify:**
- Modify: `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ISMA.ViewModels/ViewModels/IProjectViewModel.cs`
- Modify: `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs`

#### What's Missing

1. **Menu item exists** in context menu but `SelectAllCommand` not wired in `MainWindowViewModel`

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
// IProjectViewModel.cs
void TriggerSelectAll();

// LismaProjectViewModel.cs
public void TriggerSelectAll()
{
    _editorInstance?.SelectAll();
}
```

#### Acceptance Checklist

- [ ] Select All command exists in MainWindowViewModel
- [ ] Select All selects all text in focused editor
- [ ] Works via context menu and keyboard shortcut (Ctrl+A)
- [ ] No-op when no active project or blueprint project

#### Tests

- [ ] `MainWindowViewModelTests_SelectAllCommand_Exists` — Verify command exists
- [ ] `TextEditorIntegrationTests_SelectAll_SelectsAllText` — Verify selection via headless UI

---

### Task 9: Pseudo-class Styles

**Priority:** Low
**Phase:** Polish
**Complexity:** Low
**Files to Modify:** `src/ISMA.App/Styles/GlobalStyles.axaml`

#### What's Missing

Global styles exist in `GlobalStyles.axaml` but are missing pseudo-class styles for:

1. **Button hover/pressed states** — `:pointerover`, `:pressed`
2. **MenuItem hover state** — `:pointerover`
3. **TabItem selected state** — custom styling
4. **DataGrid row hover/selected** — custom styling

#### Implementation Plan

Add pseudo-class styles to `GlobalStyles.axaml`:
```axaml
<!-- Button hover/pressed -->
<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="#E8E8E8"/>
</Style>
<Style Selector="Button:pressed">
    <Setter Property="Background" Value="#D0D0D0"/>
</Style>

<!-- MenuItem hover -->
<Style Selector="MenuItem:pointerover">
    <Setter Property="Background" Value="#E8E8E8"/>
</Style>

<!-- TabItem selected -->
<Style Selector="TabControl TabItem:selected">
    <Setter Property="Background" Value="#F5F5F5"/>
</Style>

<!-- DataGrid row hover/selected -->
<Style Selector="DataGridRow:pointerover">
    <Setter Property="Background" Value="#F5F5F5"/>
</Style>
<Style Selector="DataGridRow:selected">
    <Setter Property="Background" Value="#E0E0FF"/>
</Style>

<!-- TextBox focus -->
<Style Selector="TextBox:focus">
    <Setter Property="BorderBrush" Value="#0078D7"/>
</Style>
```

#### Acceptance Checklist

- [ ] Buttons show hover effect (lighter background)
- [ ] Buttons show pressed effect (darker background)
- [ ] Menu items show hover highlight
- [ ] Tab items have distinct selected state
- [ ] DataGrid rows show hover and selected states
- [ ] TextBox shows focus border
- [ ] All pseudo-classes work on Linux (primary platform)

#### Tests

- [ ] `StyleTests_ButtonHover_VisualFeedback` — Verify hover style applied
- [ ] `StyleTests_MenuItemHover_VisualFeedback` — Verify menu hover
- [ ] `UiComponentTests_StylesApplied` — Verify styles exist in UI

---

### Task 10: Result Download — Fix Column Names

**Priority:** Medium
**Phase:** Infrastructure
**Complexity:** Low
**Original Feature:** #16
**Files to Modify:** `src/ISMA.Infrastructure/Server/GrpcSimulationClient.cs`

#### What's Missing

`DownloadResultAsync` returns `ColumnNames = ImmutableArray<string>.Empty` — column names are never populated from the gRPC response. This means the variable selection dialog has no columns to show.

#### Implementation Plan

**Step 1: Update `GrpcSimulationClient.DownloadResultAsync()` to parse column names**

```csharp
public async Task<CachedSimulationResult> DownloadResultAsync(long simulationId)
{
    var response = await _client.GetSimulationResultAsync(new GetSimulationResultRequest
    {
        SimulationId = simulationId
    });

    var downloadUrl = response.DownloadUrl;
    var columnNames = response.ColumnNames.Count > 0
        ? response.ColumnNames.ToList().ToImmutableArray()
        : ParseColumnNamesFromBinary(downloadUrl);

    var localPath = await DownloadToFileAsync(downloadUrl, simulationId);

    return new CachedSimulationResult(localPath, columnNames);
}

private ImmutableArray<string> ParseColumnNamesFromBinary(string filePath)
{
    var metadata = BinaryFilePointProvider.ReadMetadata(filePath);
    return metadata.ColumnNames.ToImmutableArray();
}
```

**Step 2: Update protobuf definition**

Add `repeated string column_names` field to `GetSimulationResultResponse` message.

#### Acceptance Checklist

- [ ] `DownloadResultAsync` returns column names from gRPC response if available
- [ ] Falls back to parsing binary file metadata if gRPC response is empty
- [ ] Column names appear in Select Variables dialog
- [ ] TIME column is present and pre-selected

#### Tests

- [ ] `GrpcSimulationClientTests_DownloadResult_ReturnsColumnNames` — Verify column names from gRPC
- [ ] `GrpcSimulationClientTests_DownloadResult_FallbackToBinaryMetadata` — Verify fallback
- [ ] `SelectVariablesDialogTests_ColumnsPopulated` — Verify columns appear in dialog

---

## Implementation Priority Order

| # | Task | Priority | Estimated Effort | Status | Original Features |
|---|------|----------|-----------------|--------|-------------------|
| 1 | LISMA Syntax Highlighting | High | 1-2 days | ⏸ Deferred | #2, #3 |
| 2 | Tasks PopOver Commands | High | 2-3 days | ✅ Completed | #15, #20, #22, #27 |
| 3 | State Content Editing | Medium | 0.5-1 day | ✅ Completed | #28 |
| 4 | Parallel Settings to Server | Medium | 0.5 day | ✅ Completed | #30 |
| 5 | Result Download Column Names | Medium | 0.5 day | ✅ Completed | #16 |
| 6 | PreferencesProvider Wire | Medium | 0.5 day | ✅ Completed | #23 |
| 7 | Error Handling & Feedback | Medium | 1 day | ✅ Completed | All features |
| 8 | Result Simplification | Low | 1-2 days | ✅ Completed | #31 |
| 9 | Select All Command | Low | 0.25 day | ✅ Completed | #26 |
| 10 | Pseudo-class Styles | Low | 0.5 day | Pending | All UI |

---

## Testing Strategy

All new features should be covered with:

1. **Unit tests** (`ISMA.Tests`) — ViewModel logic, domain logic, service logic
2. **Integration tests** (`ISMA.Tests.Integration`) — End-to-end business flows with real UI components
3. **Headless Avalonia tests** — Using `Avalonia.Headless.XUnit` for UI interaction testing

### Integration test patterns to follow:

- Use `IntegrationTestBase` for headless Avalonia setup with mocked server
- Use `UiHelpers` for UI interactions (click buttons, set text, find controls)
- Test through the actual UI control tree, not just ViewModels
- Use `window.Flush()` to force layout updates in headless mode
- Use `AutomationId` properties for reliable control identification

### Test project structure:

```
ISMA.Tests/
├── Domain/                    # Pure domain tests
│   ├── ResultSimplifierTests.cs    # NEW: simplification algorithms
│   └── ... (existing)
├── ViewModels/               # ViewModel unit tests
│   ├── SimulationWorkflowTests.cs  # NEW: full simulation flow
│   ├── TaskPopOverViewModelTests.cs # NEW: TasksPopOver sync
│   └── ... (existing)
└── Integration/              # Headless Avalonia integration tests
    ├── SimulationWorkflowTests.cs      # NEW: full flow end-to-end
    ├── SyntaxHighlightingTests.cs      # NEW: syntax highlighting
    ├── StateContentEditingTests.cs     # NEW: double-click tab creation
    └── ... (existing)

ISMA.Tests.Integration/
├── SimulationWorkflowTests.cs        # Existing + NEW: abort, export, chart
├── BlueprintEditorTests.cs           # Existing + NEW: double-click editing
├── ValidationTests.cs                # Existing
├── ProjectManagementTests.cs         # Existing
└── ... (existing)
```

---

## Files to Create/Modify

### New Files (Created)
- `src/ISMA.App/Services/LismaSyntaxHighlighting.cs` — AvaloniaEdit tokenizer (deferred)
- `src/ISMA.Domain/Conversion/ResultSimplifier.cs` — Line simplification algorithms ✅
- `src/ISMA.App/Services/DialogService.cs` — Error/success/confirmation dialogs ✅
- `tests/ISMA.Tests/Domain/ResultSimplifierTests.cs` — Simplification unit tests ✅
- `tests/ISMA.Tests.Integration/TasksPopOverCommandTests.cs` — Tasks PopOver integration tests ✅
- `tests/ISMA.Tests.Integration/SelectAllCommandTests.cs` — Select All integration tests ✅

### Modified Files (All Modified)
- `src/ISMA.App/Services/TextEditorFactory.cs` — Wire syntax highlighting (deferred)
- `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` — Add debouncing / TriggerSelectAll ✅
- `src/ISMA.ViewModels/ViewModels/InProgressSimulationViewModel.cs` — Wire AbortCommand ✅
- `src/ISMA.ViewModels/ViewModels/CompletedSimulationViewModel.cs` — Wire Show/Export/Remove commands ✅
- `src/ISMA.ViewModels/ViewModels/SelectVariablesDialogViewModel.cs` — Wire Ok/Close commands ✅
- `src/ISMA.ViewModels/ViewModels/SimulationServiceViewModel.cs` — Integrate with TasksPopOverViewModel ✅
- `src/ISMA.ViewModels/ViewModels/TasksPopOverViewModel.cs` — Add AddInProgress/AddCompleted/RemoveInProgress ✅
- `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` — Add CreateNewTextProject/SetContent ✅
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — Wire double-click tab creation ✅
- `src/ISMA.Domain/DtoModels.cs` — Add parallel fields to RunSimulationParams ✅
- `src/ISMA.Domain/Models/PreferencesModels.cs` — Add SavedParameters field ✅
- `src/ISMA.Infrastructure/Server/GrpcSimulationClient.cs` — Fix column names + Send parallel config ✅
- `src/ISMA.Infrastructure/run_simulation_request.proto` — Added parallel fields ✅
- `src/ISMA.Infrastructure/get_simulation_result_response.proto` — Added column_names field ✅
- `src/ISMA.ViewModels/Services/SimulationParametersService.cs` — Wire LoadFromPreferences/SaveToPreferences ✅
- `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` — Add SelectAllCommand ✅
- `src/ISMA.ViewModels/ViewModels/IProjectViewModel.cs` — Add TriggerSelectAll/SetContent ✅
- `src/ISMA.App/Styles/GlobalStyles.axaml` — Add pseudo-class styles (pending)

---

## Dependencies Between Tasks

```
Task 1 (Syntax Highlighting) → independent (deferred)
Task 2 (Tasks PopOver) → ✅ completed (was dependent on Task 4)
Task 3 (State Content Editing) → independent
Task 4 (Parallel Settings) → independent
Task 5 (Result Simplification) → independent
Task 6 (PreferencesProvider) → independent
Task 7 (Error Handling) → independent
Task 8 (Select All) → independent
Task 9 (Pseudo-class Styles) → independent
Task 10 (Column Names) → independent
```

**Remaining work:**
1. Task 9 (Pseudo-class Styles) — small, standalone
2. Task 1 (Syntax Highlighting) — investigate Avalonia 12 text editing API
