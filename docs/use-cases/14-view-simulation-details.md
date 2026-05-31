# Use Case 14 — View Simulation Details

**Actor:** Modeler / Analyst
**Goal:** Review the parameters and metadata used for a completed simulation.
**Preconditions:** A simulation has completed successfully. The result is available in the Tasks PopOver > Completed section.

## Main Flow

1. The user has a completed simulation in the Tasks PopOver > Completed section (see Use Case 01 / Use Case 06).
2. The user clicks the **Details** chevron button (`...`) on the completed simulation row.
3. A **Details PopOver** (floating panel with drop shadow) appears near the row, showing:
   - **Model name** — the name of the project that was simulated.
   - **Cauchy Initials:**
     - `Start` — the start time used.
     - `End` — the end time used.
     - `Step` — the initial step size used.
   - **Integration Method:**
     - `Method` — the algorithm used (e.g., RK4, Euler).
     - `Accurate` — whether adaptive step-size control was enabled.
     - `Accuracy` — the tolerance value (if applicable).
     - `Stable` — whether stability control was enabled.
4. The user reviews the details to verify which parameters were used for the simulation.
5. The user moves the mouse away or clicks elsewhere, and the Details PopOver closes automatically.

## Alternative Flows

- **3a. Additional metadata:** Future versions may include simulation timing data (start time, end time, total simulation time) in the Details PopOver.

## Postconditions

- The Details PopOver is displayed with simulation metadata.
- No state is changed — this is a read-only view.

## Related Use Cases

- **UC-001** — Run Simulation (prerequisite: a completed simulation must exist)
- **UC-007** — Visualize and Export Results (Details is shown before Show or Export)
- **UC-013** — Configure Simulation Parameters (the details reflect the configured parameters)
