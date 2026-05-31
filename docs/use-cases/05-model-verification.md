# Use Case 5 — Model Verification

**Actor:** Modeler
**Goal:** Validate a model's source code against the server without running a full simulation.
**Preconditions:** A LISMA text project or Blueprint statechart project is open and active.

## Main Flow

1. The user has a project open in the editor (LISMA text or Blueprint statechart).
2. The user clicks **Simulation > Verify** (or presses `Ctrl+F4`, or uses the toolbar Verify button).
3. If the active project is a Blueprint statechart, the `BlueprintToLismaConverter` first transforms the visual model into LISMA text.
4. The LISMA text is sent to the server for validation via the `GrpcLismaCompilerClient.ValidateAsync` gRPC call.
5. The server analyzes the source code and returns a list of errors/warnings.
6. The **Error List** DataGrid (bottom of the window) is cleared of previous results and populated with the new validation results:
   - **Row:** Line number where the issue occurs.
   - **Position:** Character position on the line.
   - **Fragment:** Code region name.
   - **Message:** Human-readable description of the issue.
7. The user reviews the errors in the Error List.
8. The user double-clicks (or navigates to) the reported line in the editor to locate and fix the issue.
9. The user may re-run Verify to check the fixes. Steps 4–7 repeat until the Error List is empty.

## Alternative Flows

- **5a. No errors:** The Error List is cleared and remains empty, indicating a valid model.
- **5b. Server unavailable:** If the ISMA server is not running or the gRPC connection fails, the user receives an error dialog. The Error List is not updated.
- **8a. Blueprint state editing:** If errors are reported in a state's LISMA body (opened via double-click on a blueprint state), the user edits the state's content in its dedicated text editor tab, not in the blueprint canvas.

## Postconditions

- The Error List reflects the current validation state of the active project.
- Previous validation results are cleared and replaced.
- The model is ready for simulation if no errors remain.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` | `Verify` command |
| `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` | `ValidateAsync` method |
| `src/ISMA.ViewModels/ViewModels/ErrorListViewModel.cs` | Error list display |
| `src/ISMA.Infrastructure/Server/GrpcLismaCompilerClient.cs` | gRPC validation call |
