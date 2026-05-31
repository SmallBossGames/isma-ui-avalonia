# Use Case 13 — Configure Simulation Parameters

**Actor:** Modeler
**Goal:** Set up simulation parameters in the Settings Panel before running a simulation.
**Preconditions:** ISMA UI is running. At least one project is open (the Settings Panel is always visible).

## Main Flow

1. The user opens or creates a project (LISMA text or Blueprint statechart).
2. The user adjusts parameters in the **Settings Panel** (right sidebar, 240px wide). The panel contains five `PropertiesGrid` controls:

   **a. Cauchy Initials (Time Range):**
   - `Start Time` — when the simulation begins (default: 0.0)
   - `End Time` — when the simulation ends (default: 10.0)
   - `Initial Step` — the starting step size for integration (default: 0.1)

   **b. Integration Method:**
   - `Method` — selected from a ComboBox populated with algorithms provided by the server (e.g., Euler, Runge-Kutta variants)
   - `Accuracy` — checkbox enabling adaptive step-size control (adjusts step size dynamically to maintain precision)
   - `Accuracy` (numeric) — tolerance for adaptive integration (only editable when `Accuracy` checkbox is checked, default: 0.1)
   - `Stable` — checkbox enabling stability control to prevent numerical divergence
   - `Parallel` — checkbox enabling remote cluster execution
   - `Server` — network address for parallel execution (only editable when `Parallel` checkbox is checked)
   - `Port` — port number for parallel execution (only editable when `Parallel` checkbox is checked)

   **c. Event Detection (Zero-Crossing):**
   - `In use` — checkbox enabling detection of when variables cross zero (default: unchecked)
   - `Gamma` — sensitivity threshold for event detection (only editable when `In use` is checked, default: 0.8)
   - `Step limit` — checkbox constraining step size during event detection searches
   - `Low Border` — minimum step size during event detection (only editable when `Step limit` is checked, default: 0.001)

   **d. Result Saving:**
   - `Saving Target` — ComboBox with two options: `MEMORY` (results kept in memory only, not persisted to disk) or `FILE` (results saved to a binary cache file for later use)

   **e. Result Processing:**
   - `Simplify` — checkbox enabling client-side result simplification
   - `Method` — ComboBox selecting the simplification algorithm: `Radial-Distance` or `Douglas-Peucker` (only editable when `Simplify` is checked)
   - `Tolerance` — numeric field for the simplification tolerance (only editable when `Simplify` is checked)

3. Parameters are applied immediately — changes are reflected in the UI controls as the user adjusts them.
4. Parameters are **not sent to the server** until the user clicks **Run**. At that point, the `SimulationParametersViewModel.Snapshot()` method captures the current values and they are used for the simulation.

## Alternative Flows

### A1: No integration methods available

At step 2b, if the server was unavailable at startup and no methods were loaded, the integration method ComboBox is empty. The user must restart the application after the server becomes available.

### A2: Conditional field availability

Several fields are conditionally enabled/disabled based on checkbox state:
- `Accuracy` (numeric) is disabled when `Accuracy` checkbox is unchecked
- `Server` / `Port` fields are disabled when `Parallel` checkbox is unchecked
- `Gamma` is disabled when `In use` (Event Detection) checkbox is unchecked
- `Low Border` is disabled when `Step limit` checkbox is unchecked

Disabled fields cannot be edited. Their values are still captured at simulation time but are ignored by the server.

### A3: Corrupted parameter file

When loading (see Use Case 06), if the parameter file is malformed or contains unrecognized data, the load operation fails silently. No error message is shown to the user.

## Postconditions

- The Settings Panel reflects the configured parameter values.
- The next simulation run will use these parameter values (captured via `Snapshot()` at run time).
- Changes are not persisted to disk unless the user explicitly stores settings (see Use Case 06).

## Related Use Cases

- **UC-001** — Integration methods loaded from server at startup
- **UC-006** — Run Simulation (uses the configured parameters)
- **UC-007** — Visualize and Export Results (parameters captured in result metadata)
- **UC-014** — View Simulation Details (parameters shown in completed task details)
