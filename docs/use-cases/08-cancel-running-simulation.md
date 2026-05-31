# Use Case 8 — Cancel a Running Simulation

**Actor:** Modeler
**Goal:** Abort a simulation that is currently executing on the server.
**Preconditions:** A simulation is running (visible in Tasks PopOver > In Progress).

## Main Flow

1. The user initiates a simulation (see Use Case 1, step 5).
2. The simulation starts executing on the ISMA server. The status text updates to *"Running simulation..."* and then *"Monitoring simulation..."*.
3. A progress bar appears in the **Tasks PopOver > In Progress** section, showing the simulation's current progress (0–100%).
4. The simulation row displays:
   - **Model name:** The name of the project being simulated.
   - **Progress bar:** Visual indicator of completion percentage.
   - **Abort button:** A button to cancel the simulation.
5. The user decides to cancel the simulation (e.g., the model is taking too long, or the user realized the parameters are incorrect).
6. The user clicks the **Abort** button on the simulation row.
7. The application sends a cancellation request to the server via the `GrpcSimulationClient.CancelSimulationAsync` gRPC call.
8. The server stops the simulation and releases resources.
9. The simulation row is removed from the **In Progress** section of the Tasks PopOver.
10. The status text resets to idle.

## Alternative Flows

- **6a. Multiple simulations:** If the user has initiated multiple simulations (e.g., parameter sweeps — though this requires external orchestration as the app supports only one active Run command at a time), only the targeted simulation is aborted.
- **7a. Server unreachable:** If the gRPC channel to the server is broken (server crashed, network issue), the abort request fails silently. The simulation row remains in In Progress until the server terminates it naturally or the connection is detected as dead.

## Postconditions

- The simulation is terminated on the server.
- The simulation row is removed from the Tasks PopOver.
- No result file is produced for the cancelled simulation.
- The user can modify parameters and re-run.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/SimulationService.cs` | `StopSimulationAsync` method |
| `src/ISMA.ViewModels/ViewModels/InProgressSimulationViewModel.cs` | `Abort` command and progress tracking |
| `src/ISMA.ViewModels/ViewModels/TasksPopOverViewModel.cs` | In Progress collection management |
| `src/ISMA.Infrastructure/Server/GrpcSimulationClient.cs` | `CancelSimulation` gRPC call |
| `src/ISMA.App/Views/TasksPopOverView.axaml` | Abort button UI |
