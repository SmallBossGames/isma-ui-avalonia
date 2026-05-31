# Use Case 3 — Open and Edit Existing Projects

**Actor:** Modeler
**Goal:** Load existing project files, make modifications, and persist changes.
**Preconditions:** ISMA UI is running. Project files exist on disk (`.iscm2`, `.scisma`, or `.im`).

## Main Flow

1. The user clicks **File > Open...** (or presses `Ctrl+O`).
2. A file picker dialog opens with filters for `*.iscm2`, `*.scisma`, and `*.im` files.
3. The user selects one or more files (multi-select is supported) and confirms.
4. Each selected file is opened as a new tab in the main editor area:
   - **`.iscm2`** files → LISMA text editor (`IsmaTextEditorView` with AvaloniaEdit)
   - **`.scisma`** files → Blueprint canvas editor (`BlueprintEditorView` with visual statechart)
   - **`.im`** files → LISMA text editor (legacy format, backward compatibility)
5. Each tab displays the project name (derived from the filename) and loads the file content:
   - For LISMA projects: the raw text content is loaded into `FullText` property.
   - For blueprint projects: the JSON model is deserialized into `BlueprintModel` (states, transactions, loop transactions) and rendered on the canvas.
6. The user edits the project content:
   - LISMA projects: types directly in the AvaloniaEdit editor with line numbers and server-driven syntax highlighting.
   - Blueprint projects: manipulates states and transitions via the canvas toolbar and interaction modes.
7. As the user edits, each project's `IsDirty` flag is set to `true`. A red asterisk (`*`) appears next to the tab title.
8. The user saves changes:
   - **If the project was previously saved:** Clicks **File > Save** (`Ctrl+S`) or the toolbar Save button. The file is overwritten in place. `IsDirty` is reset to `false`.
   - **If the project is new (unsaved):** Clicks **File > Save** (`Ctrl+S`). A "Save as" dialog opens. The user chooses a filename and location. The file is written, the tab title updates to the filename, and `IsDirty` is reset.
   - Alternatively, the user clicks **File > Save As...** (`Ctrl+Shift+S`) to explicitly choose a new filename.
9. The user closes a project by clicking the **X** button on the tab or via **File > Close**. The project is removed from the tab collection and disposed.

## Alternative Flows

- **8a. Close without saving:** If the user closes a dirty project, the application should prompt for save (behavior depends on `MainWindowViewModel.CloseTab` implementation).
- **8b. File not found:** If a previously opened file is deleted or moved on disk, subsequent save operations will fail. The "Save as" dialog should be triggered.
- **4a. Large number of files:** Opening many files creates many tabs. The `TabControl` displays them horizontally. The user switches between tabs by clicking.

## Postconditions

- All opened projects are visible in the tab bar.
- The most recently opened project becomes the `ActiveProject`.
- File paths are recorded in `~/.isma/preferences.json` (up to 5 last-opened files).

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/Services/ProjectService.cs` | Open, save, close lifecycle |
| `src/ISMA.App/Services/ProjectFileService.cs` | File dialog and I/O operations |
| `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` | LISMA project save/load |
| `src/ISMA.ViewModels/ViewModels/BlueprintProjectViewModel.cs` | Blueprint project save/load |
| `src/ISMA.App/Views/EditorTabPaneView.axaml` | Tab control with data templates |
