# Use Case 2 — Create and Run a Blueprint Statechart Project

**Actor:** Modeler
**Goal:** Design a visual finite-state machine and execute the corresponding simulation.
**Preconditions:** ISMA UI is running.

## Main Flow

1. The user creates a new blueprint statechart by clicking **File > New Statechart** (or pressing `Ctrl+B`). A new tab titled "Untitled Blueprint" opens with the visual canvas editor.
2. The canvas initializes with two pre-created states:
   - **Main** state (green `#90EE90`, fixed name, top-left position)
   - **Init** state (blue `#ADD8E6`, fixed name, below Main)
3. The user builds the state machine:
   - **Adding states:** Clicks the **New state** button in the canvas toolbar, then clicks on the canvas to place a new user state (coral `#F08080`). The `NameChangingMonitor` auto-generates unique names (e.g., "State1", "State2").
   - **Repositioning states:** Drags state boxes to desired canvas positions (tracked via `CanvasPositionX` / `CanvasPositionY`).
   - **Renaming states:** Single-clicks a user state box (after 200ms disambiguation delay to distinguish from drag) to edit its name inline. Names must be unique across all states.
   - **Adding transitions:** Toggles the **New transition** button, then clicks a source state followed by a target state. A straight arrow is drawn between them.
   - **Editing transitions:** Single-clicks an arrowhead to open an **Edit Arrow PopOver** (floating card with drop shadow). The user enters an optional **Alias** (human-readable label) and a **Predicate** (transition condition/trigger).
   - **Adding loop transactions:** Uses the canvas toolbar to create self-loop arrows on states, then double-clicks the arrowhead to open a text editor tab for loop body content.
   - **Editing state content:** Double-clicks a state box to open a new text editor tab containing the LISMA body content for that state. Changes are tracked as `IsDirty` on the project.
   - **Removing elements:** Toggles **Remove state** or **Remove transition** and clicks the element to delete it. Removing a state also removes all its associated transitions.
4. The user configures simulation parameters in the Settings Panel (right sidebar), identical to the LISMA text project workflow.
5. The user runs the simulation (**Simulation > Run** / `Ctrl+F5`). At compile time, the `BlueprintToLismaConverter` automatically transforms the visual statechart into LISMA text format:
   - Regular transactions are grouped by target state and predicate into `state "key" { from startState; }` blocks.
   - Loop transactions are expanded into two pseudo-states.
6. The simulation executes through the same pipeline as a LISMA text project (compile → run → monitor → download).
7. Upon completion, results are available in the Tasks PopOver for visualization and export.

## Alternative Flows

- **3a. Complex state machine:** The user may create many states and transitions. The canvas has no scroll limits — all elements are rendered within the Avalonia `Canvas` panel. The user arranges states for readability.
- **3b. State content editing:** When double-clicking a state to edit its LISMA body, a new tab opens named after the state (e.g., "main", "State1"). This tab is a full LISMA text editor with server-driven syntax highlighting. Changes to the state content mark the blueprint project as `IsDirty`.
- **5a. Verify first:** The user may verify the blueprint before running. The converter runs first, then the resulting LISMA text is validated by the server.

## Postconditions

- The blueprint project's visual model (states, transitions, loop transactions) is serialized to JSON when saved.
- The simulation result is stored as a binary `.bin` file.
- The blueprint tab remains open with all edits intact.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/BlueprintProjectViewModel.cs` | Blueprint project lifecycle, conversion, save/load |
| `src/ISMA.ViewModels/ViewModels/BlueprintEditorViewModel.cs` | Canvas state machine (states, transitions, modes) |
| `src/ISMA.Domain/Conversion/BlueprintToLismaConverter.cs` | Visual-to-text transformation at compile time |
| `src/ISMA.App/Views/BlueprintEditorView.axaml` | Visual canvas with three render layers |
| `src/ISMA.App/Views/EditArrowPopOverView.axaml` | Arrow edit dialog |
