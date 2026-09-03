# Use Case 11 — Complete Workflow: Blueprint Authoring to Result Visualization

**Actor:** Modeler
**Goal:** Design a complete visual state machine model, run its simulation, and visualize the results — all within ISMA UI.
**Preconditions:** ISMA UI is running.

## Main Flow

1. **Create blueprint:** The user clicks **File > New Statechart** (`Ctrl+B`). A new "Untitled Blueprint" tab opens with Main and Init states on the canvas.

2. **Design state machine:**
   - The user adds three user states by clicking the **New state** button and placing them on the canvas. The states are auto-named "State1", "State2", "State3".
   - The user renames the states to meaningful names: "Idle", "Processing", "Complete".
   - The user repositions all states on the canvas for a clear layout: Init → Idle → Processing → Complete.
   - The user toggles **New transition** and creates transitions:
     - Init → Idle (predicate: `"ready"`)
     - Idle → Processing (predicate: `"start"`)
     - Processing → Complete (predicate: `"done"`)
     - Processing → Idle (predicate: `"error"`)
   - The user clicks each arrowhead to add aliases: "Ready", "Start", "Done", "Error".
   - The user double-clicks the Init state to open its LISMA body editor and writes initialization code.
   - The user double-clicks the Idle state and writes its body content.
   - The user double-clicks the Processing state and writes its body content.
   - The user double-clicks the Complete state and writes its body content.
   - The user adds a loop transaction on the Processing state (for retry logic) by using the loop arrow feature, then double-clicks the loop arrowhead to write the loop body.

3. **Configure simulation:** The user sets simulation parameters in the Settings Panel:
   - Cauchy Initials: Start=0, End=100, Step=0.1
   - Integration Method: RK4, Accuracy enabled
   - Event Detection: Enabled, Gamma=0.8
   - Result Saving: File

4. **Verify:** The user clicks **Simulation > Verify** (`Ctrl+F4`). The Blueprint is converted to LISMA text and validated. The Error List is empty — the model is valid.

5. **Run:** The user clicks **Simulation > Run** (`Ctrl+F5`). The status transitions:
   - *"Compiling..."* → *"Running simulation..."* → *"Monitoring simulation..."* → *"Downloading results..."* → *"Simulation complete"*
   - The progress bar in the Tasks PopOver advances from 0% to 100%.

6. **Inspect details:** The user clicks the **Details** chevron on the completed simulation to review the parameters used (Cauchy initials, integration method, accuracy settings).

7. **Visualize:** The user clicks **Show** on the completed simulation. The Select Variables Dialog opens. The user selects TIME as X-axis and DE_1, DE_2, AE_1 as Y-axes. Clicks **Ok**. The Grin chart viewer launches, displaying the simulation results as interactive plots.

8. **Export:** The user closes Grin and returns to ISMA UI. The user clicks **Export** on the same completed simulation. A file picker opens. The user saves the result as `model_results.csv`.

9. **Save blueprint:** The user clicks **File > Save As** (`Ctrl+Shift+S`), saves the blueprint as `my_statechart.iscm2`.

## Alternative Flows

- **4a. Verification errors:** If Verify returns errors, the user fixes them in the appropriate editor tabs (state body tabs or the blueprint canvas), then re-verifies.
- **5a. Simulation failure:** If the server reports a runtime error, the status shows the error message. The user checks the Error List, fixes the model, and re-runs.
- **7a. Grin unavailable:** If Grin is not configured, the user sees an error dialog. The result is still available for CSV export.

## Postconditions

- A visual statechart blueprint file (`my_statechart.iscm2`) is saved on disk.
- A CSV result file (`model_results.csv`) is saved on disk.
- The blueprint tab remains open with all states, transitions, and loop transactions intact.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.BlueprintEditor/ViewModels/IsmaBlueprintViewModel.cs` | Canvas state machine management |
| `src/ISMA.App/Services/Blueprint/LismaCodegen.cs` | Visual-to-text transformation |
| `src/ISMA.App/Services/SimulationService.cs` | Full simulation pipeline |
| `src/ISMA.ExternalServices/ChartViewer/GrinProcessLauncher.cs` | Chart viewer launch |
| `src/ISMA.BlueprintEditor/Views/CanvasView.axaml` | Visual canvas |
