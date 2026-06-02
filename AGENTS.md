# ISMA UI — Agent Instructions

## Commands

```bash
dotnet build isma-ui-dotnet.slnx
dotnet run --project src/ISMA.App
dotnet test
```

No `cd` required — the solution file is at the repo root.

## Architecture

5-layer Clean Architecture, dependency direction is strict:

```
ISMA.App ──► ISMA.ViewModels ──► ISMA.Domain
     │              │
     └──────────────┼──► ISMA.Infrastructure ──► ISMA.Domain
```

| Project | Layer | What it owns |
|---|---|---|
| `src/ISMA.Domain` | Domain | Pure models, DTOs, contracts, conversion logic. Zero external deps. |
| `src/ISMA.Infrastructure` | Infrastructure | gRPC/HTTP clients, file storage, process launchers, chart viewer. |
| `src/ISMA.ViewModels` | Presentation | CommunityToolkit.Mvvm ViewModels, VM-level services, DI abstractions. |
| `src/ISMA.App` | Presentation | Avalonia views (AXAML), controls, bootstrapper, concrete DI wiring. |
| `tests/ISMA.Tests` | Tests | xUnit v3 unit tests (domain + viewmodels). |
| `tests/ISMA.Tests.Integration` | Tests | xUnit v3 headless UI tests (Avalonia.Headless.XUnit). |

## Framework specifics

- **Avalonia 12.0.3** — `AvaloniaUseCompiledBindingsByDefault` is enabled globally in `Directory.Build.props`. All views use `x:DataType`.
- **CommunityToolkit.Mvvm** — `[ObservableProperty]`, `[RelayCommand]`, source generators. No reactive frameworks.
- **DI** — `Microsoft.Extensions.DependencyInjection`. App layer registers everything in `ServiceCollectionExtensions.cs`. Tests reuse `ConfigureTestServices()` from the same file.
- **Server communication** — gRPC over Unix Domain Sockets, with HTTP (gRPC-Web) as fallback. The `ISimulationServerFacade` is the single abstraction.
- **No XAML code-behind logic** — Views contain only markup. All logic is in ViewModels or services.

## Testing

Two test projects with different scopes:

**Unit tests** (`tests/ISMA.Tests`): Domain models and ViewModels. Uses Moq for interfaces.

**Integration tests** (`tests/ISMA.Tests.Integration`): Headless Avalonia UI tests.
- All tests inherit from `IntegrationTestBase` which sets up a headless window with the app's real DI config.
- The only difference from production DI is `ISimulationServerFacade` is replaced with `MockSimulationServerFacade`.
- Use `UiHelpers` for UI interactions: `window.Flush()`, `window.ClickMenuItem()`, `window.SetEditorText()`, etc.
- Always call `window.Flush()` after UI interactions to force layout in headless mode.
- Integration tests use `[Xunit.Headless]` via `GlobalUsings.cs` (`global using global::Xunit;`).

## Package management

Centralized via `Directory.Packages.props` at both root and `src/` level. All versions pinned in one place. Never edit a `.csproj` to change a version.

## Codestyle Rules

### C#
- Prefer primary constructors
- Prefer records for data models
- Prefer `init` instead of `set` in auto-properties
- Add XML documentation comments for public interfaces
- Add XML documentation comments for data models

## Key files

| File | Role |
|---|---|
| `src/ISMA.App/Program.cs` | Entry point — `BuildAvaloniaApp()` |
| `src/ISMA.App/App.axaml.cs` | DI initialization, server path resolution, MainWindow creation |
| `src/ISMA.App/ServiceCollectionExtensions.cs` | Shared DI config for app + integration tests |
| `src/ISMA.App/Styles/GlobalStyles.axaml` | Global theme: colors, fonts, control styles |
| `src/ISMA.App/MainWindow.axaml` | Main window: menu, toolbar, editor tabs, settings, error list, process bar |
| `src/ISMA.ViewModels/Services/ProjectService.cs` | Multi-project lifecycle (open, save, close, tabs) |
| `run.sh` | Convenience script — auto-detects script dir for `appsettings.json` |

## Docs

Feature documentation lives in `docs/features/`. Start at `docs/features/README.md`.
Use cases documentation lives in `docs/use-cases/`. Start at `docs/use-cases/README.md`.
