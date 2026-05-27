# ISMA UI Avalonia — Gap Analysis & Implementation Plan

This document identifies what's missing from the original ISMA JavaFX application that hasn't been fully implemented in the Avalonia migration, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

| Status | Count | Details |
|--------|-------|---------|
| **Fully Implemented** | 29 | Multi-project tabs, blueprint canvas (states/arrows/loops), drag-and-drop, inline name editing, edit popover, blueprint-to-LISMA conversion, compile/validate/run, progress monitoring, result download, error list, settings panel, store/load settings, chart viewer UI, variable selection dialog UI, window persistence, menu/toolbar/shortcuts, clipboard propagation, blueprint editor modes, simulation abort, show/export/remove results, state content editing, variable dialog OK/Close, parallel settings to server, result simplification, Tasks PopOver integration, preferences persistence, error handling & feedback, Select All command, result download column names, pseudo-class styles |
| **Partially Implemented** | 2 | Syntax highlighting (server tokens fetched but not rendered), Loop arrow double-click (opens PopOver instead of text tab) |
| **Not Implemented** | 1 | CSV Export file dialog (uses cached path instead of user-selected path) |

---

## Feature-by-Feature Status (Original 31 Features)

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 1 | Multi-project editing (tabs) | **FULLY IMPLEMENTED** | Full CRUD lifecycle, dirty tracking, last-opened-file restoration |
| 2 | LISMA text editing with syntax highlighting | **PARTIAL** | AvaloniaEdit with line numbers works. Server tokens fetched but **never rendered** as colored spans |
| 3 | Remote syntax highlighting (server-driven) | **PARTIAL** | gRPC call returns tokens, but no bridge to AvaloniaEdit for rendering |
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
| 20 | Chart visualization (Grin process) | **FULLY IMPLEMENTED** | Grin launcher, SelectVariables dialog, OK/Close wiring |
| 21 | Variable axis selection dialog | **FULLY IMPLEMENTED** | UI complete, Ok/Close commands wired, dialog closes with result |
| 22 | CSV export of results | **PARTIAL** | ExportToFile() logic exists but `ExportCommand` uses cached file path instead of FileDialog |
| 23 | Window state persistence | **FULLY IMPLEMENTED** | Geometry saved/restored via PreferencesProvider |
| 24 | Menu bar and toolbar commands | **FULLY IMPLEMENTED** | All 15 commands wired with InputBindings |
| 25 | Keyboard shortcuts | **FULLY IMPLEMENTED** | All shortcuts defined via InputBindings |
| 26 | Clipboard propagation (cut/copy/paste) | **FULLY IMPLEMENTED** | EditorPlatformService with Cut/Copy/Paste propagation |
| 27 | Tasks PopOver (in-progress + completed) | **FULLY IMPLEMENTED** | Abort/Show/Export/Remove commands wired, synced with SimulationService |
| 28 | State content editing (double-click → text tab) | **PARTIAL** | State double-click creates tab ✅. Loop arrow double-click opens PopOver only ❌ |
| 29 | Name uniqueness enforcement | **FULLY IMPLEMENTED** | NameChangingMonitor with auto-increment |
| 30 | Parallel execution settings | **FULLY IMPLEMENTED** | Server/Port fields in RunSimulationParams, sent to gRPC |
| 31 | Result simplification settings | **FULLY IMPLEMENTED** | ResultSimplifier with Douglas-Peucker and Radial-Distance algorithms |

---

## Remaining Implementation Tasks

### Task 1: LISMA Text Editor Syntax Highlighting (Server-Driven)

**Priority:** High
**Phase:** Text Editor
**Complexity:** Medium
**Original Features:** #2, #3
**Status:** ⏳ TODO

#### What's Missing

1. **Server-highlighting tokens never rendered** — `SyntaxHighlighterService.HighlightSource()` returns `SyntaxTokenDto[]`, `LismaProjectViewModel.UpdateSyntaxHighlighting()` receives them, but `TextEditorFactory.SetSyntaxHighlighting()` is a stub that only sets `HighlightCurrentLine = true`
2. **No AvaloniaEdit tokenizer** — No implementation that converts `SyntaxTokenDto[]` to AvaloniaEdit `IHighlightingDefinition`
3. **No debouncing** — Text changes trigger highlighting immediately without delay
4. **No client-side fallback** — The existing `LISMA.xshd` embedded resource is never loaded as fallback

#### Implementation Plan

**Step 1: Create `LismaServerDrivenHighlighting` implementing `IHighlightingDefinition`**

```csharp
// src/ISMA.App/Services/LismaServerDrivenHighlighting.cs
using AvaloniaEdit.Highlighting;
using ISMA.Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace ISMA.App.Services;

/// <summary>
/// Server-driven syntax highlighting for AvaloniaEdit.
/// Converts SyntaxTokenDto[] from the server into an IHighlightingDefinition
/// that AvaloniaEdit uses to colorize the text editor.
/// </summary>
public class LismaServerDrivenHighlighting : IHighlightingDefinition
{
    private readonly IReadOnlyList<SyntaxTokenDto> _tokens;
    private readonly IReadOnlyDictionary<string, HighlightingColor> _colorCache;

    public LismaServerDrivenHighlighting(IReadOnlyList<SyntaxTokenDto> tokens)
    {
        _tokens = tokens;
        Name = "LismaServerDriven";
        _colorCache = new Dictionary<string, HighlightingColor>
        {
            { "Keyword", new HighlightingColor { Name = "Keyword", Foreground = Media.Brushes.Orange } },
            { "Comment", new HighlightingColor { Name = "Comment", Foreground = Media.Brushes.Gray } },
            { "Number", new HighlightingColor { Name = "Number", Foreground = Media.Brushes.Blue } },
            { "Text", new HighlightingColor { Name = "Text", Foreground = Media.Brushes.Green } },
            { "Default", new HighlightingColor { Name = "Default", Foreground = Media.Brushes.Black } }
        };
    }

    public string Name { get; }

    public HighlightingRuleSet MainRuleSet => new HighlightingRuleSet
    {
        Spans = _tokens
            .Where(t => t.Kind != Domain.Dtos.SyntaxTokenKind.Unspecified)
            .Select(token => new HighlightingSpan(
                new HighlightingPattern(token.Start.ToString()),
                new HighlightingPattern((token.Start + token.Length).ToString()),
                GetColorForKind(token.Kind)))
            .ToList()
    };

    public HighlightingRuleSet GetNamedRuleSet(string name) => MainRuleSet;

    public HighlightingColor GetNamedColor(string name) =>
        _colorCache.GetValueOrDefault(name) ?? _colorCache["Default"];

    public IEnumerable<HighlightingColor> NamedHighlightingColors => _colorCache.Values;

    public IReadOnlyDictionary<string, string> Properties => new Dictionary<string, string>();

    private static HighlightingColor GetColorForKind(Domain.Dtos.SyntaxTokenKind kind) => kind switch
    {
        Domain.Dtos.SyntaxTokenKind.Keyword => GetNamedColor("Keyword"),
        Domain.Dtos.SyntaxTokenKind.Comment => GetNamedColor("Comment"),
        Domain.Dtos.SyntaxTokenKind.Number => GetNamedColor("Number"),
        Domain.Dtos.SyntaxTokenKind.Text => GetNamedColor("Text"),
        _ => GetNamedColor("Default")
    };
}
```

**Step 2: Fix `TextEditorFactory.SetSyntaxHighlighting()`**

Replace stub with actual token-to-UI bridge:
```csharp
public void SetSyntaxHighlighting(object editor, SyntaxTokenDto[] tokens, string source)
{
    if (editor is not TextEditor te) return;
    te.Options.HighlightCurrentLine = true;

    if (tokens.Length > 0)
    {
        var highlighting = new LismaServerDrivenHighlighting(tokens);
        te.SyntaxHighlighting = highlighting;
    }
    else
    {
        // Load embedded LISMA.xshd as fallback
        te.SyntaxHighlighting = LoadFallbackHighlighting();
    }
}

private static IHighlightingDefinition? _fallbackHighlighting;

private static IHighlightingDefinition LoadFallbackHighlighting()
{
    if (_fallbackHighlighting != null) return _fallbackHighlighting;

    using var stream = typeof(TextEditorFactory).Assembly
        .GetManifestResourceStream("ISMA.App.Assets.LISMA.xshd");
    if (stream != null)
    {
        using var reader = new StreamReader(stream);
        var xshd = XshdSyntaxDefinition.Load(reader);
        _fallbackHighlighting = xshd.Compile();
        HighlightingManager.Instance.RegisterDefinition("LISMA", _fallbackHighlighting);
    }
    return _fallbackHighlighting;
}
```

**Step 3: Add debouncing in `LismaProjectViewModel.UpdateSyntaxHighlighting()`**

Add 100ms `DispatcherTimer` to debounce highlighting updates:
```csharp
private DispatcherTimer? _highlightingTimer;

public void UpdateSyntaxHighlighting(SyntaxTokenDto[] tokens)
{
    _highlightingTimer?.Stop();
    _highlightingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    _highlightingTimer.Tick += (s, e) =>
    {
        _highlightingTimer!.Stop();
        _editorFactory.SetSyntaxHighlighting(_editorInstance, tokens, FullText);
    };
    _highlightingTimer.Start();
}
```

#### Acceptance Checklist

- [ ] `LismaServerDrivenHighlighting` implements `IHighlightingDefinition` correctly
- [ ] Server tokens are applied to AvaloniaEdit `TextEditor` via `SetSyntaxHighlighting()`
- [ ] Keywords appear orange and bold
- [ ] Comments appear gray and italic
- [ ] Numbers appear blue
- [ ] Text appears green
- [ ] Highlighting updates with 100ms debounce (no jank during typing)
- [ ] Client-side fallback loads from embedded `LISMA.xshd` if no server tokens
- [ ] `ISMA.App.csproj` includes `LISMA.xshd` as `EmbeddedResource`

#### Tests

- [ ] `TextEditorIntegrationTests_SyntaxHighlighting_TokensApplied` — Verify tokens render correctly
- [ ] `TextEditorIntegrationTests_HighlightingDebounce` — Verify debouncing
- [ ] `TextEditorIntegrationTests_FallbackSyntax_Loads` — Verify fallback xshd loads
- [ ] `ViewModelTests_LismaProjectViewModel_HighlightingApplied` — Verify VM calls factory

---

### Task 2: Loop Arrow Double-Click → Text Editor Tab

**Priority:** High
**Phase:** Blueprint Editor
**Complexity:** Low
**Original Feature:** #28
**Status:** ⏳ TODO

#### What's Missing

1. **`OnLoopArrowHeadClicked`** only opens the Edit Arrow PopOver — it does NOT create a text editor tab for loop content editing on double-click
2. **No tab naming** — Original creates tabs named `"{stateName} (loop)"` for loop content
3. **No bidirectional binding** — Loop text should update when tab is edited and saved back to the blueprint model

#### Implementation Plan

**Step 1: Modify `BlueprintEditorView.OnLoopArrowHeadClicked()`**

Detect double-click and create a text editor tab instead of the PopOver:
```csharp
private void OnLoopArrowHeadClicked(object? sender, LoopArrow arrow)
{
    var vm = DataContext as BlueprintEditorViewModel;
    if (vm is null || arrow.State == null) return;

    var loop = vm.LoopTransactions.FirstOrDefault(l => l.State == arrow.State);
    if (loop == null) return;

    var window = FindWindow();
    if (window == null) return;

    var mainWindowVm = window.DataContext as MainWindowViewModel;
    if (mainWindowVm == null) return;

    var projectServiceField = mainWindowVm.GetType()
        .GetField("_projectService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var projectService = projectServiceField?.GetValue(mainWindowVm) as ProjectService;
    if (projectService == null) return;

    var tabName = $"{loop.State.Name} (loop)";
    var newProject = projectService.CreateNewTextProject(tabName);
    newProject.SetContent(loop.Text);
    mainWindowVm.ActiveProject = newProject;

    // Update loop text when tab content changes
    if (newProject is LismaProjectViewModel lismaProject)
    {
        lismaProject.ContentChanged += (text) => { loop.Text = text; };
    }
}
```

**Step 2: Add `ContentChanged` event to `LismaProjectViewModel`**

```csharp
public event Action<string>? ContentChanged;

private void OnTextChanged(string text)
{
    _fullText = text;
    ContentChanged?.Invoke(text);
}
```

#### Acceptance Checklist

- [ ] Double-click on loop arrowhead opens a new tab named `"{stateName} (loop)"`
- [ ] Tab content is the loop's text
- [ ] Tab close disposes the project
- [ ] Editing loop tab content updates the loop model (via ContentChanged)
- [ ] Single-click on loop arrowhead still opens the Edit PopOver

#### Tests

- [ ] `BlueprintEditorTests_DoubleClickLoop_CreatesTab` — Verify loop tab creation via headless UI
- [ ] `BlueprintEditorTests_TabClose_DisposesProject` — Verify disposal via headless UI
- [ ] `BlueprintEditorTests_LoopTabContent_MatchesLoopText` — Verify content is correct
- [ ] `BlueprintEditorTests_LoopTabContentUpdate_SyncsBack` — Verify bidirectional binding

---

### Task 3: CSV Export with FileDialog

**Priority:** Medium
**Phase:** Results
**Complexity:** Low
**Original Feature:** #22
**Status:** ⏳ TODO

#### What's Missing

1. **`CompletedSimulationViewModel.ExportCommand`** calls `_resultService.ExportToFile(_source, CachedFile)` — uses the cached binary file path instead of a user-selected CSV output path
2. **No FileDialog** for CSV export destination — Original app opens a file picker with `*.csv` filter

#### Implementation Plan

**Step 1: Add `ExportToFile` overload with filePath parameter to `ISimulationResultService`**

```csharp
// In ServiceContracts.cs
Task ExportToFile(CompletedSimulation simulation, string outputPath);
```

**Step 2: Update `CompletedSimulationViewModel.ExportCommand`**

Use `IDialogService` to get the export path:
```csharp
[RelayCommand]
private async Task Export()
{
    if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
    {
        // Open file picker for CSV export
        var owner = Application.Current?.MainWindow;
        if (owner is Window window)
        {
            var dialog = new FileDialog
            {
                Title = "Export CSV",
                Filters = { new FileDialogFilter { Name = "CSV", Extensions = ["csv"] } },
                DefaultExtension = "csv"
            };
            var result = await dialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(result))
            {
                await _resultService.ExportToFile(_source, result);
                await _dialogService?.ShowSuccessAsync("CSV exported successfully");
            }
        }
    }
}
```

**Step 3: Inject `IDialogService` into `CompletedSimulationViewModel`**

```csharp
public partial class CompletedSimulationViewModel : ObservableObject
{
    private readonly IDialogService? _dialogService;

    public CompletedSimulationViewModel(
        CompletedSimulation source,
        ISimulationResultService? resultService = null,
        TasksPopOverViewModel? tasksPopOver = null,
        IDialogService? dialogService = null)
    {
        _source = source;
        _resultService = resultService;
        _tasksPopOver = tasksPopOver;
        _dialogService = dialogService;
        // ...
    }
}
```

#### Acceptance Checklist

- [ ] Export button opens a FileDialog with `*.csv` filter
- [ ] User can select the CSV output path
- [ ] CSV file is written with correct header and data rows
- [ ] Success dialog shown after export completes
- [ ] Export runs on background thread (non-blocking)

#### Tests

- [ ] `SimulationWorkflowTests_ExportCsv_OpensFileDialog` — Verify file dialog opens
- [ ] `SimulationWorkflowTests_ExportCsv_WritesFile` — Verify CSV content
- [ ] `SimulationWorkflowTests_ExportCsv_Cancelled_DoesNotThrow` — Verify cancel handling

---

## Implementation Priority Order

| # | Task | Priority | Estimated Effort | Status | Original Features |
|---|------|----------|-----------------|--------|-------------------|
| 1 | LISMA Syntax Highlighting | High | 1-2 days | ⏳ TODO | #2, #3 |
| 2 | Loop Arrow Double-Click | High | 0.5 day | ⏳ TODO | #28 |
| 3 | CSV Export FileDialog | Medium | 0.5 day | ⏳ TODO | #22 |

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
│   └── ... (existing)
├── ViewModels/               # ViewModel unit tests
│   └── ... (existing)
└── Integration/              # Headless Avalonia integration tests
    └── ... (existing)

ISMA.Tests.Integration/
├── SimulationWorkflowTests.cs        # Existing + NEW: export with file dialog
├── BlueprintEditorTests.cs           # Existing + NEW: loop double-click
├── SyntaxHighlightingTests.cs        # NEW: server-driven highlighting
├── ValidationTests.cs                # Existing
├── ProjectManagementTests.cs         # Existing
├── TasksPopOverCommandTests.cs       # Existing
└── ... (existing)
```

---

## Files to Create/Modify

### New Files (To Create)
- `src/ISMA.App/Services/LismaServerDrivenHighlighting.cs` — Server-driven AvaloniaEdit highlighting
- `tests/ISMA.Tests.Integration/SyntaxHighlightingTests.cs` — Syntax highlighting integration tests
- `tests/ISMA.Tests.Integration/LoopArrowEditingTests.cs` — Loop arrow double-click tests
- `tests/ISMA.Tests.Integration/ExportCsvTests.cs` — CSV export with FileDialog tests

### Modified Files (To Modify)
- `src/ISMA.App/Services/TextEditorFactory.cs` — Wire server-driven syntax highlighting + fallback
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — Loop arrow double-click → text tab
- `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` — Add ContentChanged event
- `src/ISMA.ViewModels/ViewModels/CompletedSimulationViewModel.cs` — Wire Export with FileDialog
- `src/ISMA.ViewModels/ViewModels/CompletedSimulationViewModel.cs` — Inject IDialogService
- `src/ISMA.Domain/Contracts/ServiceContracts.cs` — ExportToFile signature (if needed)
