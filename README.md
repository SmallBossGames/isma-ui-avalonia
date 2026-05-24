# ISMA UI (.NET / Avalonia)

Cross-platform desktop application for the ISMA simulation platform, built with Avalonia 12 and .NET 10.

## Architecture

The project follows a 5-layer Clean Architecture with explicit dependency direction:

```
ISMA.App (Presentation)
  ├── ISMA.ViewModels (Presentation - ViewModel layer)
  │     ├── ISMA.Domain (Domain)
  │     └── Microsoft.Extensions.DependencyInjection.Abstractions
  ├── ISMA.Infrastructure (Infrastructure)
  │     └── ISMA.Domain
  └── ISMA.Domain (Domain - no dependencies)

ISMA.Tests (Test project)
  ├── ISMA.Domain
  └── ISMA.ViewModels
```

### Layer responsibilities

| Layer | Purpose |
|---|---|
| **ISMA.Domain** | Pure domain models, DTOs, contracts, and domain logic. Zero external dependencies. |
| **ISMA.Infrastructure** | gRPC/HTTP client implementations, file storage, process launchers, and external service integrations. |
| **ISMA.ViewModels** | CommunityToolkit.Mvvm view models, services, converters, and the DI registration surface. |
| **ISMA.App** | Avalonia UI — views (AXAML), application bootstrapper, DI wiring, and platform services. |
| **ISMA.Tests** | xUnit tests covering domain models, view models, and conversion logic. |

### Dependency graph

```
ISMA.App ──► ISMA.ViewModels ──► ISMA.Domain
     │              │
     └──────────────┼──► ISMA.Infrastructure ──► ISMA.Domain
```

The Application layer depends on ViewModels and Infrastructure. ViewModels depend on Domain only. Infrastructure depends on Domain only. This ensures the domain remains testable and framework-agnostic.

## Key features

- **MVVM pattern** — CommunityToolkit.Mvvm with compiled bindings (`x:DataType`), `ObservableObject`, and `IRelayCommand`
- **Dependency Injection** — `Microsoft.Extensions.DependencyInjection` wired in `App.axaml.cs`
- **Blueprint editor** — Visual CAPE-OPEN/ISMA blueprint authoring with canvas, states, transactions, and loop transactions
- **Syntax highlighting** — AvaloniaEdit with LISMA language syntax highlight definition, backed by server-side tokenization via gRPC
- **Simulation management** — Run, stop, and monitor ISMA simulations via gRPC or HTTP against the simulation server
- **Properties grid** — Custom `PropertiesGrid` control for editing simulation parameters
- **Error list** — DataGrid-bound error list with row, position, fragment, and message columns
- **Fluent theme** — Native-looking UI with custom global styles (colors, fonts, control templates)
- **Cross-platform** — Runs on Windows, Linux, and macOS via Avalonia desktop

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| Avalonia | 12.0.3 | UI framework |
| Avalonia.Themes.Fluent | 12.0.3 | Fluent design theme |
| Avalonia.Controls.DataGrid | 12.0.0 | Data grid control |
| Avalonia.AvaloniaEdit | 12.0.0 | Code editing control |
| AvaloniaEdit.TextMate | 12.0.0 | TextMate syntax integration |
| CommunityToolkit.Mvvm | 8.4.0 | MVVM toolkit |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | DI container |
| Grpc.Net.Client | 2.71.0 | gRPC client |
| Grpc.Net.Client.Web | 2.71.0 | gRPC-Web client |
| Google.Protobuf | 3.33.0 | Protocol Buffers |
| xUnit | 2.9.3 | Testing framework |
| FluentAssertions | 8.2.0 | Fluent assertions |
| Moq | 4.20.72 | Mocking framework |

## Build

```bash
cd isma-ui-dotnet
dotnet build
```

Centralized package management is enabled via `Directory.Packages.props`. All versions are pinned in one place.

## Run

```bash
cd isma-ui-dotnet
dotnet run --project ISMA.App
```

The application bootstraps via `Program.cs`, configures the DI container in `App.axaml.cs`, and creates the `MainWindow`.

## Test

```bash
cd isma-ui-dotnet
dotnet test
```

Tests use xUnit with FluentAssertions and Moq. The test project covers:

- **Domain** — `BlueprintModel`, `SimulationPoint`, `SimulationParameters`, `CodeRegion`, `Preferences`
- **ViewModels** — `BlueprintEditorViewModel`, `MainWindowViewModel`, `SimulationService`, `SimulationParametersViewModel`, `ErrorListViewModel`, `ProjectViewModel`, blueprint-to-LISMA conversion

## Architectural decisions

1. **Compiled bindings by default** — `AvaloniaUseCompiledBindingsByDefault` is enabled globally. All views use `x:DataType` for type-safe, performant bindings.

2. **CommunityToolkit.Mvvm** — Chosen over ReactiveUI/HandyMvvm for its lightweight source-generator approach, minimal runtime overhead, and broad adoption.

3. **gRPC-first communication** — The simulation server is contacted via gRPC for simulation execution, syntax highlighting, and metadata. HTTP is used as a fallback transport via `Grpc.Net.Client.Web`.

4. **Server-side syntax highlighting** — Tokenization is delegated to the ISMA server for consistency with other clients. The `SyntaxHighlighterService` wraps the gRPC call and feeds results to AvaloniaEdit.

5. **Immutable domain collections** — Blueprint states and transactions use `ImmutableArray<T>` to prevent accidental mutation and support structural sharing.

6. **Centralized package management** — CPVM (`Directory.Packages.props`) ensures consistent versions across all projects and enables MAUI-style versioning.

7. **No XAML code-behind logic** — Views contain only markup. All logic lives in view models or dedicated services injected via DI.
