# Use Case 10 — Application Startup and Session Restore

**Actor:** User (any)
**Goal:** Launch ISMA UI and resume the previous work session automatically.
**Preconditions:** ISMA UI was previously used and has a `~/.isma/preferences.json` file.

## Main Flow

1. The user launches ISMA UI (e.g., via `dotnet run`, desktop shortcut, or package manager).
2. The application bootstraps:
   - `Program.cs` creates and shows the `MainWindow`.
   - `App.axaml.cs` initializes dependency injection and resolves the external tool paths (compiler CLI, runner CLI, LSP, Grin) from `appsettings.json` / environment variables.
3. No resident server process is started at launch. The **LismaCompilerCli** and **SimulationRunnerCli** launchers start the standalone CLI tools as child processes on demand (per compile/validate and per simulation run).
4. The **PreferencesProvider** loads `~/.isma/preferences.json` from the user's home directory (`~/.isma/`):
   - **Window geometry:** Position, size, and maximized state are restored.
   - **Last opened files:** Up to 5 previously opened file paths are loaded.
   - **Saved simulation parameters:** Any previously stored parameters are available.
5. The **MainWindow** applies the restored window geometry (position, size, maximized).
6. The **ProjectService** opens each of the last-opened files as tabs:
   - Files are opened in the order they were saved to preferences.
   - Each file is opened as a new tab with the appropriate editor (LISMA text or Blueprint canvas).
   - The most recently opened file becomes the `ActiveProject`.
7. The user sees the application in the same state as the previous session: same windows, same open files, same cursor position (within AvaloniaEdit).
8. The user can continue working immediately.

## Alternative Flows

- **4a. No preferences file:** If `~/.isma/preferences.json` does not exist (first run), the application starts with an empty window and no open files. The user creates or opens a project normally.
- **4b. Corrupt preferences:** If the preferences file is malformed JSON, loading fails gracefully. The window starts at default size/position with no files opened. An error dialog may be shown.
- **4c. File no longer exists:** If a last-opened file has been deleted or moved, the tab opens but shows an error or empty content. The user must re-open the file from the correct location.
- **5a. Stale child processes:** CLI child processes are short-lived and tied to a single operation, so a previous session cannot leave a shared server behind. A hung runner is terminated on app shutdown (`CliSimulationFacade.Shutdown()`).
- **6a. Partial restore:** If some last-opened files cannot be opened (e.g., format incompatibility), those files are skipped with a warning. Other files are restored successfully.

## Postconditions

- The application is fully functional with the previous session's state restored.
- All recoverable last-opened files are displayed as tabs.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.App/Program.cs` | Entry point — `BuildAvaloniaApp()` |
| `src/ISMA.App/App.axaml.cs` | DI initialization, external tool path resolution, MainWindow creation |
| `src/ISMA.ExternalServices/Cli/CliSimulationFacade.cs` | CLI-based simulation facade (compiler/runner child processes) |
| `src/ISMA.Infrastructure/FileStorage/PreferencesProvider.cs` | JSON preferences persistence (thread-safe with `ReaderWriterLockSlim`) |
| `src/ISMA.ViewModels/Services/ProjectService.cs` | Last-opened files restoration |
