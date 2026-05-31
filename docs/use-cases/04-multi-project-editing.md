# Use Case 4 — Multi-Project Editing

**Actor:** Modeler
**Goal:** Work with multiple projects simultaneously, switching between them and managing their lifecycle.
**Preconditions:** ISMA UI is running with at least one project open.

## Main Flow

1. The user has one or more projects open in tabs (see Use Case 3 for opening projects).
2. The user creates additional projects:
   - **File > New Text** (`Ctrl+N`) for a new LISMA text project.
   - **File > New Statechart** (`Ctrl+B`) for a new blueprint project.
   - **File > Open...** (`Ctrl+O`) to load existing files.
3. Each project appears as a tab in the central editor area. The tabs are displayed in the order they were opened.
4. The user switches between projects by clicking on tabs. The clicked tab becomes the `ActiveProject`.
5. The active project's content is displayed in the editor area:
   - LISMA projects show the AvaloniaEdit text editor.
   - Blueprint projects show the visual statechart canvas.
6. The user can edit multiple projects in parallel:
   - Each project maintains its own `FullText` (LISMA) or `BlueprintModel` (statechart).
   - Each project tracks its own `IsDirty` state independently.
   - Changes to one project do not affect others.
7. The user may open a state's LISMA body from a blueprint project:
   - Double-clicking a state in the blueprint canvas opens a new tab named after the state (e.g., "main", "State1").
   - This is a full LISMA text editor tab. It is separate from the blueprint project but linked conceptually.
8. The user can save all open projects at once:
   - **File > Save All** saves every dirty project. Projects that were previously saved are overwritten; unsaved projects prompt for a filename.
9. The user can close individual tabs by clicking the **X** button on each tab, or close all at once via **File > Close All**.

## Alternative Flows

- **8a. Save All with unsaved projects:** If any project has never been saved, "Save as" dialogs open sequentially for each unsaved project.
- **8b. Switching during simulation:** A simulation can run in the background while the user switches between tabs. The Tasks PopOver shows progress regardless of which project is active.

## Postconditions

- All opened projects remain in the tab collection.
- The last-active project is remembered and becomes active on next session start (via last-opened files).
- Tab order is preserved across save/load cycles.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` | `Projects` collection, `ActiveProject`, tab management commands |
| `src/ISMA.ViewModels/Services/ProjectService.cs` | Multi-project lifecycle management |
| `src/ISMA.App/Views/EditorTabPaneView.axaml` | Tab control with `DataTemplate` selection |
