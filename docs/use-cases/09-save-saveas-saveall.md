# Use Case 9 — Save, Save As, and Save All

**Actor:** Modeler
**Goal:** Persist project changes to disk using the appropriate save operation.
**Preconditions:** One or more projects are open in the editor.

## Main Flow — Save (Ctrl+S)

1. The user has an active project tab open with `IsDirty = true` (content has been modified).
2. The user clicks **File > Save** (or presses `Ctrl+S`, or clicks the toolbar Save button).
3. The application checks if the project has a `FilePath` (i.e., whether it was previously saved):
   - **If `FilePath` is set:** The project content is written to the existing file path, overwriting the previous content. `IsDirty` is reset to `false`. The red asterisk disappears from the tab title.
   - **If `FilePath` is null (new/unsaved project):** A "Save as" dialog opens (see Save As flow below).
4. For LISMA projects, the `FullText` content is written as plain text.
5. For Blueprint projects, the `BlueprintModel` is serialized to JSON.

## Main Flow — Save As (Ctrl+Shift+S)

1. The user clicks **File > Save As...** (or presses `Ctrl+Shift+S`).
2. A file picker dialog opens with file type filters:
   - `*.iscm2` for LISMA text projects.
   - `*.scisma` for Blueprint statechart projects.
   - `*.im` is not offered as a Save As target (legacy format, write-only).
3. The user enters or selects a filename and location, then confirms.
4. The application writes the project content to the chosen path.
5. The project's `FilePath` is updated to the new path. The tab title changes to the filename (without extension).
6. `IsDirty` is reset to `false`.

## Main Flow — Save All

1. The user clicks **File > Save All**.
2. The application iterates over all open projects in the `Projects` collection.
3. For each project:
   - If `IsDirty` is `false`, the project is skipped.
   - If `FilePath` is set, the project is saved in place (same as Save).
   - If `FilePath` is null, a "Save as" dialog opens for that project. The user must resolve it before Save All continues.
4. After all projects are saved, all `IsDirty` flags are reset to `false`.

## Alternative Flows

- **3a. Write permission denied:** If the user lacks write permissions to the target file, an error dialog is displayed. The save operation fails and `IsDirty` remains `true`.
- **3b. Disk full:** If the disk is full, the write operation fails. An error dialog is displayed.
- **3c. File locked:** If the target file is open in another application, the write fails. The user must close the file elsewhere or choose a different path.
- **8a. Save All with many unsaved projects:** Multiple "Save as" dialogs open sequentially, one per unsaved project. The user must resolve each before Save All completes.

## Postconditions

- **Save:** The project file on disk reflects the current editor content. `IsDirty = false`.
- **Save As:** A new file is created at the chosen path. The project is now tracked at the new location.
- **Save All:** All dirty projects are persisted. No dirty tabs remain.

## Related Files

| File | Role |
|------|------|
| `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` | `Save`, `SaveAs`, `SaveAll` commands |
| `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` | `SaveAsync`, `SaveAsAsync` |
| `src/ISMA.ViewModels/ViewModels/BlueprintProjectViewModel.cs` | `SaveAsync`, `SaveAsAsync` |
| `src/ISMA.App/Services/ProjectFileService.cs` | File dialog and I/O |
