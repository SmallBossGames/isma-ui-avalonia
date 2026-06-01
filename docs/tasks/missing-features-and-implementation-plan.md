# ISMA UI Avalonia — Gap Analysis & Implementation Plan

This document identifies what's missing or incomplete from the original ISMA JavaFX application that hasn't been fully implemented in the Avalonia migration, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

The original ISMA JavaFX application has **23 business features** (per `06-ux-reference.md` Feature Matrix). The Avalonia migration has implemented the core architecture and most features, but several UI/UX details are incomplete or incorrect compared to the original specification.

| Status | Count | Details |
|--------|-------|---------|
| **Core Architecture** | ✅ Complete | All 5 layers (Domain, Infrastructure, ViewModels, App, Tests), DI, MVVM, gRPC |
| **Domain Models** | ✅ Complete | All models, DTOs, contracts, BlueprintToLismaConverter, ResultSimplifier |
| **Infrastructure** | ✅ Complete | Server communication, Unix sockets, binary file reader, Grin launcher, preferences |
| **ViewModels** | ✅ Complete | All ViewModels, services, converters, NameChangingMonitor |
| **UI Shell** | ✅ Complete | MainWindow, MenuBar, ToolBar, SettingsPanel, ErrorList, ProcessBar |
| **Text Editor** | ✅ Complete | AvaloniaEdit with line numbers, server-driven syntax highlighting |
| **Blueprint Editor** | ✅ Complete | Canvas rendering, drag, states, arrows, loops, modes, inline editing |
| **Simulation Flow** | ✅ Complete | Compile → Run → Monitor → Download → Commit pipeline |
| **Tasks PopOver** | ✅ Complete | In-progress + Completed sections with Details PopOver |
| **File Operations** | ✅ Complete | Open/Save/SaveAs/SaveAll with FileDialog |
| **Tab Close Buttons** | ✅ Complete | X button on each tab, closes specific project |
| **Keyboard Shortcuts** | ⚠️ Partial | Menu bar bindings work, but not global (won't fire when text editor has focus) |
| **Tests** | ⚠️ Partial | Unit tests for Domain/ViewModels, integration tests for some flows — need more UI coverage |
| **Missing/Incomplete** | **5 items** | See details below |

---

## Gap Analysis: Missing or Incomplete Features

### Gap 1: Main/Init State Height Incorrect

**Original spec** (`07-blueprint-editor-ux.md` §3, §17-18):
- User states: `Width=110`, `Height=65`, `arcWidth=20`, `arcHeight=20`, fill=`#F08080` (CORAL)
- Main state: `Width=110`, `Height=60`, fill=`#90EE90` (LIGHTGREEN)
- Init state: `Width=110`, `Height=60`, fill=`#ADD8E6` (LIGHTBLUE)

**Current implementation** (`BlueprintEditorView.axaml:85-92`, `BlueprintEditorView.axaml.cs:24-25`, `ArrowLine.cs:19-20`, `LoopArrow.cs:19-20`):
- All states: `Width="110"`, `Height="65"`, `CornerRadius="10"`
- Constants: `StateWidth = 110.0`, `StateHeight = 65.0` (used in ArrowLine and LoopArrow hit testing)

**Impact:** Main/Init states appear 5px taller than the original spec. Arrow geometry calculations use 65px for all states, which is correct for user states but slightly off for Main/Init.

**Priority:** MEDIUM — affects visual fidelity to original spec

---

### Gap 2: Main/Init Labels on Canvas (Extra UI Element)

**Original spec** (`07-blueprint-editor-ux.md` §3.1):
- "State boxes show only the state name, centered"
- No labels or indicators on the canvas

**Current implementation** (`BlueprintEditorView.axaml:106-121`):
- Two `StackPanel` elements with "Main" (green) and "Init" (blue) labels
- `IsVisible="{Binding IsMain}"` and `IsVisible="{Binding IsInit}"`
- These labels are NOT in the original spec

**Impact:** Canvas shows extra "Main"/"Init" labels that weren't in the original design. This clutters the canvas view.

**Priority:** LOW — cosmetic difference, removes visual clutter to match original

---

### Gap 3: Arrow Hit Testing Logic Duplicated Between Controls and Code-Behind

**Original spec** (`07-blueprint-editor-ux.md` §4.5, §5.4):
- Arrow body click in remove mode → remove arrow
- Arrowhead single-click → open EditArrowPopOver
- Loop body click in remove mode → remove loop
- Loop arrowhead single-click → open EditArrowPopOver

**Current implementation:**
- `ArrowLine.cs:129-132` — `OnPointerPressed` is empty (just calls `base.OnPointerPressed(e)`)
- `LoopArrow.cs:112-115` — `OnPointerPressed` is empty
- `BlueprintEditorView.axaml.cs:210-268` — Hit testing logic is entirely in code-behind:
  - `OnArrowPointerPressed` recalculates arrowhead distance, body distance
  - `OnLoopArrowPointerPressed` recalculates circle distance, arrowhead distance
  - Both duplicate the geometry calculations already in the Render methods

**Impact:** Hit testing logic is duplicated between the control's Render method and the code-behind. This is fragile — if dimensions change, both places must be updated. The controls themselves don't participate in hit testing.

**Priority:** MEDIUM — code quality, maintainability, and testability

---

### Gap 4: Keyboard Shortcuts Not Global

**Original spec** (`06-ux-reference.md` §Keyboard Shortcuts Reference):
- All shortcuts work globally: `Ctrl+N`, `Ctrl+B`, `Ctrl+O`, `Ctrl+S`, `Ctrl+W`, `Ctrl+X`, `Ctrl+C`, `Ctrl+V`, `Ctrl+F4`, `Ctrl+F5`

**Current implementation** (`IsmaMenuBarView.axaml:8-20`):
- `Menu.KeyBindings` — KeyBindings scoped to the `Menu` element only
- When focus is on the text editor, `TextEditor` consumes Ctrl+S, Ctrl+X, Ctrl+C, Ctrl+V
- `Ctrl+W` (Exit) and `Ctrl+F4`/`Ctrl+F5` (Verify/Run) won't fire when focus is on editor

**Impact:** Keyboard shortcuts only work when focus is on the menu bar. In the original app, shortcuts work globally regardless of focus.

**Priority:** HIGH — breaks muscle memory and workflow from original app

---

### Gap 5: Integration Test Coverage Gaps

**What's tested:**
- Blueprint editor VM operations (add/remove states, transitions, modes)
- Simulation workflow (full pipeline with mocked server)
- Syntax highlighting (fallback + server-driven)
- CSV export
- Settings panel UI
- Project management (create, open, save, close)
- Validation
- Tab close operations (VM-level)

**What's NOT tested (per original feature matrix):**
- State drag-and-drop with arrow reconnection (UI-level)
- Inline name editing with 200ms timer disambiguation (UI-level)
- Arrow body vs head click detection (UI-level)
- Loop arrow double-click → text editor tab (UI-level)
- Edit Arrow PopOver open/close/binding (UI-level)
- Blueprint-to-LISMA conversion with visual canvas state (UI-level)
- Window geometry save/restore
- Global keyboard shortcut execution (Ctrl+F5 to run simulation)
- Toggle button label changes (UI-level)

**Priority:** HIGH — critical interactions lack UI-level test coverage

---

## Previously Completed Items (for reference)

The following items were identified as gaps in the initial analysis but have since been implemented:

| Item | Status | Implementation |
|------|--------|---------------|
| Tab close buttons | ✅ Done | `EditorTabPaneView.axaml:16-29` — X button with `CloseTabCommand` |
| Keyboard shortcut Ctrl+W=Exit | ✅ Done | `IsmaMenuBarView.axaml:14` — `ExitCommand` with `Ctrl+W` |
| Toggle button dynamic labels | ✅ Done | `BlueprintEditorViewModel.cs:62-82,450-496` — computed properties |
| Tasks PopOver Details PopOver | ✅ Done | `TasksPopOverView.axaml:110-176` — chevron button with Flyout |
| State dimensions (user) | ✅ Done | `BlueprintEditorView.axaml:85-87` — `Width="110" Height="65"` |
| Arrow/Loop constants | ✅ Done | `ArrowLine.cs:19-20`, `LoopArrow.cs:19-20` — `StateWidth=110, StateHeight=65` |
| State content preview removal | ⚠️ See Gap 2 | "Main"/"Init" labels still present, should be removed |

---

## Implementation Plans

### Task 1: Fix Main/Init State Height to 60px

**Files to modify:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml` — Main/Init state height
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — `StateHeight` constant for Main/Init
- `src/ISMA.App/Controls/ArrowLine.cs` — differentiated state height in center calculation
- `src/ISMA.App/Controls/LoopArrow.cs` — differentiated state height in center calculation

**Implementation:**
1. The AXAML template currently uses a single `Height="65"` for all states. We need to make Main/Init states 60px tall.
2. Options:
   a. Use a `DataTrigger` or `MultiBinding` to set height based on `IsMain`/`IsInit`
   b. Add `StateHeight` property to `BlueprintStateViewModel` (60 for Main/Init, 65 for user)
   c. Use a converter in the binding
3. The cleanest approach: Add `StateHeight` (double) to `BlueprintStateViewModel`, set 60 for Main/Init, 65 for user. Bind `Height="{Binding StateHeight}"` in AXAML.
4. Update `ArrowLine.GetCenter()` and `LoopArrow.GetCenter()` to use the state's actual height (via the view model property).

**Acceptance Checklist:**
- [ ] Main state renders at 110×60px
- [ ] Init state renders at 110×60px
- [ ] User states render at 110×65px
- [ ] Arrow lines connect to correct state centers (accounting for different heights)
- [ ] Loop arrows are centered on states correctly
- [ ] `BlueprintEditorTests_MainInitStateHeight_Is60px` passes
- [ ] `BlueprintEditorTests_UserStateHeight_Is65px` passes

---

### Task 2: Remove Main/Init Labels from Canvas

**Files to modify:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml` — remove lines 106-121

**Implementation:**
1. Remove the two `StackPanel` elements (lines 106-121) that show "Main" and "Init" labels
2. Keep only the state name `TextBlock` (lines 100-105)

**Acceptance Checklist:**
- [ ] State boxes show only the name on the canvas
- [ ] No "Main"/"Init" labels visible
- [ ] Double-click still opens text editor tab
- [ ] `BlueprintEditorTests_NoMainInitLabelsOnCanvas` passes

---

### Task 3: Consolidate Arrow Hit Testing into Controls

**Files to modify:**
- `src/ISMA.App/Controls/ArrowLine.cs` — implement `OnPointerPressed` with hit test
- `src/ISMA.App/Controls/LoopArrow.cs` — implement `OnPointerPressed` with hit test
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — remove duplicate hit test logic

**Implementation:**
1. Add `PointerPressedRoutedEvent` to both `ArrowLine` and `LoopArrow`
2. Define custom routed event args with hit test info:
   - `IsArrowHead` (bool) — true if click was on arrowhead
   - `Position` (Point) — click position in control coordinates
3. In `OnPointerPressed`, compute distance to arrowhead using the same constants as `Render()`:
   - For `ArrowLine`: distance from click to arrowhead center < `ArrowheadSize` → head click
   - For `LoopArrow`: distance from click to circle edge < 8px → body click; distance to arrowhead < `ArrowheadSize` → head click
4. Raise the routed event with hit test info
5. In `BlueprintEditorView.axaml`, subscribe to the routed events via `EventSetter` or code-behind
6. The code-behind handlers receive the event args and act on the hit test results

**Acceptance Checklist:**
- [ ] ArrowLine correctly identifies arrowhead clicks vs body clicks
- [ ] LoopArrow correctly identifies arrowhead clicks vs body clicks
- [ ] Routed events propagate to parent handlers in BlueprintEditorView
- [ ] No duplicate hit test logic between controls and code-behind
- [ ] `ArrowHitTestTests_ArrowBodyClick_Detected` passes
- [ ] `ArrowHitTestTests_ArrowHeadClick_Detected` passes
- [ ] `ArrowHitTestTests_LoopBodyClick_Detected` passes
- [ ] `ArrowHitTestTests_LoopHeadClick_Detected` passes

---

### Task 4: Make Keyboard Shortcuts Global

**Files to modify:**
- `src/ISMA.App/MainWindow.axaml` — add `Window.InputBindings`
- `src/ISMA.App/Views/IsmaMenuBarView.axaml` — remove or keep as duplicate (for menu item display)

**Implementation:**
1. Add `InputBindings` to `MainWindow.axaml` for all global shortcuts:
   - `Ctrl+N` → `NewTextCommand`
   - `Ctrl+B` → `NewBlueprintCommand`
   - `Ctrl+O` → `OpenCommand`
   - `Ctrl+S` → `SaveCommand`
   - `Ctrl+Shift+S` → `SaveAsCommand`
   - `Ctrl+W` → `ExitCommand`
   - `Ctrl+X` → `CutCommand`
   - `Ctrl+C` → `CopyCommand`
   - `Ctrl+V` → `PasteCommand`
   - `Ctrl+F4` → `VerifyCommand`
   - `Ctrl+F5` → `RunCommand`
2. Keep the `Menu.KeyBindings` in `IsmaMenuBarView` for visual display in menu items (Avalonia shows the gesture next to menu items)
3. Use `CommandTarget` binding if needed to route commands to the correct target

**Acceptance Checklist:**
- [ ] `Ctrl+S` saves when focus is on text editor
- [ ] `Ctrl+F5` runs simulation when focus is on text editor
- [ ] `Ctrl+W` exits app when focus is on text editor
- [ ] `Ctrl+C`/`Ctrl+V`/`Ctrl+X` still work in text editor (AvaloniaEdit handles these natively)
- [ ] Menu items still show the keyboard shortcuts
- [ ] `KeyboardShortcutsTests_GlobalShortcutsWorkWhenEditorHasFocus` passes

---

### Task 5: Write Integration Tests for Missing UI Interactions

**Files to create:**
- `tests/ISMA.Tests.Integration/BlueprintCanvasUiTests.cs` — UI-level blueprint editor tests
- `tests/ISMA.Tests.Integration/ArrowHitTestUiTests.cs` — arrow click detection tests
- `tests/ISMA.Tests.Integration/KeyboardShortcutsUiTests.cs` — global keyboard shortcut tests
- `tests/ISMA.Tests.Integration/InlineNameEditUiTests.cs` — inline name editing tests

**Implementation:**
1. Follow existing `IntegrationTestBase` pattern (headless Avalonia with mocked server)
2. Use `UiHelpers` for UI interactions
3. Test through actual UI control tree, not just ViewModels
4. Use `window.Flush()` to force layout updates in headless mode
5. Use `AutomationId` properties for reliable control identification

**Test scenarios:**
- Drag state → verify position update + arrow reconnection
- Single-click state (200ms) → inline name editor appears → commit → verify name change
- Click arrowhead → EditArrowPopOver opens → change alias/predicate → verify binding
- Click arrow body in remove mode → arrow removed
- Double-click loop arrowhead → new text editor tab opens with "(loop)" suffix
- Ctrl+F5 → simulation starts (mock server) → progress bar updates
- Toggle button → label changes from "Add Transition" to "Stop adding transaction"

**Acceptance Checklist:**
- [ ] `BlueprintCanvasUiTests_StateDrag_RepositionsStateAndUpdatesArrows` passes
- [ ] `BlueprintCanvasUiTests_TransitionCreation_TwoClicks_CreatesArrow` passes
- [ ] `BlueprintCanvasUiTests_LoopCreation_SameStateTwice_CreatesLoop` passes
- [ ] `ArrowHitTestUiTests_ArrowHeadClick_OpensPopOver` passes
- [ ] `ArrowHitTestUiTests_ArrowBodyClickInRemoveMode_RemovesArrow` passes
- [ ] `InlineNameEditUiTests_SingleClick_RenamesState` passes
- [ ] `InlineNameEditUiTests_DragDisambiguation_SkipsNameEdit` passes
- [ ] `KeyboardShortcutsUiTests_CtrlF5_RunsSimulation` passes
- [ ] `KeyboardShortcutsUiTests_CtrlS_SavesProject` passes
- [ ] `ToggleLabelUiTests_ButtonLabelChangesWithMode` passes
- [ ] All new tests pass: `dotnet test ISMA.Tests.Integration` 100% green

---

## Implementation Priority Order

| # | Task | Priority | Effort | Depends On |
|---|------|----------|--------|------------|
| 1 | Fix Main/Init state height | MEDIUM | 1h | — |
| 2 | Remove Main/Init labels | LOW | 15m | — |
| 3 | Consolidate arrow hit testing | MEDIUM | 2h | Task 1 |
| 4 | Global keyboard shortcuts | HIGH | 1h | — |
| 5 | Write integration tests | HIGH | 4h | Tasks 1-4 |

**Total estimated effort: ~9 hours**

---

## Testing Strategy

All new features and fixes should be covered with:

1. **Unit tests** (`ISMA.Tests`) — ViewModel logic, domain logic, service logic
2. **Integration tests** (`ISMA.Tests.Integration`) — End-to-end business flows with real UI components
3. **Headless Avalonia tests** — Using `Avalonia.Headless.XUnit` for UI interaction testing

### Integration test patterns to follow:

- Use `IntegrationTestBase` for headless Avalonia setup with mocked server
- Use `UiHelpers` for UI interactions (click buttons, set text, find controls)
- Test through the actual UI control tree, not just ViewModels
- Use `window.Flush()` to force layout updates in headless mode
- Use `AutomationId` properties for reliable control identification
- Prefer testing through real UI components over ViewModel-only tests
- Each test should be self-contained and independent
