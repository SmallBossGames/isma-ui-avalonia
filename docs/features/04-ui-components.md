# UI Components

## Purpose

The `ISMA.App` assembly is the application layer — it contains the Avalonia UI components, business logic services, project models, and the application entry point. It wires together the domain models, infrastructure services, and ViewModels into a cohesive desktop application.

## Structure

```
ISMA.App/
├── Program.cs                               # Application entry point
├── App.axaml / App.axaml.cs                 # Application resources + DI configuration
├── MainWindow.axaml / MainWindow.axaml.cs   # Main window layout + geometry persistence
├── Views/
│   ├── IsmaMenuBarView.axaml                # Menu bar (File, Edit, Simulation menus)
│   ├── IsmaToolBarView.axaml                # Toolbar buttons
│   ├── EditorTabPaneView.axaml              # Tab-based project management
│   ├── IsmaTextEditorView.axaml             # AvaloniaEdit-based LISMA editor
│   ├── BlueprintEditorView.axaml            # Visual statechart editor canvas
│   ├── EditArrowPopOverView.axaml           # Floating arrow edit dialog
│   ├── SettingsPanelView.axaml              # Right sidebar settings panel
│   ├── IsmaErrorListTableView.axaml         # Error list DataGrid
│   ├── SimulationProcessBarView.axaml       # Bottom process bar
│   ├── TasksPopOverView.axaml               # In-progress + completed simulations
│   └── SelectVariablesDialogWindow.axaml    # Chart variable selector dialog
├── Controls/
│   ├── PropertiesGrid.axaml                 # Dynamic property grid from ViewModel
│   ├── ArrowLine.cs                         # Custom control: transaction arrow
│   └── LoopArrow.cs                         # Custom control: loop arrow
├── Services/
│   ├── ProjectFileService.cs                # File open/save operations
│   ├── ServerDrivenHighlightingTransformer.cs  # Server-driven syntax highlighting
│   ├── LismaSyntaxHelper.cs                 # Embedded XSHD fallback highlighting
│   ├── TextEditorFactory.cs                 # TextEditor instance management
│   ├── SimulationResultService.cs           # Chart launch + CSV export
│   ├── SimulationParametersService.cs       # Parameters store service
│   ├── EditorPlatformService.cs             # Cut/copy/paste propagation
│   └── DialogService.cs                     # Dialog abstraction
├── Styles/
│   └── GlobalStyles.axaml                   # Theme: colors, fonts, widget styles
└── Automation/
    └── AutomationIds.cs                     # Centralized automation identifiers
```

## Module Configuration

**File:** `ISMA.App/ISMA.App.csproj`

Key packages:
- `Avalonia.Themes.Fluent` — Fluent theme
- `Avalonia.Fonts.Inter` — Inter font family
- `ICSharpCode.AvaloniaEdit` — Rich text editor (Avalonia port of AvalonEdit)
- `CommunityToolkit.Mvvm` — MVVM source generation
- `Microsoft.Extensions.DependencyInjection` — DI container
- `Avalonia.EmbeddedIcon` — Icon support

## Application Entry Point

### Program

**File:** `Program.cs`

```csharp
public class Program
{
    public static AppBuilder BuildAvaloniaApp(PlatformDetection platform)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### App

**File:** `App.axaml` — Application-level resources:
- Merged `GlobalStyles.axaml` (theme, colors, fonts)
- AvaloniaEdit Fluent theme (syntax highlighting colors)
- View model converters (`BoolToVisibilityConverter`, `ModeToVisibilityConverter`, etc.)

**File:** `App.axaml.cs` — DI configuration:
- Reads `appsettings.json` for server and Grin script paths
- Configures `IServiceCollection` with all services and ViewModels
- Creates `MainWindowViewModel` and `MainWindow`

## Main Window Layout

**File:** `MainWindow.axaml`

Three-row `Grid` layout:

```
┌────────────────────────────────────────────────────────────────────┐
│ Menu Bar: [File] [Edit] [Simulation]                              │
│ Toolbar: [New] [Blueprint] [Open] [Save] [SaveAll] │ [Cut] [Copy] │
├────────────────────────────────────────────────────────────────────┤
│                                          ┌────────────────────┐  │
│                                          │ Settings Panel     │  │
│  ┌─────────────────────────────────────┐ │ [Initials]         │  │
│  │                                     │ │ [Integration]      │  │
│  │  EditorTabPaneView (TabControl)     │ │ [Event detection]  │  │
│  │  ┌───────────────────────────────┐  │ │ [Result proc.]     │  │
│  │  │ [New project | Tab 2 | ...]   │  │ │ [Saving]           │  │
│  │  ├───────────────────────────────┤  │ └────────────────────┘  │
│  │  │                               │                             │
│  │  │  Text Editor or               │                             │
│  │  │  Blueprint Canvas             │                             │
│  │  │                               │                             │
│  │  └───────────────────────────────┘                             │
│  ├───────────────────────────────────────────────────────────────┤ │
│  │ Error List (DataGrid: Row/Pos/Fragment/Message)               │ │
│  ├───────────────────────────────────────────────────────────────┤ │
│  │ [▶ Run] ┌─────────────────────────────────────────────────┐  │ │
│  │         │ Tasks ▼ (flyout with In Progress + Completed)   │  │ │
│  │         └─────────────────────────────────────────────────┘  │ │
│  └───────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
```

**Window properties:**
- **Minimum size:** 500×600
- **Title bar:** "ISMA 22"
- **State persistence:** Position, size, and maximized state saved/restored via `IPreferencesProvider`

## Project Models

### IProjectViewModel

**File:** `ISMA.ViewModels/ViewModels/IProjectViewModel.cs`

```csharp
public interface IProjectViewModel
{
    string Name { get; set; }
    string? FilePath { get; set; }
    string EditorContent { get; set; }
    bool IsDirty { get; }
    void Cut();
    void Copy();
    void Paste();
    void SetContent(string content);
}
```

### LismaProjectViewModel

**File:** `ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs`

Text-based LISMA project. Wraps an AvaloniaEdit `TextEditor` instance:

```csharp
[ObservableObject]
public partial class LismaProjectViewModel : ObservableObject, IProjectViewModel
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string _fullText;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private ObservableCollection<SyntaxTokenDto> _highlightTokens = new();

    // Syncs editor content to ViewModel
    public void SetContent(string content) { FullText = content; IsDirty = true; }

    // Calls server to validate source code
    public async Task ValidateAsync() { ... }

    // Triggers server-side syntax highlighting
    public async Task UpdateSyntaxHighlighting(string source) { ... }
}
```

### BlueprintProjectViewModel

**File:** `ISMA.ViewModels/ViewModels/BlueprintProjectViewModel.cs`

Visual statechart project. Wraps a `BlueprintEditorViewModel` instance:

```csharp
[ObservableObject]
public partial class BlueprintProjectViewModel : ObservableObject, IProjectViewModel
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private BlueprintEditorViewModel _blueprintEditor;
    [ObservableProperty] private bool _isDirty;

    // Converts blueprint to LISMA text at compile time
    public string GetLismaText() => _blueprintEditor.Blueprint.ConvertToLisma();
}
```

## Services

### ProjectService

**File:** `ISMA.ViewModels/Services/ProjectService.cs`

Manages the list of projects:

```csharp
public class ProjectService : IProjectService
{
    public ObservableCollection<IProjectViewModel> Projects { get; }
    public IProjectViewModel? ActiveProject { get; set; }

    public IProjectViewModel CreateNewText(string name = "New project");
    public IProjectViewModel CreateNewBlueprint(string name = "New statechart");
    public void Close(IProjectViewModel project);
    public void CloseAll();
}
```

`EditorTabPaneView` observes this collection to create/delete tabs.

### ProjectFileService

**File:** `ISMA.App/Services/ProjectFileService.cs`

File operations using Avalonia `OpenFilePicker` / `SaveFilePicker`:

| Method | Description |
|--------|-------------|
| `OpenAsync()` | Opens file picker with filters: `*.iscm2`, `*.scisma`, `*.im` |
| `SaveAsync(project)` | Saves active project (or prompts "Save as" if unsaved) |
| `SaveAllAsync()` | Saves all open projects |
| `SaveAsAsync(project)` | Opens file picker to save with a chosen filename |

**File type mapping:**
| Extension | Type | Editor |
|-----------|------|--------|
| `*.iscm2` | LISMA Text | AvaloniaEdit |
| `*.scisma` | Blueprint/Statechart | Visual canvas |
| `*.im` | Legacy | AvaloniaEdit |

### SimulationServiceViewModel

**File:** `ISMA.ViewModels/ViewModels/SimulationServiceViewModel.cs`

Orchestrates the full simulation lifecycle:

```csharp
[ObservableObject]
public partial class SimulationServiceViewModel : ISimulationServiceViewModel
{
    [ObservableProperty] private ObservableCollection<InProgressSimulationViewModel> _inProgressSimulations = new();
    [ObservableProperty] private ObservableCollection<CompletedSimulationViewModel> _completedSimulations = new();

    [RelayCommand]
    private async Task SimulateAsync()
    {
        // 1. Snapshot parameters
        // 2. Compile model
        // 3. Report errors if any
        // 4. Run simulation
        // 5. Monitor progress (IAsyncEnumerable → update progress bars)
        // 6. Download result
        // 7. Create CompletedSimulationViewModel and add to collection
    }

    [RelayCommand]
    private async Task VerifyAsync() { ... }
}
```

### SimulationResultService

**File:** `ISMA.App/Services/SimulationResultService.cs`

Manages completed simulation results:

| Method | Description |
|--------|-------------|
| `ShowChart(CompletedSimulation)` | Opens variable selector dialog, then launches Grin |
| `ExportToFile(CompletedSimulation, filePath)` | Exports to CSV asynchronously on background thread |
| `ShowExportDialog(CompletedSimulation)` | Opens `SaveFilePicker` for CSV output path |

CSV export writes headers (`x, DE_*, AE_*, f*`) followed by one row per simulation time step.

### EditorPlatformService

**File:** `ISMA.App/Services/EditorPlatformService.cs`

Propagates cut/copy/paste events to the focused editor. Used by the menu bar and toolbar to route clipboard commands to the active editor:

```csharp
public class EditorPlatformService : IEditorPlatformService
{
    private readonly TextEditor? _activeEditor;

    public void Cut() => _activeEditor?.Cut();
    public void Copy() => _activeEditor?.Copy();
    public void Paste() => _activeEditor?.Paste();
}
```

### SyntaxHighlighterService

**File:** `ISMA.ViewModels/Services/SyntaxHighlighterService.cs`

Calls `_serverFacade.HighlightSource(source)` to get `SyntaxTokenDto[]`:

```csharp
public class SyntaxHighlighterService : ISyntaxHighlighter
{
    public async Task<SyntaxTokenDto[]> HighlightSource(string source)
        => await _serverFacade.HighlightSource(source);
}
```

### ServerDrivenHighlightingTransformer

**File:** `ISMA.App/Services/ServerDrivenHighlightingTransformer.cs`

`DocumentColorizingTransformer` subclass that applies server tokens to AvaloniaEdit rendering:

```csharp
public class ServerDrivenHighlightingTransformer : DocumentColorizingTransformer
{
    private readonly IReadOnlyList<SyntaxTokenDto> _tokens;

    protected override void ColorizeLine(DocumentLine line, IColorizingContext context)
    {
        // Find tokens overlapping this line, apply foreground brushes
    }
}
```

**Token-to-color mapping:**

| Token Kind | Color |
|------------|-------|
| `Keyword` | Orange |
| `Comment` | Gray |
| `Number` | Blue |
| `Text` | Green |
| Other | Black |

**Debouncing:** Uses a version counter (`_highlightVersion`) in `LismaProjectViewModel` to discard stale highlighting updates. A 100ms delay is used between text changes and highlighting requests.

**Fallback:** When no server tokens are available (offline mode), the embedded `LISMA.xshd` provides client-side regex-based highlighting via `LismaSyntaxHelper.GetFallbackHighlighting()`.

### TextEditorFactory

**File:** `ISMA.App/Services/TextEditorFactory.cs`

Creates and manages AvaloniaEdit `TextEditor` instances:

```csharp
public class TextEditorFactory
{
    public TextEditor CreateTextEditor(string initialText, Action<string>? onTextChanged = null)
    {
        var editor = new TextEditor
        {
            FontFamily = "Consolas",
            FontSize = 12,
            ShowLineNumbers = true,
            WordWrap = false
        };
        // Wire TextChanged event
        // Apply syntax highlighting if tokens available
        return editor;
    }
}
```

## Views

### IsmaMenuBarView

**File:** `Views/IsmaMenuBarView.axaml`

Menu structure:

| Menu | Items | Keyboard Shortcut |
|------|-------|-------------------|
| **File** | New Text, New Statechart, Open, Save, Save As, Save All, Close, Close All, Exit | Ctrl+N, Ctrl+B, Ctrl+O, Ctrl+S, Ctrl+Shift+S, Ctrl+W, Ctrl+Q |
| **Edit** | Cut, Copy, Paste | Ctrl+X, Ctrl+C, Ctrl+V |
| **Simulation** | Verify, Run, Store Settings, Load Settings | Ctrl+F4, Ctrl+F5 |

### IsmaToolBarView

**File:** `Views/IsmaToolBarView.axaml`

Toolbar buttons mirroring menu operations: New Text, New Blueprint, Open, Save, Save All, Cut, Copy, Paste, Verify, Store, Load.

### EditorTabPaneView

**File:** `Views/EditorTabPaneView.axaml`

`TabControl` bound to `Projects` collection:

```axaml
<TabControl ItemsSource="{Binding Projects}" SelectedItem="{Binding ActiveProject}">
    <TabControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text=" *" Foreground="Red" Visibility="{Binding IsDirty, Converter={StaticResource BoolToVisibilityConverter}}" />
            </StackPanel>
        </DataTemplate>
    </TabControl.ItemTemplate>
    <TabControl.ContentTemplate>
        <DataTemplate DataType="local:LismaProjectViewModel">
            <local:IsmaTextEditorView />
        </DataTemplate>
        <DataTemplate DataType="local:BlueprintProjectViewModel">
            <ContentControl />
        </DataTemplate>
    </TabControl.ContentTemplate>
</TabControl>
```

Tab titles show a red asterisk (`*`) when `IsDirty` is true.

### IsmaTextEditorView

**File:** `Views/IsmaTextEditorView.axaml`

AvaloniaEdit `TextEditor` with:
- Consolas 12pt font
- Line numbers on the left margin
- Context menu (Cut, Copy, Paste, Select All, Find/Replace)
- Syntax highlighting status indicator in the status bar
- Bidirectional binding to `LismaProjectViewModel.FullText`

### BlueprintEditorView

**File:** `Views/BlueprintEditorView.axaml`

Visual statechart editor with a `Canvas` containing three `ItemsControl` layers (rendered back-to-front):
1. **Transaction arrows** — inter-state transitions
2. **Loop arrows** — self-loop transitions
3. **States** — draggable state boxes

Bottom toolbar with mode toggle buttons: New State, New Transition, Remove State, Remove Transition.

### SettingsPanelView

**File:** `Views/SettingsPanelView.axaml`

Right sidebar with five `PropertiesGrid` controls:

| Grid | ViewModel | Purpose |
|------|-----------|---------|
| **Cauchy Initials** | `CauchyInitialsViewModel` | Start time, end time, initial step |
| **Integration** | `MethodSettingsViewModel` | Method, accuracy, stability, parallel |
| **Event Detection** | `EventDetectionViewModel` | Gamma, low border, step limit |
| **Result Saving** | `ResultSavingViewModel` | MEMORY / FILE target |
| **Result Processing** | `ResultProcessingViewModel` | Simplify method, tolerance |

### PropertiesGrid

**File:** `Controls/PropertiesGrid.axaml`

Reusable custom control that dynamically generates property input fields from a ViewModel using reflection. Each property is rendered as a label-control pair:

```axaml
<controls:PropertiesGrid Properties="{Binding CauchyInitials}" />
```

Supports:
- `double` → `NumericUpDown`
- `int` → `NumericUpDown`
- `bool` → `CheckBox`
- `string` → `TextBox`
- `enum` → `ComboBox`

### IsmaErrorListTableView

**File:** `Views/IsmaErrorListTableView.axaml`

`DataGrid` with columns:

| Column | Width | Content |
|--------|-------|---------|
| **Row** | 5% | Line number where the error occurred |
| **Pos** | 5% | Character position on the line |
| **Fragment** | 10% | Name of the code fragment/region |
| **Message** | 80% | Human-readable error description |

Populated by `ErrorListViewModel` on compile or verify operations.

### SimulationProcessBarView

**File:** `Views/SimulationProcessBarView.axaml`

Bottom bar with:
- **▶ Run button** — triggers `SimulationServiceViewModel.SimulateAsync()`
- **Tasks button** — opens a `Flyout` containing `TasksPopOverView`

### TasksPopOverView

**File:** `Views/TasksPopOverView.axaml`

Two-section floating panel:

**In Progress section:**
- One row per running simulation
- Progress bar showing normalized progress (0%–100%)
- Abort button — calls `InProgressSimulationViewModel.AbortCommand`

**Completed section:**
- One row per completed simulation
- Show button — opens chart viewer (SelectVariablesDialog → Grin)
- Export button — opens CSV export dialog
- Remove button — removes from the list
- Details chevron — opens a nested flyout with simulation metadata

### SelectVariablesDialogWindow

**File:** `Views/SelectVariablesDialogWindow.axaml`

Dialog for selecting chart axes:

| Element | Type | Description |
|---------|------|-------------|
| **X Axis** | `ComboBox` | Dropdown listing all available columns. Default: "TIME" pre-selected. |
| **Y Axis** | `ListBox` with `CheckBox` | Scrollable list with checkboxes. Multiple selection. |
| **Select All** | `Button` | Checks all Y-axis checkboxes |
| **Unselect All** | `Button` | Unchecks all Y-axis checkboxes |
| **OK** | `Button` | Confirms selection and proceeds to chart visualization |
| **Close** | `Button` | Cancels the dialog |

## Styles

### GlobalStyles

**File:** `Styles/GlobalStyles.axaml`

Defines the application-wide theme:

| Resource | Value | Usage |
|----------|-------|-------|
| **Primary color** | `#0078D4` | Accent color for buttons, selections |
| **Error color** | `Red` | Error list, validation failures |
| **Success color** | `Green` | Success indicators |
| **Warning color** | `Orange` | Warning indicators |
| **Primary font** | `Inter` | Body text, UI elements |
| **Mono font** | `Consolas` | Code editor |
| **Normal size** | `12` | Default font size |
| **Small size** | `11` | Secondary text |
| **Large size** | `14` | Headers |

Custom styles for:
- `Button` / `ToggleButton` — hover and pressed states
- `MenuItem` — hover background
- `TextBox` / `DataGrid` — focus border with accent color
- `TabControl` — tab item styling
- `BoxShadow` — utility style for elevation effects

## ViewModels

### MainWindowViewModel

**File:** `ISMA.ViewModels/ViewModels/MainWindowViewModel.cs`

Central command hub. All application commands are defined here:

| Command | Shortcut | Description |
|---------|----------|-------------|
| `NewText` | Ctrl+N | Creates new LISMA text project |
| `NewBlueprint` | Ctrl+B | Creates new blueprint project |
| `Open` | Ctrl+O | Opens file picker, creates project tab |
| `Save` | Ctrl+S | Saves active project |
| `SaveAs` | — | Saves active project with chosen filename |
| `SaveAll` | Ctrl+Shift+S | Saves all open projects |
| `Close` | — | Closes active project |
| `CloseAll` | — | Closes all projects |
| `Exit` | Ctrl+W / Ctrl+Q | Exits application |
| `Cut` | Ctrl+X | Cuts from active editor |
| `Copy` | Ctrl+C | Copies from active editor |
| `Paste` | Ctrl+V | Pastes into active editor |
| `SelectAll` | — | Selects all in active editor |
| `Verify` | Ctrl+F4 | Validates active project |
| `Run` | Ctrl+F5 | Runs simulation |
| `StoreSettings` | — | Saves parameters to JSON file |
| `LoadSettings` | — | Loads parameters from JSON file |

### InProgressSimulationViewModel

**File:** `ISMA.ViewModels/ViewModels/InProgressSimulationViewModel.cs`

Tracks a running simulation:

```csharp
[ObservableObject]
public partial class InProgressSimulationViewModel
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isAborted;

    [RelayCommand]
    private async Task AbortAsync() { ... }
}
```

### CompletedSimulationViewModel

**File:** `ISMA.ViewModels/ViewModels/CompletedSimulationViewModel.cs`

Wraps a completed simulation with UI commands:

```csharp
[ObservableObject]
public partial class CompletedSimulationViewModel
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name;
    [ObservableProperty] private CompletedSimulation _simulation;

    [RelayCommand]
    private void ShowChart() { _resultService.ShowChart(_simulation); }

    [RelayCommand]
    private void Export() { _resultService.ShowExportDialog(_simulation); }

    [RelayCommand]
    private void Remove() { ... }
}
```

## Error Handling

| Scenario | Behavior |
| --- | --- |
| Compilation errors | `CompilationErrorDto[]` → `ErrorInfo[]` → `ErrorListViewModel.Errors` → `DataGrid` |
| Server process not found | `InvalidOperationException` — server script path not configured |
| Missing config | `InvalidOperationException` — server or Grin script path not found in appsettings.json |
| Simulation failure | Exception re-thrown from `SimulationServiceViewModel`, displayed in error list |
