# Use Case 1 — Create and Run a LISMA Text Project

**Actor:** Modeler (any user)
**Goal:** Write a LISMA mathematical model and execute a simulation to produce results.
**Preconditions:** ISMA UI is installed and launched. The ISMA server process is available.

## Main Flow

1. The user launches ISMA UI. The application restores the last session: previously opened files and window geometry are recovered from `~/.isma/preferences.json`.
2. The user creates a new LISMA text project by clicking **File > New Text** (or pressing `Ctrl+N`). A new tab titled "Untitled" opens with an empty AvaloniaEdit text editor.
3. The user types LISMA source code into the editor. As the user types, the server sends syntax highlighting tokens (debounced at 100ms) — keywords appear in orange, comments in gray, numbers in blue, and text literals in green.
4. The user configures simulation parameters in the **Settings Panel** (right sidebar):
   - **Cauchy Initials:** Sets `Start Time`, `End Time`, and `Initial Step` for the numerical integrator.
   - **Integration Method:** Selects an algorithm (e.g., Runge-Kutta) from the server-provided list, optionally enabling accuracy mode, stability control, or parallel execution.
   - **Event Detection:** Enables zero-crossing detection if needed, configuring `Gamma` and `Low Border` thresholds.
   - **Result Saving:** Chooses whether results are stored in memory or written to a file.
   - **Result Processing:** Optionally enables simplification (Radial-Distance or Douglas-Peucker algorithm with a tolerance value).
5. The user clicks **Simulation > Run** (or presses `Ctrl+F5`, or clicks the **Run** button in the bottom process bar).
6. The application transitions through the following status states:
   - *"Compiling..."* — sends the source code to the server for compilation via gRPC.
   - *"Running simulation..."* — initiates the simulation on the server.
   - *"Monitoring simulation..."* — establishes a server-streaming gRPC channel to receive progress updates.
   - *"Downloading results..."* — fetches the binary `.bin` result file via HTTP over a Unix Domain Socket.
   - *"Simulation complete"* — the result is committed locally.
7. During execution, a progress bar appears in the **Tasks PopOver** (accessed via the **Tasks** button), showing the simulation's progress from 0% to 100%. If compilation produces errors, they are displayed in the **Error List** DataGrid at the bottom of the window.
8. After completion, the simulation appears in the **Tasks PopOver > Completed** section with the model name, a **Show** button, an **Export** button, and a **Remove** button.

## Alternative Flows

- **5a. Verify first:** The user may click **Simulation > Verify** (`Ctrl+F4`) before running. The server validates the source code and returns errors/warnings to the Error List. The user fixes issues, then proceeds to step 5.
- **7a. Errors during compile:** If compilation fails, the Error List is populated with error rows (row number, position, fragment name, message). The simulation does not start. The user must fix the errors in the editor and retry.
- **7b. Errors during simulation:** If the server encounters a runtime error, the status updates with an error message and the simulation is removed from the In Progress section.

## Postconditions

- The simulation result is stored locally as a binary `.bin` file with column metadata (DE_*, AE_*, f* columns).
- The project tab remains open with the edited source code.
- If the project was unsaved, the "Untitled" tab title remains (the project is not auto-saved).

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` | `Run` command orchestration |
| `src/ISMA.ViewModels/Services/SimulationService.cs` | Compile → run → monitor → download pipeline |
| `src/ISMA.App/Views/SettingsPanelView.axaml` | Settings panel UI |
| `src/ISMA.App/Views/SimulationProcessBarView.axaml` | Run button |
| `src/ISMA.App/Views/TasksPopOverView.axaml` | Progress tracking UI |
