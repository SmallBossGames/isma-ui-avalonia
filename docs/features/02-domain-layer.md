# Domain Layer

## Purpose

The `ISMA.Domain` assembly provides pure C# (no UI dependencies) data models, DTOs, service contracts, and conversion logic used across the application for simulation results, blueprint statecharts, progress tracking, and metadata. It serves as the contract between the server-returned binary data and the UI's visualization pipeline.

## Structure

```
ISMA.Domain/
├── Models/
│   ├── BlueprintModel.cs                  # Serializable JSON blueprint model
│   ├── BlueprintStateModel.cs             # State data (position, name, text)
│   ├── BlueprintTransactionModel.cs       # Inter-state transition
│   ├── BlueprintLoopTransactionModel.cs   # Self-loop transition
│   ├── CompletedSimulation.cs             # Completed simulation result wrapper
│   ├── SimulationParameters.cs            # All simulation parameter sections
│   ├── SimulationProgress.cs              # Progress snapshot
│   ├── SimulationPoint.cs                 # Single simulation data point
│   ├── SimulationMetadata.cs              # Column name metadata
│   ├── ErrorInfo.cs                       # Compilation/validation error
│   ├── PreferencesModels.cs               # Window + file preferences
│   └── SavingTarget.cs                    # MEMORY / FILE enum
├── Dtos/
│   ├── DtoModels.cs                       # SyntaxTokenDto, CompilationErrorDto, etc.
│   └── ServiceContracts.cs                # SyntaxTokenKind enum
├── Contracts/
│   └── ServiceContracts.cs                # ISimulationServerFacade, etc.
└── Conversion/
    ├── BlueprintToLismaConverter.cs       # Blueprint → LISMA text conversion
    └── ResultSimplifier.cs                # Line simplification algorithms
```

## Models

### BlueprintModel

**File:** `Models/BlueprintModel.cs`

Immutable model representing a visual statechart. Serializable via `System.Text.Json`.

```csharp
public record BlueprintModel(
    BlueprintStateModel Main,
    BlueprintStateModel Init,
    BlueprintStateModel[] States,
    BlueprintTransactionModel[] Transactions,
    BlueprintLoopTransactionModel[] LoopTransactions
);
```

**Default empty model:**
```csharp
public static BlueprintModel Empty => new(
    new BlueprintStateModel(10.0, 10.0, "Main", "", isMain: true, isInit: false),
    new BlueprintStateModel(10.0, 100.0, "init", "", isMain: false, isInit: true),
    Array.Empty<BlueprintStateModel>(),
    Array.Empty<BlueprintTransactionModel>(),
    Array.Empty<BlueprintLoopTransactionModel>()
);
```

### BlueprintStateModel

**File:** `Models/BlueprintStateModel.cs`

```csharp
public record BlueprintStateModel(
    double CanvasPositionX,
    double CanvasPositionY,
    string Name,
    string Text,
    bool IsMain = false,
    bool IsInit = false
);
```

| Property | Type | Description |
|----------|------|-------------|
| `CanvasPositionX` | `double` | X coordinate on the canvas |
| `CanvasPositionY` | `double` | Y coordinate on the canvas |
| `Name` | `string` | State name (must be unique) |
| `Text` | `string` | LISMA body text for this state |
| `IsMain` | `bool` | Whether this is the Main state |
| `IsInit` | `bool` | Whether this is the Init state |

### BlueprintTransactionModel

**File:** `Models/BlueprintTransactionModel.cs`

```csharp
public record BlueprintTransactionModel(
    string StartStateName,
    string EndStateName,
    string Predicate,
    string Alias = ""
);
```

Represents an inter-state transition. State references are by name (resolved at load time).

### BlueprintLoopTransactionModel

**File:** `Models/BlueprintLoopTransactionModel.cs`

```csharp
public record BlueprintLoopTransactionModel(
    string StateName,
    string Predicate,
    string Alias = "",
    string Text = ""
);
```

Represents a self-loop transition. Unlike inter-state transitions, loop arrows carry `Text` (the LISMA body for the loop pseudo-state).

### CompletedSimulation

**File:** `Models/CompletedSimulation.cs`

Wraps a completed simulation result with metadata:

```csharp
public class CompletedSimulation
{
    public long Id { get; }
    public string Name { get; }
    public string CachedFilePath { get; }
    public IReadOnlyList<string> ColumnNames { get; }
    public MetricData MetricData { get; }
    public IEquationIndexProvider EquationIndexProvider { get; }
}
```

### SimulationParameters

**File:** `Models/SimulationParameters.cs`

Root model aggregating all simulation parameter sections:

```csharp
public class SimulationParameters
{
    public CauchyInitials CauchyInitials { get; set; }
    public IntegrationMethodParameters IntegrationMethod { get; set; }
    public EventDetectionParameters EventDetection { get; set; }
    public ResultSavingParameters ResultSaving { get; set; }
    public ResultProcessingParameters ResultProcessing { get; set; }
}
```

**Sub-models:**

| Sub-model | Properties |
|-----------|------------|
| `CauchyInitials` | `StartTime`, `EndTime`, `InitialStep` |
| `IntegrationMethodParameters` | `MethodName`, `Accuracy`, `IsAccuracyInUse`, `IsStabilityControlInUse`, `IsParallelInUse`, `Server`, `Port` |
| `EventDetectionParameters` | `IsEventDetectionInUse`, `Gamma`, `IsStepLimitInUse`, `LowBorder` |
| `ResultSavingParameters` | `SavingTarget` (`MEMORY` or `FILE`) |
| `ResultProcessingParameters` | `IsSimplifyInUse`, `SelectedSimplifyMethod`, `Tolerance` |

### SimulationPoint

**File:** `Models/SimulationPoint.cs`

Represents a single data point in the simulation output:

```csharp
public record SimulationPoint(
    double X,
    double[] YForDE,
    double[][] Rhs
);
```

| Property | Type | Description |
|----------|------|-------------|
| `X` | `double` | Independent variable (time) |
| `YForDE` | `double[]` | Dependent variables for differential equations |
| `Rhs` | `double[][]` | Right-hand side values (index 0 = DE, index 1 = AE) |

### SimulationProgress

**File:** `Models/SimulationProgress.cs`

Snapshot of simulation timing sent from the server during `MonitorSimulation()`:

```csharp
public record SimulationProgress(
    double StartTime,
    double EndTime,
    double CurrentTime
);
```

The UI normalizes `CurrentTime` to a 0.0–1.0 progress value.

### SimulationMetadata

**File:** `Models/SimulationMetadata.cs`

Column name metadata parsed from the binary result file:

```csharp
public record SimulationMetadata(
    IReadOnlyList<string> ColumnNames
);
```

Column names use prefix conventions (`DE_`, `AE_`, `f`) that feed into `BinaryEquationIndexProvider`.

### ErrorInfo

**File:** `Models/ErrorInfo.cs`

```csharp
public record ErrorInfo(
    int Row,
    int Position,
    string FragmentName,
    string Message
);
```

### PreferencesModels

**File:** `Models/PreferencesModels.cs`

```csharp
public record WindowPreferences(double X, double Y, double Width, double Height, bool IsMaximized);
public record DefaultFilesPreferences(IList<string> LastOpenedFiles);
public record Preferences(WindowPreferences Window, DefaultFilesPreferences DefaultFiles);
```

## DTOs

### SyntaxTokenDto

**File:** `Dtos/DtoModels.cs`

Server syntax highlighting tokens:

```csharp
public record SyntaxTokenDto(int Start, int Length, SyntaxTokenKind Kind);
```

### SyntaxTokenKind

**File:** `Dtos/ServiceContracts.cs`

```csharp
public enum SyntaxTokenKind
{
    Unspecified,
    Keyword,
    Comment,
    Number,
    Text
}
```

### CompilationErrorDto

**File:** `Dtos/DtoModels.cs`

```csharp
public record CompilationErrorDto(int Row, int Column, string Message);
```

## Service Contracts

**File:** `Contracts/ServiceContracts.cs`

Key interfaces defined in the domain layer:

| Interface | Purpose |
|-----------|---------|
| `ISimulationServerFacade` | All server operations (compile, validate, run, monitor, cancel, download, highlight) |
| `ISimulationResultReader` | Stream simulation points |
| `IEquationIndexProvider` | Derive equation info from column metadata |
| `ISyntaxHighlighter` | Request syntax tokens from server |
| `IProjectService` | Project lifecycle management |
| `IProjectFileService` | File open/save operations |
| `ISimulationServiceViewModel` | Simulation orchestration |
| `ISimulationResultService` | Result visualization and export |
| `IPreferencesProvider` | Preferences persistence |

## Conversion Logic

### BlueprintToLismaConverter

**File:** `Conversion/BlueprintToLismaConverter.cs`

Converts a `BlueprintModel` to LISMA text. Runs at compile/snapshot time, not during editing.

**Regular transactions:**
Each group of transitions targeting the same state with the same predicate produces a single `StateBlock`:
```
state "key" {
    <state text>
} from <startState1>,<startState2>,...;
```

Multiple transitions from different states to the same target with the same predicate are **merged** into a single `from` clause: `from StateA,StateB,StateC;`

**Loop transactions:**
Each loop is expanded into **two pseudo-states**:
```
state <stateName>_pseudo_1 (<predicate>) {
    <loop text>
} from <stateName>;

state <stateName> (1 > 0) {
    <original state text>
} from <stateName>_pseudo_1;
```

**Output order:**
1. Main state text (first, as top-level content)
2. All state blocks from regular transactions (grouped by target + predicate)
3. All loop transaction expansions (one pseudo-state pair per loop)

Each section is followed by a blank line.

### ResultSimplifier

**File:** `Conversion/ResultSimplifier.cs`

Post-processing simplification of simulation results using line-simplification algorithms:

| Algorithm | Description |
|-----------|-------------|
| **Radial-Distance** | Removes points based on radial distance thresholds. Faster but may distort sharp features. |
| **Douglas-Peucker** | Preserves the overall curve shape while removing fewer essential points. Better for curves with sharp turns. |

## DI Configuration

The domain assembly has no DI registrations of its own — all domain models are created ad-hoc (records/classes) or provided by the infrastructure assembly (`BinaryEquationIndexProvider`, `BinaryFilePointProvider`).

## Dependencies

| Assembly | Provides |
| --- | --- |
| `ISMA.Infrastructure` | `BinaryEquationIndexProvider` (implements `IEquationIndexProvider`) |
| `ISMA.Infrastructure` | `BinaryFilePointProvider` (implements `ISimulationResultReader`) |
| `ISMA.ViewModels` | `CompletedSimulation` wrapper for UI consumption |
| `ISMA.ViewModels` | `SimulationParameters` view model aggregation |
