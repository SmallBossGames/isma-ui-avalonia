# UX Reference — ISMA UI Avalonia

## Purpose

This document describes the complete user experience of the ISMA desktop application from a user-interaction perspective. It covers every window, menu, toolbar, dialog, panel, transition, and feature. Use this as a specification when implementing a replacement UI — the goal is feature parity, not framework fidelity.

## Application Shell

### Main Window Layout

```
┌────────────────────────────────────────────────────────────────────┐
│ Menu Bar: [File] [Edit] [Simulation]                              │
├────────────────────────────────────────────────────────────────────┤
│ Toolbar: [New] [Blueprint] [Open] [Save] [SaveAll] │ [Cut] [Copy] │
│          [Paste] │ [Verify] │ [Store] [Load]                      │
├────────────────────────────────────────────────────────────────────┤
│                                          ┌────────────────────┐  │
│                                          │ Settings Panel     │  │
│                                          │ [Initials]         │  │
│                                          │ [Integration]      │  │
│                                          │ [Event detection]  │  │
│                                          │ [Result proc.]     │  │
│                                          │ [Saving]           │  │
│                                          └────────────────────┘  │
│  ┌─────────────────────────────────────┐                         │
│  │                                     │                         │
│  │  EditorTabPaneView (TabControl)     │                         │
│  │  ┌───────────────────────────────┐  │                         │
│  │  │ [New project | Tab 2 | ...]   │  │                         │
│  │  ├───────────────────────────────┤  │                         │
│  │  │                               │                         │
│  │  │  Text Editor or               │                         │
│  │  │  Blueprint Canvas             │                         │
│  │  │                               │                         │
│  │  └───────────────────────────────┘                         │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │ Error List (DataGrid: Row/Pos/Fragment/Message)             │ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │ [▶ Run] ┌─────────────────────────────────────────────────┐ │ │
│  │         │ Tasks ▼ (flyout with In Progress + Completed)   │ │ │
│  │         └─────────────────────────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
```

**Window properties:**
- **Minimum size:** 500×600
- **Title bar:** "ISMA 22"
- **State persistence:** Position, size, and maximized state are saved between sessions via `IPreferencesProvider`

---

## Menu Bar

### File Menu

| Item | Keyboard Shortcut | Action |
|------|-------------------|--------|
| **New Text** | `Ctrl+N` / `Cmd+N` | Creates a new text-based LISMA project tab named "New project" |
| **New Statechart** | `Ctrl+B` / `Cmd+B` | Creates a new blueprint (visual statechart) project tab named "New statechart" |
| **Open…** | `Ctrl+O` / `Cmd+O` | Opens a file picker dialog. Filters: `*.im2` (text), `*.iscm2` (statechart) |
| **Save** | `Ctrl+S` / `Cmd+S` | Saves the active project tab. If unsaved, prompts "Save as". |
| **Save As…** | `Ctrl+Shift+S` | Opens file picker to save the active project with a chosen filename and type |
| **Save All** | — | Saves all open project tabs |
| **Close** | — | Closes the active project tab |
| **Close All** | — | Closes all open project tabs |
| **Exit** | `Ctrl+W` / `Ctrl+Q` | Terminates the application |

**File format behavior:**
- `*.im2` — Text-based LISMA source code (plain text file)
- `*.iscm2` — Blueprint statechart (JSON-encoded visual statechart model)
- `*.im` — Legacy ISMA project (not supported; opening reports an error)

### Edit Menu

| Item | Keyboard Shortcut | Action |
|------|-------------------|--------|
| **Cut** | `Ctrl+X` / `Cmd+X` | Cut selected text from the focused editor |
| **Copy** | `Ctrl+C` / `Cmd+C` | Copy selected text to clipboard |
| **Paste** | `Ctrl+V` | Paste clipboard content into the focused editor |

**Behavior:** These commands are routed to the currently focused editor (text or blueprint). The text editor supports cut/copy/paste via the platform clipboard. The blueprint editor does not have text editing in its canvas area, so these actions apply to any focused text field within the blueprint editor (e.g., arrow predicate/alias fields).

### Simulation Menu

| Item | Keyboard Shortcut | Action |
|------|-------------------|--------|
| **Verify** | `Ctrl+F4` | Validates the active project's source code against the server's PDE translator. Errors appear in the Error List. |
| **Run** | `Ctrl+F5` | Runs the simulation with current parameters (see Simulation Flow section). |
| **Store Settings…** | — | Opens file picker to save current simulation parameters as JSON (`*.params.json`) |
| **Load Settings…** | — | Opens file picker to load simulation parameters from a JSON file (`*.params.json`) |

---

## Toolbars

### Main Toolbar

Icons use Material Design glyphs. From left to right:

| # | Icon Glyph | Tooltip | Action |
|---|-----------|---------|--------|
| 1 | `add_circle_outline` | "New model" | Same as File → New Text |
| 2 | `add_box` | "New statechart" | Same as File → New Statechart |
| 3 | `folder_open` | "Open model" | Same as File → Open |
| 4 | `save` | "Save current model" | Same as File → Save |
| 5 | `save_all` | "Save all models" | Same as File → Save All |
| — | *(separator)* | — | — |
| 6 | `content_cut` | "Cut" | Same as Edit → Cut |
| 7 | `content_copy` | "Copy" | Same as Edit → Copy |
| 8 | `content_paste` | "Paste" | Same as Edit → Paste |
| — | *(separator)* | — | — |
| 9 | `check_circle` | "Verify" | Same as Simulation → Verify |
| — | *(separator)* | — | — |
| 10 | `bookmark` | "Store Settings" | Same as Simulation → Store Settings |
| 11 | `bookmark_border` | "Load Settings" | Same as Simulation → Load Settings |

### Simulation Process Bar (Bottom Bar)

Located at the bottom of the editor area, left side:

| Element | Icon/Text | Tooltip | Action |
|---------|-----------|---------|--------|
| Play button | `▶ Run` | "Run simulation" | Runs simulation (same as Simulation → Run) |
| Tasks button | "Tasks" | — | Opens the Tasks PopOver (see below) |

---

## Editor Area

### Tab-based Project Management

The central area is a `TabControl`. Each tab represents one open project.

**Tab behavior:**
- **New tab:** Created when user opens/creates a project via File menu or toolbar
- **Tab title:** Shows project name (derived from filename if saved, otherwise "New project" or "New statechart")
- **Dirty indicator:** A red asterisk (`*`) appears after the name when the project has unsaved changes
- **Tab close:** Clicking the close button (X) on a tab closes that project (same as File → Close)
- **Tab selection:** Selecting a tab makes that project the "active" project for all toolbar/menu actions
- **Multiple tabs:** Multiple projects can be open simultaneously

### Project Types

#### Text Editor Tab (LISMA source code)

A rich text editor with:
- **Font:** Consolas 12pt
- **Line numbers:** Displayed on the left margin
- **Syntax highlighting:** Server-driven — keywords (orange, bold), comments (gray, italic), numbers (blue)
- **Text editing:** Full insert/delete/cut/copy/paste via keyboard and menu
- **Context menu:** Right-click shows Cut, Copy, Paste, Select All, Find/Replace
- **Content:** LISMA mathematical modeling language source code

**How it works:** The text editor component is embedded within each text project tab. Each tab has its own text content model backed by `LismaProjectViewModel.FullText`.

#### Blueprint Editor Tab (Visual statechart)

A drag-and-drop visual canvas for building state machines:

**Canvas layout:**
- Scrollable canvas with states, transitions, and loops
- Two pre-created states: **Main** (green, top-left, non-editable) and **init** (blue, below Main, non-editable, no rename)
- User-created states: Coral-colored rounded rectangles, draggable, editable names
- Transitions: Arrows connecting states with optional predicate and alias labels
- Loop transitions: Circular arrows self-connecting a state to itself

**Toolbar (at the bottom of the blueprint editor):**

| Button | Action |
|--------|--------|
| **New state** | Creates a new state at default position (10, 200) with auto-generated name |
| **New transition** | Enter "Add transition" mode — click source state, then click target state |
| **Remove state** | Enter "Remove state" mode — click a state to delete it (and all its arrows) |
| **Remove transition** | Enter "Remove transition" mode — click an arrow to delete it |

**Interaction modes (mutually exclusive):**

1. **Default (drag) mode:** Drag states to reposition them on the canvas
2. **Add transition mode:** Click a source state, then click a target state. If the same state is clicked twice, a loop (self-transition) is created.
3. **Remove state mode:** Click any state to delete it along with all associated arrows
4. **Remove transition mode:** Click any transition arrow to delete it

**State box details:**
- **Shape:** Rounded rectangle (corner radius 20px), width 110px, height 65px (60px for Main/Init)
- **Colors:** Main state = LightGreen, init state = LightBlue, user states = Coral (default fill)
- **Name editing:** Single-click a state to enter inline name-edit mode. Names must be unique across all states.
- **Double-click:** Opens a text editor tab for editing the state's content text
  - For regular states: tab named after the state (e.g., "MyState")
  - For loop transitions: tab named "{stateName} (loop)"

**Transition arrow details:**
- **Regular transition:** Line with arrowhead from source state to target state
- **Loop transition:** Circle (radius 40px) attached to the state, arrowhead returns to the state
- **Label:** Shows the predicate condition and/or alias name
- **Single-click arrow body:** Opens Edit Arrow PopOver (see below)
- **Double-click arrowhead (loop):** Opens a text editor tab for editing loop content

**Edit Arrow PopOver:**
- Floating card with DropShadow, positioned near the clicked arrow
- Contains two labeled text fields:
  - **"Alias (optional)"** — optional label for the transition
  - **"Predicate"** — the condition/trigger for the transition
- Changes are reflected on the arrow in real-time (two-way binding)
- Auto-closes when mouse exits the popover

**Blueprint-to-LISMA conversion:**
When a blueprint project is compiled/simulated, the visual statechart is automatically converted to LISMA text format:
- Main state → `state "Main" { ... }`
- Init state → `state "init" { ... }`
- Transitions → `state "predicate" { ... } from startState;`
- Loop transitions → pseudo-state pattern with `(predicate)` syntax

---

## Right Sidebar: Settings Panel

**Location:** Right side of the main window, occupies the full height of the content area (below the toolbars).

**Visibility:** Always visible as part of the main window layout. All sections are expanded and visible simultaneously.

**Implementation:** Five `PropertiesGrid` controls, each dynamically generating property input fields from a ViewModel using reflection. The panel uses a label-control pair layout.

**Data flow:** The panel reads from and writes to the `SimulationParametersViewModel` singleton, which holds all parameter sub-ViewModels. When the user clicks "Run", the service's `Snapshot()` method captures the current view model state into a serializable `SimulationParameters` model, which is then sent to the server via gRPC. The panel values are **not** live-bound to the simulation — they are only read at the moment of execution.

---

### 1. Initials (Cauchy Initials)

**Purpose:** Defines the time domain for the numerical integration. These parameters specify the interval over which the differential equations are solved.

**ViewModel:** `CauchyInitialsViewModel`

| Label | Control | Type | Default | Property |
|-------|---------|------|---------|----------|
| **Start** | NumericUpDown | Double | 0.0 | `StartTime` |
| **End** | NumericUpDown | Double | 10.0 | `EndTime` |
| **Step** | NumericUpDown | Double | 0.1 | `InitialStep` |

**When values are consumed:** Only when "Run" is clicked. The values are captured via `Snapshot()` → `SimulationParameters` → sent to server via gRPC.

**Parameter details:**
- **Start (t₀):** The initial time value. The integration begins from this point. Must be less than or equal to End.
- **End (t₁):** The final time value. The integration runs until this time is reached. Must be greater than or equal to Start.
- **Step (h₀):** The initial integration step size. This is a *hint* to the integration method — adaptive methods (like RK Fehlberg) will adjust step size dynamically based on error estimates, while fixed-step methods use this value directly.

---

### 2. Integration

**Purpose:** Configures the numerical integration algorithm and its control parameters. This section determines *how* the differential equations are solved.

**ViewModel:** `MethodSettingsViewModel`

| Label | Control | Type | Default | Disabled when | Bound to |
|-------|---------|------|---------|---------------|----------|
| **Method** | ComboBox | String (from server) | First method in list | — | `MethodName` |
| **Accurate** | CheckBox | Boolean | false | — | `IsAccuracyInUse` |
| **Accuracy** | NumericUpDown | Double | 0.1 | "Accurate" unchecked | `Accuracy` |
| **Stable** | CheckBox | Boolean | false | — | `IsStabilityControlInUse` |
| **Parallel** | CheckBox | Boolean | false | — | `IsParallelInUse` |
| **Server** | TextBox | String | "localhost" | "Parallel" unchecked | `Server` |
| **Port** | NumericUpDown | Integer | 7890 | "Parallel" unchecked | `Port` |

**Method list population:** The ComboBox items come from the server's available integration methods at startup (e.g., Euler, RK2, RK3, RK Fehlberg).

**Conditional behavior:**
- **Accuracy field:** Disabled when "Accurate" is unchecked
- **Server / Port fields:** Disabled when "Parallel" is unchecked

**When values are consumed:** At simulation start, via `Snapshot()` → `SimulationParameters` → sent to server via gRPC.

**Parameter details:**
- **Method:** The numerical integration algorithm. Different methods offer different trade-offs between speed and accuracy.
- **Accurate (IsAccuracyInUse):** Enables adaptive step-size control. When checked, the integration method adjusts its step size dynamically to keep the error below the Accuracy threshold.
- **Accuracy:** The tolerance for adaptive integration. Smaller values (e.g., 0.001) produce more accurate but slower results.
- **Stable (IsStabilityControlInUse):** Enables stability control during integration. Prevents numerical instability (oscillations or divergence).
- **Parallel (IsParallelInUse):** When checked, the simulation runs on a remote server cluster instead of the local server.
- **Server / Port:** Network address for the parallel execution target. Used only when Parallel is enabled.

---

### 3. Event Detection

**Purpose:** Configures zero-crossing detection during integration. Event detection allows the simulator to pinpoint exact times when state variables or user-defined functions cross zero.

**ViewModel:** `EventDetectionViewModel`

| Label | Control | Type | Default | Disabled when | Bound to |
|-------|---------|------|---------|---------------|----------|
| **In use** | CheckBox | Boolean | false | — | `IsEventDetectionInUse` |
| **Gamma** | NumericUpDown | Double | 0.8 | "In use" unchecked | `Gamma` |
| **Step limit** | CheckBox | Boolean | false | — | `IsStepLimitInUse` |
| **Low border** | NumericUpDown | Double | 0.001 | "Step limit" unchecked | `LowBorder` |

**Conditional behavior:**
- **Gamma field:** Disabled when "In use" is unchecked
- **Low border field:** Disabled when "Step limit" is unchecked

**When values are consumed:** At simulation start. `Gamma` and `LowBorder` are sent to the server only when event detection is enabled.

**Parameter details:**
- **In use (IsEventDetectionInUse):** Enables zero-crossing event detection. Without this, the integrator only records values at discrete time steps.
- **Gamma (γ):** Event detection sensitivity. A value between 0 and 1 that controls how close a variable must be to zero for an event to be detected.
- **Step limit (IsStepLimitInUse):** Constrains the integrator's step size during event detection searches.
- **Low border:** The minimum step size allowed during event detection. Prevents excessively small steps.

---

### 4. Result Saving

**Purpose:** Determines where and how simulation results are stored after completion.

**ViewModel:** `ResultSavingViewModel`

| Label | Control | Type | Default | Bound to |
|-------|---------|------|---------|----------|
| **Save result** | ComboBox | Enum: `MEMORY` / `FILE` | `MEMORY` | `SavingTarget` |

**Parameter details:**
- **MEMORY:** Results are kept in memory only. Available for immediate chart display, but not persisted to disk.
- **FILE:** Results are written to a binary cache file on disk (`.bin` format). Available for chart display, CSV export, and later sessions.

---

### 5. Result Processing

**Purpose:** Configures post-processing simplification of simulation results. This applies a line-simplification algorithm to reduce the number of data points.

**ViewModel:** `ResultProcessingViewModel`

| Label | Control | Type | Default | Bound to |
|-------|---------|------|---------|----------|
| **Simplify** | CheckBox | Boolean | false | `IsSimplifyInUse` |
| **Method** | ComboBox | String: "Radial-Distance" / "Douglas-Peucker" | First in list | `SelectedSimplifyMethod` |
| **Tolerance** | NumericUpDown | Double | 20.0 | `Tolerance` |

**When values are consumed:** Result processing parameters are **not** sent to the server. They are application-local settings that affect only the client-side chart rendering and CSV export.

**Parameter details:**
- **Simplify (IsSimplifyInUse):** Enables line-simplification post-processing.
- **Method:** The line-simplification algorithm:
  - **Radial-Distance:** Faster but may distort sharp features
  - **Douglas-Peucker:** Better for curves with sharp turns or peaks
- **Tolerance:** The maximum allowed deviation between the original and simplified curve.

---

### Data Flow Summary

```
User edits settings → ViewModel properties (CommunityToolkit.Mvvm [ObservableProperty])
        ↓
SimulationParametersViewModel holds all 5 sub-ViewModels as a singleton
        ↓
On "Run" click → Snapshot() captures all ViewModels → SimulationParameters
        ↓
Sent to SimulationServerFacade.RunSimulation() → server via gRPC
```

**Parameters sent to server:** Cauchy initials (start, end, step), integration method (name, accuracy, flags), event detection (gamma, low border — only when enabled).

**Parameters NOT sent to server:** `IsStabilityControlInUse` (client-side metadata), `IsParallelInUse` / `Server` / `Port` (client-side connection config), `SavingTarget` (client-side storage preference), result processing settings (client-side rendering only).

---

## Bottom Area: Error List & Process Bar

### Error List Drawer

A DataGrid panel between the editor area and the process bar.

**Columns:**

| Column | Width | Content |
|--------|-------|---------|
| **Row** | 5% | Line number where the error occurred |
| **Pos** | 5% | Character position on the line |
| **Fragment** | 10% | Name of the code fragment/region |
| **Message** | 80% | Human-readable error description |

**Populated by:**
- **Compile errors:** When "Run" is clicked, compilation errors from the server appear here
- **Verify results:** When "Verify" is clicked, validation errors appear here
- **Behavior:** Each new run/verify clears the previous error list and shows fresh errors

### Simulation Process Bar

Located at the very bottom of the window:

**Elements:**
- **▶ Run button:** Starts the simulation
- **Tasks button:** Opens the Tasks PopOver

---

## Tasks PopOver

A floating panel that opens from the "Tasks" button. Shows running and completed simulations.

### Layout

```
┌─────────────────────────────────────────┐
│ In Progress                             │
│ ┌─────────────────────────────────────┐ │
│ │ Task #1  [████████░░] [Abort]      │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ Task #2  [██████░░░░] [Abort]      │ │
│ └─────────────────────────────────────┘ │
├─────────────────────────────────────────┤
│ Completed                               │
│ ┌─────────────────────────────────────┐ │
│ │ Task #1  [Show] [Export] [Remove]   │ │
│ │            [⋯ Details]             │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ Task #2  [Show] [Export] [Remove]   │ │
│ │            [⋯ Details]             │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

### In Progress Section

One row per running simulation. Each row contains:
- **Task label:** "Task #N" (auto-incrementing counter starting from 1)
- **Progress bar:** Shows normalized progress from 0% to 100%
- **Abort button:** Cancels the running simulation on the server and removes the task

**Behavior:** New tasks appear when "Run" is clicked. Rows are removed when the simulation completes or is aborted.

### Completed Section

One row per completed simulation. Each row contains:
- **Task label:** "Task #N"
- **Show button:** Opens the chart viewer (see Results Visualization)
- **Export button:** Opens file picker to export results as CSV
- **Remove button:** Removes this entry from the list (does not delete the cached file)
- **Details button:** Chevron icon (⋯) — opens a nested flyout with simulation metadata

### Details Flyout (nested)

Triggered by clicking the "Details" chevron button. Shows:

```
Model
Name: Project name

Cauchy Initials
Start: 0.0
End: 10.0
Initial step: 0.1

Integration Method
Method: Euler
Is accurate: true
Accuracy: 0.1
Is stable: true

Statistic
Simulation time: 1234ms
```

**Fields displayed:**
- Model name
- Cauchy initials: Start, End, Initial step
- Integration method: Method name, Accuracy status, Accuracy value (if enabled), Stability status
- Statistics: Wall-clock simulation time in milliseconds

---

## Dialogs

### Select Variables Dialog (Axis Picker)

Opens when the user clicks "Show" on a completed simulation. Used to select which axes/variables to plot.

**Title:** "Select variables"

**Layout:**

```
┌─────────────────────────────────────────┐
│ X Axis:   [━━━━━━━━━━━━━━━━━━━━━━━━━▼] │
│                                           │
│ Y Axis:   [☐ Variable 1]                │
│            [☐ Variable 2]                │
│            [☐ Variable 3]                │
│            [☐ TIME] (pre-selected)       │
│            [☐ Variable N]                │
│                                           │
│                    [Select all] [Unselect │
│                         all]              │
│                                           │
│              [Ok]          [Close]        │
└─────────────────────────────────────────┘
```

**Elements:**
- **X Axis ComboBox:** Dropdown listing all available columns. One item selectable. Default: "TIME" column pre-selected.
- **Y Axis ListBox:** Scrollable list with checkboxes next to each column name. Multiple items selectable.
- **Select all:** Checks all Y-axis checkboxes
- **Unselect all:** Unchecks all Y-axis checkboxes
- **Ok:** Confirms selection and proceeds to chart visualization
- **Close:** Cancels the dialog, no chart is generated

**Behavior:**
- Only non-null items from the Y-axis list are displayed as selectable options
- TIME column is always pre-selected as the X-axis
- On confirmation, the chart viewer launches with the selected X and Y variables

---

## Simulation Flow

### Complete User Journey: Run a Simulation

```mermaid
sequenceDiagram
    participant User
    participant UI as Main Window
    participant SimSvc as Simulation<br/>ServiceViewModel
    participant Server as ISMA Server
    participant Errors as Error List
    participant Tasks as Tasks PopOver
    participant Result as Results<br/>Service
    participant Grin as Grin<br/>Chart Viewer

    User->>UI: Configure settings (right panel)
    User->>UI: Edit source (editor tab)
    User->>UI: Click ▶ Run (or Ctrl+F5)
    UI->>SimSvc: SimulateAsync()

    SimSvc->>SimSvc: Snapshot parameters
    SimSvc->>SimSvc: Get active project source

    SimSvc->>Server: CompileModel(source)
    Server-->>SimSvc: CompileResult

    alt Compilation errors
        SimSvc->>Errors: Display errors in DataGrid
        SimSvc-->>UI: Stop (no simulation)
    else Compilation succeeds
        SimSvc->>Server: RunSimulation(params)
        Server-->>SimSvc: simulationId

        SimSvc->>Tasks: Add "In progress" row
        SimSvc->>Server: MonitorSimulation(id)

        loop Progress updates
            Server-->>SimSvc: SimulationProgress (yield)
            SimSvc->>Tasks: Update progress bar
        end

        SimSvc->>Server: DownloadResult(id)
        Server-->>SimSvc: DownloadResult(file, columnNames)

        SimSvc->>Tasks: Move to "Completed"

        SimSvc->>Tasks: Remove "In progress" row
    end

    User->>Tasks: Click "Show" on completed task
    Tasks->>Result: ShowChart(result)
    Result->>UI: Open axis picker dialog
    User->>UI: Select X and Y variables
    UI-->>Result: Selected axes
    Result->>Grin: Launch with data + axis params
    Grin-->>User: Display chart window
```

### Verify Flow

```mermaid
sequenceDiagram
    participant User
    participant UI as Main Window
    participant PdeSvc as LismaPdeService
    participant Server as ISMA Server
    participant Errors as Error List

    User->>UI: Click Verify (toolbar or Ctrl+F4)
    UI->>PdeSvc: ValidateAsync(source)
    PdeSvc->>Server: ValidateModel(source)
    Server-->>PdeSvc: ValidateResult

    alt Validation errors
        PdeSvc->>Errors: Display errors in DataGrid
    else No errors
        PdeSvc->>Errors: Clear error list
    end
```

---

## File Operations

### Open Project

**Trigger:** File → Open, or toolbar `folder_open` button, or `Ctrl+O`

**Dialog behavior:**
- File picker with a single filter: `All ISMA project files` (`.im2`, `.iscm2`)
- Single selection

**After open:**
- Each file creates a new tab in the editor area
- Text files (`.im2`) → LISMA text editor tab
- Statechart files (`.iscm2`) → Blueprint editor tab
- Tab name is derived from the filename (without extension)

### Save Project

**Trigger:** File → Save, or toolbar `save` button, or `Ctrl+S`

**Behavior:**
- If the project was opened from a file: overwrites the same file
- If the project is new (never saved): opens "Save as" dialog
- After save: tab title updates (red asterisk disappears)

### Save Project As

**Trigger:** File → Save As, or Save on an unsaved project

**Dialog behavior:**
- File picker with type filter determined by project type
- On save: project name updates to filename, file path is stored

### Save All

**Trigger:** File → Save All, or toolbar `save_all` button

**Behavior:** Iterates through all open projects and saves each one (same logic as individual save).

### Close Project

**Trigger:** File → Close, or tab close button

**Behavior:**
- Closes the current tab
- Disposes the project's resources (editor instances)
- If no tabs remain, the editor area is empty

### Close All

**Trigger:** File → Close All

**Behavior:** Closes all open tabs at once.

---

## Settings Persistence

### Store Settings

**Trigger:** File → Store Settings, Simulation → Store Settings, or toolbar `bookmark` button

**Dialog behavior:**
- File picker to save simulation parameters
- Default filter: "Simulation Parameters File" (`.params.json`)
- **Serialized data:** Cauchy initials, integration method params, event detection params, result saving params
- **Not serialized:** Result processing params (simplify settings)

### Load Settings

**Trigger:** File → Load Settings, Simulation → Load Settings, or toolbar `bookmark_border` button

**Dialog behavior:**
- File picker to load simulation parameters
- Default filter: "Simulation Parameters File" (`.params.json`)
- **Deserialized data:** Populates all settings panels with loaded values
- **Not loaded:** Result processing params (unchanged)

### Window Preferences

**Behavior:**
- **On startup:** Main window geometry (x, y, width, height, maximized) is restored from saved preferences
- **On exit:** Main window geometry is saved to a JSON preferences file
- **Last opened files:** Paths of recently opened projects are persisted and reloaded on startup

---

## Results Visualization

### Chart Display

**Trigger:** Tasks PopOver → Completed → "Show" button on a completed simulation

**Steps:**
1. Axis picker dialog opens (see Select Variables Dialog above)
2. User selects X-axis variable and one or more Y-axis variables
3. Grin chart viewer process is launched as a separate child process
4. Grin receives: result binary file path, X-axis column name, Y-axis column names
5. Grin displays an interactive chart window

**Data format:** Binary `.bin` file containing `SimulationPoint` records (x, yForDE[], rhs[][]). Column names come from server metadata.

### CSV Export

**Trigger:** Tasks PopOver → Completed → "Export" button on a completed simulation

**Steps:**
1. File picker opens (default filter: `*.csv`)
2. On confirmation, a background task streams the binary data and writes CSV
3. **CSV header:** `x, [DE column names], [AE column names], f0, f1, ..., fN`
   - `x` = independent variable (time)
   - DE columns = differential equation variable names from metadata
   - AE columns = algebraic equation variable names from metadata
   - `fN` = RHS values for differential equations
4. Each subsequent row = one simulation time step

**Export behavior:**
- Runs on a background task (non-blocking)
- Progress is not shown during export

---

## Keyboard Shortcuts Reference

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` / `Cmd+N` | New text project |
| `Ctrl+B` / `Cmd+B` | New statechart project |
| `Ctrl+O` / `Cmd+O` | Open project |
| `Ctrl+S` / `Cmd+S` | Save project |
| `Ctrl+Shift+S` | Save all projects |
| `Ctrl+W` / `Ctrl+Q` | Exit application |
| `Ctrl+X` / `Cmd+X` | Cut |
| `Ctrl+C` / `Cmd+C` | Copy |
| `Ctrl+V` | Paste |
| `Ctrl+F4` | Verify model |
| `Ctrl+F5` | Run simulation |

---

## Feature Matrix

| Feature | Location | Description |
|---------|----------|-------------|
| **Multi-project editing** | Editor tab pane | Open, create, switch, and close multiple projects simultaneously |
| **Text-based LISMA editing** | Text editor tab | Rich text editor with syntax highlighting and line numbers |
| **Visual statechart editing** | Blueprint editor tab | Drag-and-drop canvas with states, transitions, and loop transactions |
| **Remote syntax highlighting** | Text editor | Server-driven tokenization — keywords, comments, numbers colored |
| **Model compilation** | Server (via gRPC) | Compiles LISMA text or blueprint to a runnable model |
| **Model validation** | Server (via gRPC) | Validates source code without running (Verify button) |
| **Simulation execution** | Server (via gRPC) | Runs the compiled model with given parameters |
| **Real-time progress** | gRPC stream | Server pushes progress updates during simulation |
| **Simulation cancellation** | gRPC call | Stops a running simulation mid-execution |
| **Error list** | Bottom drawer | Tabular display of compilation/validation errors |
| **Simulation parameters** | Right sidebar | Configurable: time range, integration method, event detection, result storage |
| **Parameter presets** | Store/Load settings | Save and load parameter sets as JSON files |
| **Chart visualization** | Grin process | External chart viewer launched with simulation data |
| **Variable axis selection** | Select Variables dialog | Interactive picker for X and Y axes |
| **CSV export** | File export | Export simulation results to CSV format |
| **Blueprint-to-LISMA** | Automatic conversion | Visual statecharts are converted to LISMA text at compile time |
| **State content editing** | Blueprint editor | Double-click states/loops to open inline text editors |
| **Transition editing** | Blueprint editor | Edit predicate and alias via floating PopOver |
| **Window state persistence** | Preferences | Save/restore window geometry and last opened files |
| **Parallel execution** | Integration settings | Option to run simulation on a remote server cluster |
| **Result simplification** | Result processing | Line-simplification (Radial-Distance, Douglas-Peucker) for smoother charts |
| **Clipboard propagation** | Text editor | Cut/copy/paste events propagated to the focused editor |

---

## Architecture Notes for Re-implementation

When building a replacement UI with the same features:

1. **Separation of concerns:** The UI is cleanly layered — domain models (pure data) → infrastructure (server communication) → ViewModels (business logic) → views (UI components)
2. **Server communication:** All compilation, validation, and simulation happen on a separate server process. The UI communicates via gRPC (compile, validate, run, monitor, cancel, download, highlight) and HTTP (binary result download).
3. **Multi-project:** Projects are managed in an `ObservableCollection` with an active project concept. Each project has its own ViewModel.
4. **Observable collections:** UI state is driven by `ObservableCollection<T>` that updates the UI reactively via Avalonia bindings.
5. **Blueprint-to-text:** The blueprint editor is a visual layer that serializes to/from a LISMA text representation. The conversion happens at compile time via `BlueprintToLismaConverter`, not in real-time.
6. **External processes:** Two external processes are launched by the UI:
   - ISMA Server (gRPC backend) — launched automatically on first use via `SimulationServerManager`
   - Grin Chart Viewer — launched on-demand when user clicks "Show" on results via `GrinProcessLauncher`
7. **Preferences:** Window geometry and last-opened files are persisted as JSON via `PreferencesProvider`. Simulation parameters are stored/loaded as separate JSON files by the user via `ISimulationParametersStoreService`.
8. **Syntax highlighting:** Computed server-side via `HighlightSource()` gRPC call. The UI receives token positions and kinds, then applies colors via `DocumentColorizingTransformer` in AvaloniaEdit.
9. **Result format:** Binary files containing `SimulationPoint` records (x, yForDE[], rhs[][]). Column names come from server metadata. Both the chart viewer and CSV export consume this binary format via `BinaryFilePointProvider`.
10. **Clipboard propagation:** Cut/copy/paste events are propagated via `EditorPlatformService` to the focused AvaloniaEdit `TextEditor`.
11. **Result simplification:** Post-processing simplification uses Radial-Distance or Douglas-Peucker algorithms to reduce data points for smoother chart rendering.
12. **MVVM pattern:** All UI state is managed in ViewModels using CommunityToolkit.Mvvm source generation. Views bind to ViewModel properties and commands via AXAML `Binding`.
