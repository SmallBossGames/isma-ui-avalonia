# ISMA UI Avalonia — Parity Report

Final parity status of the C#/Avalonia rewrite against the original Kotlin/JavaFX application (`/home/agarder/IdeaProjects/isma/isma-ui`). All planned phases are complete; the gap list from the previous plan (Main/Init state height, canvas labels, arrow hit testing, global shortcuts, test coverage) is fully resolved.

## Verification

```bash
dotnet build isma-ui-dotnet.slnx   # Ошибок: 0
dotnet test                        # 446 passed (261 unit + 185 integration), 0 failed
```

| Suite | Project | Count |
|-------|---------|-------|
| Unit | `tests/ISMA.Tests` | 261 |
| Integration (headless Avalonia) | `tests/ISMA.Tests.Integration` | 185 |

## Parity Matrix

| Area | Status | Key files |
|------|--------|-----------|
| Window shell | ✅ Title `ISMA 22`, icon `Assets/isma-2016-title.png`, min 600×500 | `src/ISMA.App/MainWindow.axaml` |
| Menu / toolbar | ✅ Original labels, inline Material `PathIcon` data (20×20) | `src/ISMA.App/Views/IsmaMenuBarView.axaml`, `IsmaToolBarView.axaml` |
| Text editor | ✅ AvaloniaEdit, line numbers, server-driven highlighting | `src/ISMA.App/Views/IsmaTextEditorView.axaml` |
| Syntax colors | ✅ Keyword orange+bold, comment gray+italic, number blue, default black | `src/ISMA.App/Services/ServerDrivenHighlightingTransformer.cs`, `src/ISMA.App/Assets/LISMA.xshd` |
| Blueprint editor | ✅ States 110×65 (user) / 110×60 (main/init), drag, arrows, loops, inline editing | `src/ISMA.App/Views/BlueprintEditorView.axaml`, `src/ISMA.App/Controls/{StateBox,ArrowLine,LoopArrow}.cs` |
| Blueprint serialization | ✅ Original `.iscm2` schema; internal Guid model mapped at the boundary | `src/ISMA.Domain/` converter |
| Settings panel | ✅ 4 sections: Initials, Integration, Event detection, Result saving | `src/ISMA.App/Views/SettingsPanelView.axaml`, `src/ISMA.App/Controls/PropertiesGrid.axaml.cs` |
| Simulation flow | ✅ Compile → Run → Monitor → Download → Commit | `src/ISMA.ViewModels/Services/SimulationService.cs` |
| Process bar | ✅ Run button with `Play` tooltip | `src/ISMA.App/Views/SimulationProcessBarView.axaml` |
| Tasks popover | ✅ In progress / Completed / Failed sections | `src/ISMA.App/Views/TasksPopOverView.axaml` |
| Error list | ✅ Collapsed `Expander`, `DataGrid` maxHeight 200, columns 5/5/10/80 | `src/ISMA.App/MainWindow.axaml` |
| File operations | ✅ Open / Save / SaveAs / SaveAll, naming `New project` / `New statechart` | `src/ISMA.ViewModels/Services/ProjectService.cs` |
| Keyboard shortcuts | ✅ Window-level, work with editor focus | `src/ISMA.App/MainWindow.axaml` (`Window.KeyBindings`) |

### Keyboard shortcuts

Declared once at window level (`MainWindow.axaml:12-24`) so they fire regardless of focus; `InputGesture` on the menu items provides the visual hints.

| Gesture | Command |
|---------|---------|
| `Ctrl+N` / `Ctrl+B` | New text / New statechart |
| `Ctrl+O` | Open |
| `Ctrl+S` / `Ctrl+Shift+S` | Save / Save as |
| `Ctrl+W` | Exit |
| `Ctrl+X` / `Ctrl+C` / `Ctrl+V` | Cut / Copy / Paste |
| `Ctrl+F4` / `Ctrl+F5` | Verify / Run |

## Approved Deviations

Behavioral differences from the original, accepted during the rewrite.

| ID | Original | Avalonia | Rationale |
|----|----------|----------|-----------|
| D1 | Cancel sent client-side task id | Cancel sends the server `simulationId` | Matches the server API contract |
| D2 | Preferences in JavaFX `Preferences` | `%AppData%/isma/preferences.json` | Platform-standard file location |
| D3 | Highlighting applied synchronously | Applied asynchronously | Keeps the UI responsive on large files |
| D4 | Paste = `Ctrl+P` | Paste = `Ctrl+V` | `Ctrl+V` is the standard paste gesture |
| D5 | Aborted task kept in In Progress | Aborted task removed from In Progress | Aborted work is not in progress |
| D6 | Event detection parameters not always sent | Sent whenever event detection is enabled | Complete parameter snapshot per run |
| D7 | CSV AE-rhs read as text | Read as binary | Correctness of the AE-rhs column |

## Removed Superset Features

Features present in the intermediate Avalonia implementation but absent from the original were stripped to reach exact parity:

- Undo / redo (blueprint editor)
- Copy / paste (blueprint editor)
- Grid snap
- Multi-select (blueprint editor)
- Dirty markers (tabs)
- `ResultProcessing` settings section (and its domain model `ResultProcessingParameters`)
- Find & replace (text editor)
- Highlighting status badge (editor)
- Auto-save

## Resolved Gaps (from the previous plan)

| Gap | Resolution |
|-----|------------|
| Main/Init state height 65px | `StateHeight` property on the state view model — 60 for Main/Init, 65 for user states; bound in `BlueprintEditorView.axaml`, used by `ArrowLine`/`LoopArrow` center math |
| "Main"/"Init" labels on canvas | Removed; state boxes show only the state name |
| Duplicated arrow hit testing | Hit testing lives in `ArrowLine.OnPointerPressed` / `LoopArrow.OnPointerPressed` and raises `ArrowHitTestEventArgs` (`ArrowHeadClicked`, `ArrowBodyClicked`, `LoopBodyClicked`, `LoopArrowDoubleClick`); `BlueprintEditorView.axaml.cs` only dispatches on the events |
| Shortcuts scoped to the menu | Moved to `Window.KeyBindings` — global regardless of focus |
| UI-level test coverage | 185 integration tests across project management, blueprint authoring (incl. loop arrows), settings panel, simulation workflow, CSV export, syntax highlighting, validation, keyboard shortcuts, startup, and window components |

## Remaining

- **Live-server protocol verification** — requires a running `isma-server`; all flows are verified against `MockSimulationServerFacade` in the integration suite.
