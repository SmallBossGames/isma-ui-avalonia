# Use Case 7 — Visualize and Export Simulation Results

**Actor:** Modeler / Analyst
**Goal:** View simulation results as charts and/or export them to CSV for external analysis.
**Preconditions:** A simulation has completed successfully. The result is available in the Tasks PopOver > Completed section.

## Main Flow — Visualize in Grin Chart

1. The user runs a simulation and waits for completion (see Use Case 1).
2. Upon completion, the simulation appears in the **Tasks PopOver > Completed** section.
3. The user clicks the **Show** button on the completed simulation row.
4. A **Select Variables Dialog** opens, displaying:
   - **X-axis selector:** Dropdown with TIME as the default option.
   - **Y-axis multi-selector:** List of all available columns (DE_* for differential equations, AE_* for algebraic equations, f* for function outputs). Checkboxes allow selecting multiple Y variables. A **Select All** / **UnselectAll** utility is provided.
5. The user selects the X-axis variable (typically TIME) and one or more Y-axis variables to plot.
6. The user clicks **Ok**.
7. The application launches the **Grin chart viewer** as a separate child process with the following arguments:
   - `--result-file <path>` — path to the cached binary `.bin` result file.
   - `--x-axis <col>` — the selected X-axis column name.
   - `--charts <col1,col2,...>` — comma-separated Y-axis column names.
8. The Grin viewer loads the result file and renders interactive charts.
9. The user interacts with the charts (zoom, pan, inspect data points) within the Grin application.
10. The user closes the Grin viewer when done.

## Main Flow — Export to CSV

1. The user has a completed simulation in the Tasks PopOver.
2. The user clicks the **Export** button on the completed simulation row.
3. A file picker dialog opens with a CSV file filter (`*.csv`).
4. The user chooses a filename and location, then confirms.
5. The application reads the binary `.bin` result file from disk.
6. The binary data is converted to CSV format:
   - Headers: `x, DE_*, AE_*, f*` (independent variable, differential equation outputs, algebraic equation outputs, function outputs).
   - Each `SimulationPoint` becomes a row with X, YForDe, Rhs values.
   - Encoding: UTF-8.
7. The CSV file is written to the selected location.
8. A success confirmation is shown to the user.

## Alternative Flows

- **3a. Details flyout:** Before showing or exporting, the user may click the **Details** chevron (`...`) button to view simulation metadata: model name, Cauchy initials (start/end/step), integration method (algorithm, accuracy, stability settings).
- **7a. Grin not found:** If the Grin script path is not configured in `appsettings.json` and the `ISMA_GRIN_SCRIPT` environment variable is not set, an error dialog informs the user that the chart viewer is unavailable.
- **4a. Export cancelled:** If the user cancels the file picker, no export occurs.
- **6a. Large result files:** For simulations with many data points, the CSV export may take noticeable time. The UI remains responsive as the conversion runs on a background thread.

## Postconditions

- **Visualize:** Grin viewer displays the selected variables as charts. The result file remains cached locally.
- **Export:** A CSV file exists on disk with the simulation data, suitable for import into spreadsheets or analysis tools.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.App/Views/SelectVariablesDialogWindow.axaml` | Axis picker dialog |
| `src/ISMA.App/Views/TasksPopOverView.axaml` | Show/Export buttons |
| `src/ISMA.Infrastructure/ChartViewer/GrinProcessLauncher.cs` | Grin child process management |
| `src/ISMA.App/Services/SimulationResultService.cs` | Chart display and CSV export |
| `src/ISMA.Domain/Models/CompletedSimulation.cs` | Result data model |
