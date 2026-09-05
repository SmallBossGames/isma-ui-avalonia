# ISMA UI (.NET / Avalonia)

Cross-platform desktop application for the ISMA simulation platform, built with Avalonia 12 and .NET 10.

## Architecture

The project follows a layered Clean Architecture with explicit dependency direction, mirroring the module layout of the original ISMA UI:

```
src/
  ├── ISMA.App (Presentation)
  │     ├── ISMA.Domain
  │     ├── ISMA.ExternalServices
  │     ├── ISMA.BlueprintEditor
  │     ├── ISMA.TextEditor
  │     └── ISMA.Toolkit
  ├── ISMA.ExternalServices (Infrastructure)
  │     ├── ISMA.Domain
  │     └── ISMA.Grpc
  ├── ISMA.TextEditor (Presentation)
  │     └── ISMA.Domain
  ├── ISMA.BlueprintEditor (Presentation — zero project dependencies)
  ├── ISMA.Toolkit (shared UI toolkit — zero project dependencies)
  ├── ISMA.Grpc (generated gRPC stubs — zero project dependencies)
  └── ISMA.Domain (Domain — zero project dependencies)

tests/
  ├── ISMA.Tests (unit tests)
  │     ├── ISMA.Domain
  │     ├── ISMA.App
  │     └── ISMA.BlueprintEditor
  └── ISMA.Tests.Integration (headless Avalonia UI tests)
        ├── ISMA.Domain
        └── ISMA.App
```

### Layer responsibilities

| Layer | Purpose |
|---|---|
| **ISMA.Domain** | Pure domain models, DTOs, contracts, and domain logic. Zero external dependencies. |
| **ISMA.Grpc** | Generated gRPC/protobuf stubs for the simulation server. Zero project dependencies. |
| **ISMA.ExternalServices** | gRPC client implementations, process launchers, and external service integrations. |
| **ISMA.Toolkit** | Shared UI toolkit: `PropertiesGrid` control, value converters, and presentation helpers. |
| **ISMA.TextEditor** | AvaloniaEdit-based LISMA text editor (`IsmaTextEditor`) with syntax highlighting. |
| **ISMA.BlueprintEditor** | Visual blueprint (statechart) editor: canvas, states, transactions, loop transactions. Framework-agnostic view models, zero project dependencies. |
| **ISMA.App** | Avalonia UI — views (AXAML), application bootstrapper, DI wiring, project lifecycle, and platform services. |
| **ISMA.Tests** | xUnit unit tests covering domain models, view models, and editor logic. |
| **ISMA.Tests.Integration** | Headless Avalonia end-to-end tests driving the real UI with pointer events. |

### Dependency graph

```
ISMA.App ──► ISMA.ExternalServices ──► ISMA.Grpc
     │              │
     ├──► ISMA.TextEditor ────────────► ISMA.Domain
     ├──► ISMA.BlueprintEditor
     ├──► ISMA.Toolkit
     └──► ISMA.Domain
```

The Application layer depends on all presentation and infrastructure modules; each module depends on `ISMA.Domain` (and `ISMA.Grpc` for transport) only. This keeps the domain testable and framework-agnostic, and lets the editors ship as self-contained modules.

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
| Avalonia | 12.1.2 | UI framework |
| Avalonia.Themes.Fluent | 12.1.2 | Fluent design theme |
| Avalonia.Controls.DataGrid | 12.1.2 | Data grid control |
| Avalonia.AvaloniaEdit | 12.0.0 | Code editing control |
| AvaloniaEdit.TextMate | 12.0.0 | TextMate syntax integration |
| Avalonia.Wayland | 12.1.2 | Native Wayland backend (Linux) |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM toolkit |
| Microsoft.Extensions.DependencyInjection | 10.0.11 | DI container |
| Grpc.Net.Client | 2.83.0 | gRPC client |
| Grpc.Net.Client.Web | 2.83.0 | gRPC-Web client |
| Google.Protobuf | 3.36.1 | Protocol Buffers |
| xunit.v3 | 4.0.0 | Testing framework |
| FluentAssertions | 8.10.0 | Fluent assertions |
| Moq | 4.20.72 | Mocking framework |

## Build

```bash
dotnet build isma-ui-dotnet.slnx
```

Centralized package management is enabled via `Directory.Packages.props`. All versions are pinned in one place.

## Run

```bash
dotnet run --project src/ISMA.App
```

The application bootstraps via `Program.cs`, configures the DI container in `App.axaml.cs`, and creates the `MainWindow`.

## Test

```bash
dotnet test
```

Tests use xUnit v3 with FluentAssertions and Moq:

- **ISMA.Tests** (unit) — domain models, app view models, blueprint editor view models (`IsmaBlueprintViewModel`, `CanvasViewModel`, `EditorMode`), and editor utilities (`ArrowGeometryCalculator`, `NameChangingMonitor`), blueprint-to-LISMA conversion
- **ISMA.Tests.Integration** (headless UI) — end-to-end flows driving the real Avalonia UI with pointer events, including blueprint canvas interactions (state creation, dragging, transitions, loop transitions, rename, delete, save/reload round-trip)

## Architectural decisions

1. **Compiled bindings by default** — `AvaloniaUseCompiledBindingsByDefault` is enabled globally. All views use `x:DataType` for type-safe, performant bindings.

2. **CommunityToolkit.Mvvm** — Chosen over ReactiveUI/HandyMvvm for its lightweight source-generator approach, minimal runtime overhead, and broad adoption.

3. **gRPC-first communication** — The simulation server is contacted via gRPC for simulation execution, syntax highlighting, and metadata. HTTP is used as a fallback transport via `Grpc.Net.Client.Web`.

4. **Server-side syntax highlighting** — Tokenization is delegated to the ISMA server for consistency with other clients. The `SyntaxHighlighterService` wraps the gRPC call and feeds results to AvaloniaEdit.

5. **Immutable domain collections** — Blueprint states and transactions use `ImmutableArray<T>` to prevent accidental mutation and support structural sharing.

6. **Centralized package management** — CPVM (`Directory.Packages.props`) ensures consistent versions across all projects and enables MAUI-style versioning.

7. **No XAML code-behind logic** — Views contain only markup. All logic lives in view models or dedicated services injected via DI.
