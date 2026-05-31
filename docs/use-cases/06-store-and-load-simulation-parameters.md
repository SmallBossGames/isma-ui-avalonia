# Use Case 6 — Store and Load Simulation Parameters

**Actor:** Modeler
**Goal:** Save simulation parameters to a JSON file for reuse, or load previously saved parameters into the current session.
**Preconditions:** ISMA UI is running. At least one project is open (simulation parameters are project-scoped).

## Main Flow — Store Settings

1. The user configures simulation parameters in the Settings Panel (right sidebar):
   - **Cauchy Initials:** `Start Time`, `End Time`, `Initial Step`.
   - **Integration Method:** Selected algorithm, accuracy, stability, parallel settings.
   - **Event Detection:** Enabled/disabled, `Gamma`, `Low Border`.
   - **Result Saving:** `Memory` or `File` target.
2. The user clicks **Simulation > Store Settings**.
3. A file picker dialog opens with a `.params.json` file filter.
4. The user chooses a filename and location, then confirms.
5. The `SimulationParametersViewModel.Snapshot()` method captures the current parameter values.
6. The parameters are serialized to JSON and written to the selected file. The serialized format includes:
   - Cauchy initials (start, end, step)
   - Integration method (selected method, accuracy flag, accuracy value, stability flag, parallel flag, server, port)
   - Event detection (enabled flag, gamma, step limit flag, low border)
   - Result saving (target enum)
   - **Note:** Result processing settings (simplify, method, tolerance) are NOT stored.
7. A success confirmation is shown to the user.

## Main Flow — Load Settings

1. The user has simulation parameters configured (either default values or previously loaded values).
2. The user clicks **Simulation > Load Settings**.
3. A file picker dialog opens with a `.params.json` file filter.
4. The user selects a previously stored parameters file and confirms.
5. The file is read and deserialized into a parameter model.
6. The Settings Panel controls are updated with the loaded values:
   - Cauchy initials fields populate with start, end, step.
   - Integration method dropdown selects the stored method; checkboxes and numeric fields update accordingly.
   - Event detection controls enable/disable and populate values.
   - Result saving dropdown selects the stored target.
7. The user can now run the simulation with the loaded parameters.

## Alternative Flows

- **3a/4a. No file selected:** If the user cancels the file picker, no action is taken. Parameters remain unchanged.
- **5a. Corrupt or incompatible JSON:** If the file cannot be deserialized (wrong format, missing fields), an error dialog is displayed. Parameters remain unchanged.
- **6a. Partial parameters:** If the JSON file omits some fields, only the present fields are updated; others retain their current values.

## Postconditions

- **Store:** A `.params.json` file exists on disk with the current simulation parameters.
- **Load:** The Settings Panel reflects the loaded parameter values, ready for simulation.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/SimulationParametersViewModel.cs` | `Snapshot()` and parameter aggregation |
| `src/ISMA.ViewModels/Services/SimulationParametersService.cs` | JSON serialization/deserialization |
| `src/ISMA.App/ViewModels/MainWindowViewModel.cs` | `StoreSettings` and `LoadSettings` commands |
| `src/ISMA.App/Views/SettingsPanelView.axaml` | Settings panel with `PropertiesGrid` controls |
