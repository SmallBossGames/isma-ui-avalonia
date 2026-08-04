# ISMA UI Avalonia

Cross-platform desktop client for the ISMA mathematical modeling environment, built with C#, .NET, and AvaloniaUI.

## Purpose

ISMA UI Avalonia is the desktop client for the ISMA mathematical modeling environment. It provides a multi-project editing interface with syntax-highlighted LISMA source code, a visual statechart (blueprint) editor, simulation execution pipeline, and result visualization. The UI runs as a separate process from `isma-server`, communicating via gRPC over Unix Domain Sockets and HTTP over Unix sockets.

## Quick Navigation

| Document | Description |
| --- | --- |
| [Overview](01-overview.md) | Architecture, module structure, dependency graph, DI wiring |
| [Domain Layer](02-domain-layer.md) | Domain models, interfaces, result streaming |
| [External Services](03-external-services.md) | gRPC client, HTTP client, server lifecycle, facade |
| [UI Components](04-ui-components.md) | App entry point, views, editors, toolbars, models |
| [UX Reference](05-ux-reference.md) | Complete user experience specification: windows, menus, dialogs, transitions, features |
| [Blueprint Editor Architecture](06-blueprint-editor-architecture.md) | Technical architecture: module structure, MVVM pattern, data flow, controls, utilities, serialization |
| [Blueprint Editor UX](08-blueprint-editor-ux.md) | Detailed specification of the visual statechart editor: canvas, states, arrows, popover, toolbar, modes, LISMA conversion |
| [Build & Deployment](07-build-and-deployment.md) | .NET project structure, dependencies, startup |

## Key Files

| File | Role |
| --- | --- |
| `src/ISMA.App/Program.cs` | Avalonia `Application` entry point |
| `src/ISMA.App/App.axaml.cs` | DI configuration, server path resolution, main window creation |
| `src/ISMA.App/MainWindow.axaml` | Main window layout: menu, toolbar, editor tab pane, settings panel, error list, process bar |
| `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` | All application commands: File, Edit, Simulation, Settings |
| `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` | LISMA text project with syntax highlighting |
| `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` | Visual statechart with states, transitions, loop arrows |
| `src/ISMA.ViewModels/ViewModels/SimulationServiceViewModel.cs` | Simulation orchestration: compile, run, monitor, download |
| `src/ISMA.Infrastructure/Server/SimulationServerFacade.cs` | Server communication facade |
| `src/ISMA.Domain/Conversion/BlueprintToLismaConverter.cs` | Blueprint-to-LISMA conversion |
| `src/ISMA.App/Views/IsmaTextEditorView.axaml` | AvaloniaEdit-based LISMA text editor |
| `src/ISMA.App/Views/BlueprintEditorView.axaml` | Visual statechart editor canvas |
| `src/ISMA.App/Services/ServerDrivenHighlightingTransformer.cs` | Server-driven syntax highlighting for AvaloniaEdit |
| `src/ISMA.App/Controls/PropertiesGrid.axaml` | Reusable property grid component |
| `src/ISMA.Infrastructure/FileStorage/PreferencesProvider.cs` | JSON preferences persistence |

## Building

```bash
dotnet build isma-ui-dotnet.slnx
```

## Running

Requires `appsettings.json` with server and Grin launcher paths:

```json
{
  "Server": { "ScriptPath": "/path/to/isma-server" },
  "Grin": { "ScriptPath": "/path/to/grin" }
}
```

When running via the provided `run.sh` script, these paths are auto-configured.

## Module Index

| Module | Description | Main Package |
| --- | --- | --- |
| `ISMA.Domain` | Pure C# domain models, DTOs, contracts, conversion logic | `ISMA.Domain` |
| `ISMA.ViewModels` | MVVM ViewModels, converters, VM-level services | `ISMA.ViewModels` |
| `ISMA.Infrastructure` | Server communication (gRPC/HTTP), file storage, chart viewer | `ISMA.Infrastructure` |
| `ISMA.App` | UI views, controls, app entry point, DI composition | `ISMA.App` |

## Documents by Audience

| Document | Target Audience |
| --- | --- |
| [01-overview](01-overview.md) | Architects, contributors understanding module layout |
| [02-domain-layer](02-domain-layer.md) | Backend developers working with simulation models |
| [03-external-services](03-external-services.md) | Developers modifying server communication |
| [04-ui-components](04-ui-components.md) | Developers modifying existing Avalonia UI |
| [05-ux-reference](05-ux-reference.md) | Anyone implementing a replacement UI with feature parity |
| [06-blueprint-editor-architecture](06-blueprint-editor-architecture.md) | Developers implementing or modifying the blueprint editor module |
| [08-blueprint-editor-ux](08-blueprint-editor-ux.md) | Migrator implementing the Avalonia statechart editor, or anyone needing deep canvas/interaction details |
| [07-build-and-deployment](07-build-and-deployment.md) | DevOps, contributors setting up the build |
