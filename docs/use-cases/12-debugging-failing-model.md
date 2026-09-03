# Use Case 12 — Complete Workflow: Debugging a Failing Model

**Actor:** Modeler
**Goal:** Identify, diagnose, and fix errors in a model through iterative verification and simulation.
**Preconditions:** ISMA UI is running. A LISMA text project or Blueprint statechart project is open.

## Main Flow

1. **Open project:** The user opens an existing project via **File > Open...** (`Ctrl+O`). The file may be a LISMA text project (`.im2`) or a Blueprint statechart (`.iscm2`).

2. **Initial run attempt:** The user clicks **Simulation > Run** (`Ctrl+F5`) expecting the model to execute.

3. **Compilation error:** The status shows *"Compiling..."* then displays an error message. The **Error List** DataGrid populates with one or more error rows:
   - Row 12, Position 5, Fragment "main": *"Unknown identifier 'xpr'*"
   - Row 45, Position 2, Fragment "Processing": *"Expected ';' at end of statement"*

4. **Navigate to errors:** The user reviews the Error List and identifies the issues:
   - A typo in variable name `xpr` (should be `x`).
   - A missing semicolon in the Processing state's body.

5. **Fix first error:** The user navigates to line 12 in the editor (for LISMA projects) or opens the relevant state's body tab (for Blueprint projects). The user corrects `xpr` to `x`.

6. **Fix second error:** The user navigates to line 45 and adds the missing semicolon.

7. **Verify:** The user clicks **Simulation > Verify** (`Ctrl+F4`) to check for remaining issues. The Error List is cleared and repopulated with one new error:
   - Row 78, Position 0, Fragment "Idle": *"Division by zero detected"*

8. **Fix third error:** The user navigates to the Idle state's body (line 78) and adds a guard condition to prevent division by zero (e.g., `if (denominator > 0.001)`).

9. **Verify again:** The user clicks **Verify** again. The Error List is now empty — the model is valid.

10. **Run:** The user clicks **Simulation > Run** (`Ctrl+F5`). The simulation executes successfully:
    - *"Compiling..."* → *"Running simulation..."* → *"Monitoring simulation..."* → *"Downloading results..."* → *"Simulation complete"*

11. **Inspect results:** The user clicks **Show** on the completed simulation, selects variables in the axis picker, and views the charts in Grin. The results look correct.

12. **Save:** The user clicks **File > Save** (`Ctrl+S`) to persist the fixes.

## Alternative Flows

- **3a. Runtime error:** If the model compiles but fails during simulation, the status shows a runtime error message. The Error List may not be populated (runtime errors are not always returned as structured error list entries). The user must read the error message and fix the model accordingly.
- **4a. Many errors:** If the Error List contains dozens of errors, some may be cascading (one root cause producing many symptoms). The user prioritizes errors by row number, fixing the earliest line first, then re-verifying to see if subsequent errors disappear.
- **7a. Blueprint debugging:** For Blueprint projects, errors in state bodies are reported with the state name in the Fragment column. The user double-clicks the relevant state to open its body editor tab, makes fixes, and re-verifies.
- **10a. Slow simulation:** If the simulation takes a long time, the user may click **Abort** (see Use Case 8) and adjust parameters (e.g., reduce End Time) before re-running.

## Postconditions

- The model is error-free and produces valid simulation results.
- Fixes are saved to disk.
- The user has a confirmed working result set available for visualization or export.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/ErrorListViewModel.cs` | Error list display and management |
| `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` | `ValidateAsync` for verification |
| `src/ISMA.ViewModels/ViewModels/SimulationService.cs` | Compile/run pipeline with error handling |
| `src/ISMA.App/Views/MainWindow.axaml` | Error List DataGrid layout |
