# External Services

## Purpose

The `ISMA.ExternalServices` assembly handles all communication with external ISMA tools. It launches and supervises the standalone CLI tools — the LISMA compiler CLI (`lisma-compiler-cli`) and the simulation runner CLI (`simulation-runner-cli`) — via a facade that maps them to the `ISimulationServerFacade` contract, and it drives the standalone `isma-lsp` language server process for syntax highlighting. There is no server process: compilation and simulation run as short-lived child processes.

## Structure

```
ISMA.ExternalServices/
├── Cli/
│   ├── CliSimulationFacade.cs           # ISimulationServerFacade over the CLIs
│   ├── LismaCompilerCli.cs              # Compiler CLI launcher (stdin → .class)
│   ├── SimulationRunnerCli.cs           # Runner CLI launcher (simulation runs)
│   ├── BinaryFilePointProvider.cs       # Binary result file reader
│   └── BinaryEquationIndexProvider.cs   # Equation metadata from column names
├── Lsp/
│   ├── ILspTransport.cs                 # Byte-level LSP transport contract
│   ├── ProcessLspTransport.cs           # stdio transport with Content-Length framing
│   ├── LspProcessManager.cs             # isma-lsp process lifecycle
│   ├── LspClient.cs                     # JSON-RPC client (semantic tokens)
│   └── LspSyntaxHighlighter.cs          # ISyntaxHighlighter backed by LspClient
├── ChartViewer/
│   └── GrinProcessLauncher.cs           # Grin chart viewer process launcher
└── FileStorage/
    └── PreferencesProvider.cs           # JSON preferences persistence
```

## CLI Tool Lifecycle

### LismaCompilerCli

**File:** `Cli/LismaCompilerCli.cs`

Launches the LISMA compiler CLI. The script path is resolved from the `ISMA_COMPILER_CLI` environment variable or the `isma.compiler.cli` config key (set from `appsettings.json` `Compiler:ScriptPath` / `appsettings.Local.json`).

**Contract:** LISMA source is written to stdin.

| Mode | Exit 0 | Exit 1 |
| --- | --- | --- |
| Compile (default) | stdout = compiled `.class` bytes | stdout = one `row:col:message` error line per error |
| `--validate` | no output | stdout = error lines |

`row`/`col` are 1-based/0-based ANTLR positions; `-1` means the error has no source position. Any other exit code (or a `lisma-compiler-cli: <msg>` stderr line) is a launcher-level failure and throws `InvalidOperationException`.

### SimulationRunnerCli

**File:** `Cli/SimulationRunnerCli.cs`

Launches the simulation runner CLI. The script path is resolved from the `ISMA_RUNNER_CLI` environment variable or the `isma.runner.cli` config key (set from `appsettings.json` `Runner:ScriptPath`).

**Arguments:**

```
--model <compiled.class> --out <result.bin> --progress <file>
--start <t0> --end <t1> --step <h> --method <name>
[--accuracy <a> --accuracy-enabled] [--stability-enabled]
[--gamma <g> --low-border <b>]
```

- `--list-methods` prints the bundled integration method names, one per line (exit 0).
- Exit 0 = success; exit 1 = failure with `simulation-runner: <message>` on stderr and the `--out` file deleted.
- The `--progress` file is plain text, overwritten each step with the current time.
- Cancellation is SIGTERM (the runner's shutdown hook aborts the run).

### CliSimulationFacade

**File:** `Cli/CliSimulationFacade.cs`

Implements `ISimulationServerFacade` (domain contract) on top of the two launchers:

| Method | Implementation |
| --- | --- |
| `CompileModel(source)` | Runs the compiler, stores the `.class` in `%TEMP%/isma-ui/`, returns `ModelId` = the file path |
| `ValidateModel(source)` | Runs the compiler with `--validate`, parses error lines |
| `RunSimulation(params)` | Starts the runner with `--model <ModelId>` + a per-simulation `--out`/`--progress` pair, returns a local simulation ID |
| `MonitorSimulation(id)` | Polls the progress file every 100 ms until the process exits, yielding `SimulationProgress` |
| `DownloadResult(id)` | Waits for exit; non-zero exit throws with the runner stderr; otherwise returns the result file path + column names read from the binary header |
| `CancelSimulation(id)` | Sends SIGTERM to the runner process (hard kill fallback) |
| `GetSimulationMethods()` | Runs `--list-methods` |
| `Shutdown()` | Terminates running runner processes and deletes stored model files |

Model IDs are local file paths; simulation IDs are local counters. Compiled models and result files live in `%TEMP%/isma-ui/`.

## Data Providers

### BinaryFilePointProvider

**File:** `Cli/BinaryFilePointProvider.cs`

Implements `ISimulationResultReader`. Reads binary simulation results and streams `SimulationPoint` data:

```csharp
public class BinaryFilePointProvider : ISimulationResultReader
{
    public SimulationPoint[] ReadPoints(string filePath);
    public SimulationMetadata ReadMetadata(string filePath);
}
```

The binary exchange format (big-endian, no header/magic): `short` column count, per column `short` name length + UTF-8 name, then rows of `columnCount` big-endian doubles until EOF. Column order: `TIME`, then `DE_*`, then `AE_*`, then `f*`.

### BinaryEquationIndexProvider

**File:** `Cli/BinaryEquationIndexProvider.cs`

Implements `IEquationIndexProvider` by parsing column name prefixes:
- `DE_` — differential equation variables
- `AE_` — algebraic equation variables
- `f` — forcing functions

Derives equation counts and codes from the column metadata of the simulation result.

## Communication Flow: Simulation Run

```mermaid
sequenceDiagram
    participant UI as SimulationServiceViewModel
    participant Facade as CliSimulationFacade
    participant Comp as lisma-compiler-cli
    participant Run as simulation-runner-cli

    UI->>Facade: CompileModel(source)
    Facade->>Comp: stdin = source
    Comp-->>Facade: stdout = .class bytes (exit 0)
    Facade-->>UI: CompileResult(ModelId = .class path)

    UI->>Facade: RunSimulation(params)
    Facade->>Run: --model <ModelId> --out ... --progress ... --start ... --method ...
    Run-->>Facade: process started
    Facade-->>UI: simulationId

    loop until process exits
        Facade->>Run: read progress file (100 ms poll)
        Facade-->>UI: SimulationProgress
    end

    UI->>Facade: DownloadResult(id)
    Facade->>Run: wait for exit, read result file header
    Facade-->>UI: CachedSimulationResult(file, columnNames)
```

## External Processes

### GrinProcessLauncher

**File:** `ChartViewer/GrinProcessLauncher.cs`

Launches the GRIN chart viewer as a child process. Resolves script path from `appsettings.json` (`Grin:ScriptPath`) or environment variable `ISMA_GRIN_SCRIPT`.

**Arguments:**
| Argument | Description |
|----------|-------------|
| `--result-file` | Path to the binary simulation result file |
| `--x-axis` | X-axis column name |
| `--charts` | Comma-separated Y-axis column names |

## LSP Client

The `Lsp/` package drives the standalone `isma-lsp` language server for syntax highlighting.

### ILspTransport / ProcessLspTransport

**Files:** `Lsp/ILspTransport.cs`, `Lsp/ProcessLspTransport.cs`

`ILspTransport` is the byte-level transport contract: `Send(string json)`, `ReadNext(): string?` (blocking; null on end of stream), `Close()`. `ProcessLspTransport` backs it with a child process speaking LSP over stdio — stdout carries `Content-Length`-framed JSON-RPC 2.0 messages, stderr is drained to the logger.

### LspProcessManager

**File:** `Lsp/LspProcessManager.cs`

Manages the `isma-lsp` child process. The script path is resolved from `appsettings.json` (`Lsp:ScriptPath`) or environment variable `ISMA_LSP_SCRIPT`. `Start()` is idempotent — repeated calls return the same transport.

### LspClient

**File:** `Lsp/LspClient.cs`

Minimal LSP client. A reader thread dispatches responses to pending request tasks keyed by id. The `initialize`/`initialized` handshake is performed lazily on the first request.

| Member | Description |
| --- | --- |
| `SemanticTokensAsync(documentId, source)` | Opens the document on first call (`didOpen`), updates on subsequent calls (`didChange`), then requests `semanticTokens/full` and decodes the relative-format data into `LspToken(Line, StartChar, Length, Type)` records |
| `CloseDocument(documentId)` | Sends `textDocument/didClose` (idempotent) |
| `Shutdown()` | Best-effort: `shutdown` request, `exit` notification, transport close. Never throws |

Document URIs are `file:///isma/<documentId>.lisma`. Requests time out after 10 seconds.

### LspSyntaxHighlighter

**File:** `Lsp/LspSyntaxHighlighter.cs`

Implements `ISyntaxHighlighter` (domain contract) by delegating to `LspClient` and mapping LSP token types (`keyword`, `comment`, `number`) to `SyntaxTokenKind`.

## Preferences Persistence

### PreferencesProvider

**File:** `FileStorage/PreferencesProvider.cs`

JSON file persistence using `System.Text.Json`. Stores `Preferences` (window geometry + last opened files) at `%APPDATA%/isma/preferences.json` (platform-specific path).

```csharp
public class PreferencesProvider : IPreferencesProvider
{
    public Preferences Load();
    public void Save(Preferences preferences);
}
```

**Thread safety:** Uses a `ReaderWriterLockSlim` to allow concurrent reads with exclusive writes.

## Error Handling

| Scenario | Behavior |
| --- | --- |
| CLI script not found | `InvalidOperationException` — tool path not configured (`ISMA_COMPILER_CLI` / `ISMA_RUNNER_CLI` or appsettings keys) |
| Compiler exits with code > 1 | `InvalidOperationException` with the compiler stderr |
| Runner exits non-zero | `DownloadResult` throws `InvalidOperationException` with the runner stderr (`simulation-runner: <message>`) |
| Runner result file missing after exit | `InvalidOperationException` — result file not found |
| Unknown simulation ID | `InvalidOperationException` — simulation registry miss |
| Unknown script path (LSP/Grin) | LSP: `InvalidOperationException`; Grin: launch skipped with an error log |
