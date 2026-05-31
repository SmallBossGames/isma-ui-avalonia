# ISMA UI Avalonia — Gap Analysis & Implementation Plan

This document identifies what's missing or incomplete from the original ISMA JavaFX application that hasn't been fully implemented in the Avalonia migration, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

The original ISMA JavaFX application has **31 business features** (per `06-ux-reference.md` Feature Matrix). The Avalonia migration has implemented the core architecture and most features, but several UI/UX details are incomplete or incorrect compared to the original specification.

| Status | Count | Details |
|--------|-------|---------|
| **Core Architecture** | ✅ Complete | All 5 layers (Domain, Infrastructure, ViewModels, App, Tests), DI, MVVM, gRPC |
| **Domain Models** | ✅ Complete | All models, DTOs, contracts, BlueprintToLismaConverter, ResultSimplifier |
| **Infrastructure** | ✅ Complete | Server communication, Unix sockets, binary file reader, Grin launcher, preferences |
| **ViewModels** | ✅ Complete | All ViewModels, services, converters, NameChangingMonitor |
| **UI Shell** | ✅ Complete | MainWindow, MenuBar, ToolBar, SettingsPanel, ErrorList, ProcessBar |
| **Text Editor** | ✅ Complete | AvaloniaEdit with line numbers, server-driven syntax highlighting |
| **Blueprint Editor** | ⚠️ Partial | Canvas rendering, drag, states, arrows, loops, modes — but visual specs incorrect |
| **Simulation Flow** | ✅ Complete | Compile → Run → Monitor → Download → Commit pipeline |
| **Tasks PopOver** | ⚠️ Partial | In-progress + Completed sections work, but missing Details PopOver |
| **File Operations** | ✅ Complete | Open/Save/SaveAs/SaveAll with FileDialog |
| **Tests** | ⚠️ Partial | Unit tests for Domain/ViewModels, integration tests for some flows — need more UI coverage |
| **Missing/Incomplete** | **8 items** | See details below |

---

## Gap Analysis: Missing or Incomplete Features

### Gap 1: Blueprint Editor — State Box Dimensions and Colors Incorrect

**Original spec** (`07-blueprint-editor-ux.md` §3, §17-18):
- User states: `Width=110`, `Height=65`, `arcWidth=20`, `arcHeight=20`, fill=`#F08080` (CORAL)
- Main/Init states: `Width=110`, `Height=60`, fill=`#90EE90` (LIGHTGREEN) / `#ADD8E6` (LIGHTBLUE)

**Current implementation** (`BlueprintEditorView.axaml:85-96`, `BlueprintEditorView.axaml.cs:24-25`, `ArrowLine.cs:19-20`, `LoopArrow.cs:19-20`):
- States: `Width="120"`, `Height="60"`, `CornerRadius="8"` (not 20px arc)
- Constants: `StateWidth = 120.0`, `StateHeight = 60.0` (used in ArrowLine and LoopArrow hit testing)
- Fill colors: Set from ViewModel hex strings, but hardcoded dimensions are wrong

**Impact:** States appear too wide and too short. Corner rounding is too subtle. Arrow geometry calculations are off by 10px in width and 5px in height, causing arrows to connect to wrong positions.

**Priority:** HIGH — affects all blueprint editing interactions

---

### Gap 2: Blueprint Editor — Tab Close Buttons Missing

**Original spec** (`06-ux-reference.md` §Editor Area — Tab-based Project Management):
- "Tab close: Clicking the close button (X) on a tab closes that project"

**Current implementation** (`EditorTabPaneView.axaml:8-27`):
- `TabControl` with `ItemsSource="{Binding Projects}"` and `SelectedItem="{Binding ActiveProject}"`
- Tab header shows name + dirty indicator asterisk
- **No close button (X) on individual tabs**
- Tab closing is only available via File → Close menu or Ctrl+W

**Impact:** Users cannot close tabs directly from the tab bar, which is the standard UX pattern for tabbed interfaces.

**Priority:** HIGH — core UX pattern missing

---

### Gap 3: Keyboard Shortcuts Mismatch with Original Spec

**Original spec** (`06-ux-reference.md` §Keyboard Shortcuts Reference):
- `Ctrl+W` / `Cmd+W` → **Exit** application
- No explicit mention of `Ctrl+Q`

**Current implementation** (`IsmaMenuBarView.axaml:14-15`):
- `Ctrl+W` → `CloseCommand` (closes active tab, NOT exit)
- `Ctrl+Q` → `ExitCommand` (exits application)

**Impact:** The keyboard shortcut `Ctrl+W` has a different meaning than the original app. In the original, Ctrl+W exits the app. In the current implementation, Ctrl+W closes the active tab. This is a regression.

**Priority:** MEDIUM — breaks muscle memory from original app

---

### Gap 4: Blueprint Editor Toolbar — Toggle Buttons Don't Show State Labels

**Original spec** (`07-blueprint-editor-ux.md` §7.2):
- "New transition" / "Stop adding transaction" — text changes based on mode
- "Remove state" / "Stop remove state" — text changes based on mode
- "Remove transition" / "Stop remove transition" — text changes based on mode

**Current implementation** (`BlueprintEditorView.axaml:202-212`):
- `ToggleButton Content="Add Transition"` — static text, no mode-dependent label
- `ToggleButton Content="Remove State"` — static text
- `ToggleButton Content="Remove Transition"` — static text
- The mode-dependent visibility panels (lines 162-198) show helper text but don't change button labels

**Impact:** Users can't tell from the button text whether a toggle mode is active. The original app clearly shows "Stop adding transaction" when in add-transaction mode.

**Priority:** MEDIUM — reduces usability of toolbar

---

### Gap 5: ArrowLine and LoopArrow — Pointer Hit Testing Incomplete

**Original spec** (`07-blueprint-editor-ux.md` §4.5, §5.4):
- Arrow body click in remove mode → remove arrow
- Arrowhead single-click → open EditArrowPopOver
- Loop body click in remove mode → remove loop
- Loop arrowhead single-click → open EditArrowPopOver

**Current implementation:**
- `ArrowLine.cs:129-142` — `OnPointerPressed` calculates head distance but does nothing with it (empty body after distance check)
- `LoopArrow.cs:112-137` — `OnPointerPressed` calculates head distance and circle distance but does nothing with them (empty body after calculations)
- `BlueprintEditorView.axaml.cs:210-268` — The event handlers here (`OnArrowPointerPressed`, `OnLoopArrowPointerPressed`) do the actual work, but the controls themselves don't raise events properly

**Impact:** The ArrowLine and LoopArrow controls compute hit test distances but don't propagate them. The BlueprintEditorView.axaml.cs handlers work around this by receiving the control references and doing their own calculations. This is fragile and duplicates logic.

**Priority:** MEDIUM — causes unreliable arrow click detection

---

### Gap 6: Tasks PopOver — Missing Details PopOver for Simulation Metadata

**Original spec** (`06-ux-reference.md` §Tasks PopOver — Details PopOver (nested)):
- "Details button: Chevron icon (⋯) — opens a nested PopOver with simulation metadata"
- Fields: Model name, Cauchy initials (Start, End, Initial step), Integration method (name, accuracy, stability), Statistics (simulation time)

**Current implementation** (`TasksPopOverView.axaml`):
- Completed items have Show, Export, Remove buttons
- **No Details/chevron button**
- No nested PopOver for simulation metadata

**Impact:** Users cannot view simulation metadata (parameters used, timing) from the Tasks PopOver.

**Priority:** MEDIUM — missing UX feature from original

---

### Gap 7: State Content Preview Text in Blueprint Canvas

**Original spec** (`07-blueprint-editor-ux.md` §3.1):
- State boxes show only the state name, centered
- No content text preview on the canvas

**Current implementation** (`BlueprintEditorView.axaml:106-111`):
- Shows a second `TextBlock` with `Text="{Binding Text}"` below the name
- FontSize=9, Foreground=#666666, MaxWidth=100, wrapped

**Impact:** The canvas shows state content text preview, which the original does NOT have. This clutters the canvas view and wasn't in the original design.

**Priority:** LOW — cosmetic difference, not a bug per se

---

### Gap 8: Integration Test Coverage Gaps

**What's tested:**
- Blueprint editor VM operations (add/remove states, transitions, modes)
- Simulation workflow (full pipeline with mocked server)
- Syntax highlighting (fallback + server-driven)
- CSV export
- Settings panel UI
- Project management
- Validation

**What's NOT tested (per original feature matrix):**
- Tab close button interactions
- State drag-and-drop with arrow reconnection
- Inline name editing (200ms timer, drag disambiguation)
- Arrow click detection (body vs head)
- Edit Arrow PopOver open/close/binding
- Loop arrow double-click → text tab
- Blueprint-to-LISMA conversion with visual canvas state
- Tasks PopOver Details PopOver
- Window geometry save/restore
- Menu bar command execution through full UI tree

**Priority:** HIGH — critical interactions lack UI-level test coverage

---

## Implementation Plans

### Task 1: Fix Blueprint Editor State Dimensions and Colors

**Files to modify:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml` — state Border dimensions and CornerRadius
- `src/ISMA.App/Views/BlueprintEditorView.axaml.cs` — StateWidth/StateHeight constants
- `src/ISMA.App/Controls/ArrowLine.cs` — StateWidth/StateHeight constants
- `src/ISMA.App/Controls/LoopArrow.cs` — StateWidth/StateHeight constants
- `src/ISMA.ViewModels/ViewModels/BlueprintStateViewModel.cs` — FillColorHex values

**Implementation:**
1. Change state Border in AXAML: `Width="110" Height="65" CornerRadius="10"` (CornerRadius 10 = 20px arc diameter in Avalonia)
2. Update constants: `StateWidth = 110.0`, `StateHeight = 60.0` for Main/Init, `StateHeight = 65.0` for user states
3. Update ArrowLine/LoopArrow constants to match
4. Ensure fill colors match: Main=`#90EE90`, Init=`#ADD8E6`, User=`#F08080`

**Acceptance Checklist:**
- [ ] State boxes are 110px wide × 65px high (user states)
- [ ] Main/Init states are 110px wide × 60px high
- [ ] Corner rounding is visible (CornerRadius=10 in Avalonia = 20px arc)
- [ ] Main state fill is #90EE90 (LightGreen)
- [ ] Init state fill is #ADD8E6 (LightBlue)
- [ ] User states fill is #F08080 (Coral)
- [ ] Arrow lines connect to correct state centers
- [ ] Loop arrows are centered on states correctly
- [ ] `BlueprintEditorTests_CanvasStateDimensions_MatchOriginalSpec` passes

---

### Task 2: Add Tab Close Buttons to EditorTabPaneView

**Files to modify:**
- `src/ISMA.App/Views/EditorTabPaneView.axaml` — add close button to tab header

**Implementation:**
1. In `TabControl.ItemTemplate`, add a `PathIcon` or `Button` with an "X" glyph next to the name TextBlock
2. Wire the close button's `Command` to a command that closes the specific tab
3. Since `TabControl` doesn't support per-tab commands natively, use a custom approach:
   - Add a `Button` in the DataTemplate with `Command="{Binding $parent[TabControl].DataContext.CloseCommand}"`
   - Pass the specific project via `CommandParameter="{Binding}"`
   - In `MainWindowViewModel`, add a `CloseTabCommand(IProjectViewModel tab)` that finds and closes the specific project

**Acceptance Checklist:**
- [ ] Each tab has an X close button in the header
- [ ] Clicking X closes that specific tab
- [ ] Dirty indicator (*) still shows when present
- [ ] Tab close disposes project resources
- [ ] `EditorTabPaneTests_TabCloseButton_ClosesSpecificTab` passes
- [ ] `EditorTabPaneTests_TabCloseButton_DisposesProject` passes

---

### Task 3: Fix Keyboard Shortcuts

**Files to modify:**
- `src/ISMA.App/Views/IsmaMenuBarView.axaml` — swap Ctrl+W and Ctrl+Q
- `src/ISMA.ViewModels/ViewModels/MainWindowViewModel.cs` — verify command names

**Implementation:**
1. Change `Ctrl+W` from `CloseCommand` to `ExitCommand`
2. Change `Ctrl+Q` from `ExitCommand` to `CloseCommand`
3. Update menu item `InputGesture` attributes accordingly
4. Per original spec: `Ctrl+W` = Exit, `Ctrl+Q` is not originally specified — use `Ctrl+W` for Exit and keep `Ctrl+Shift+W` or no shortcut for Close

**Acceptance Checklist:**
- [ ] `Ctrl+W` exits the application (same as original)
- [ ] `Ctrl+Q` closes the active tab (or remove this shortcut)
- [ ] All other shortcuts unchanged (Ctrl+N, Ctrl+B, Ctrl+O, Ctrl+S, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+F4, Ctrl+F5)
- [ ] `KeyboardShortcutsTests_ExitUsesCtrlW` passes

---

### Task 4: Fix Blueprint Editor Toolbar Toggle Buttons

**Files to modify:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml` — toggle button content

**Implementation:**
1. Bind toggle button `Content` to a computed property on `BlueprintEditorViewModel`:
   - `AddTransitionButtonContent` → "Add Transition" or "Stop adding transaction"
   - `RemoveStateButtonContent` → "Remove State" or "Stop remove state"
   - `RemoveTransitionButtonContent` → "Remove Transition" or "Stop remove transition"
2. Add these properties to `BlueprintEditorViewModel` using `[Computed]` from CommunityToolkit.Mvvm

**Acceptance Checklist:**
- [ ] "Add Transition" button shows "Stop adding transaction" when mode is active
- [ ] "Remove State" button shows "Stop remove state" when mode is active
- [ ] "Remove Transition" button shows "Stop remove transition" when mode is active
- [ ] Button text updates immediately on mode change
- [ ] `BlueprintEditorTests_ToggleButtonLabels_UpdateWithMode` passes

---

### Task 5: Fix ArrowLine and LoopArrow Pointer Hit Testing

**Files to modify:**
- `src/ISMA.App/Controls/ArrowLine.cs` — complete OnPointerPressed
- `src/ISMA.App/Controls/LoopArrow.cs` — complete OnPointerPressed

**Implementation:**
1. Add `PointerPressedRoutedEvent` to both controls
2. Define routed event args with hit test info (isHead, position)
3. In `OnPointerPressed`, compute distance to arrowhead and raise the routed event
4. In `BlueprintEditorView.axaml`, subscribe to the routed events instead of doing manual calculations

**Acceptance Checklist:**
- [ ] ArrowLine correctly identifies arrowhead clicks vs body clicks
- [ ] LoopArrow correctly identifies arrowhead clicks vs body clicks
- [ ] Routed events propagate to parent handlers
- [ ] No duplicate hit test logic between controls and code-behind
- [ ] `ArrowHitTestTests_ArrowBodyClick_Detected` passes
- [ ] `ArrowHitTestTests_ArrowHeadClick_Detected` passes
- [ ] `ArrowHitTestTests_LoopBodyClick_Detected` passes
- [ ] `ArrowHitTestTests_LoopHeadClick_Detected` passes

---

### Task 6: Add Tasks PopOver Details PopOver

**Files to create/modify:**
- `src/ISMA.App/Views/TasksPopOverView.axaml` — add chevron button and nested Popup
- `src/ISMA.ViewModels/ViewModels/CompletedSimulationViewModel.cs` — add ShowDetailsCommand
- `src/ISMA.App/Views/DetailsPopOverView.axaml` — new view for simulation metadata

**Implementation:**
1. Add a "⋯" button next to Remove in each completed item
2. Create a `DetailsPopOverView` showing: Model name, Cauchy initials, Integration method, Statistics
3. Use Avalonia `Popup` with `IsLightDismissEnabled=True`
4. Bind to `CompletedSimulationViewModel` properties

**Acceptance Checklist:**
- [ ] Chevron (⋯) button appears on completed simulation items
- [ ] Clicking opens a nested PopOver with simulation metadata
- [ ] Metadata shows: Model name, Start/End/Step, Method, Accuracy, Stability, Simulation time
- [ ] PopOver dismisses on light dismiss (click outside)
- [ ] `TasksPopOverTests_DetailsPopOver_ShowsMetadata` passes

---

### Task 7: Remove State Content Preview from Canvas

**Files to modify:**
- `src/ISMA.App/Views/BlueprintEditorView.axaml` — remove the TextBlock showing state text

**Implementation:**
1. Remove the second `TextBlock` (lines 106-111) that shows `{Binding Text}` below the state name
2. Keep only the state name `TextBlock`

**Acceptance Checklist:**
- [ ] State boxes show only the name on the canvas
- [ ] No content text preview visible
- [ ] Double-click still opens text editor tab

---

### Task 8: Write Integration Tests for Missing UI Interactions

**Files to create:**
- `tests/ISMA.Tests.Integration/BlueprintEditorUiTests.cs` — UI-level blueprint editor tests
- `tests/ISMA.Tests.Integration/TabCloseTests.cs` — tab close button tests
- `tests/ISMA.Tests.Integration/KeyboardShortcutsTests.cs` — keyboard shortcut tests
- `tests/ISMA.Tests.Integration/ArrowHitTestTests.cs` — arrow click detection tests
- `tests/ISMA.Tests.Integration/DetailsPopOverTests.cs` — details popover tests

**Implementation:**
1. Use existing `IntegrationTestBase` pattern (headless Avalonia with mocked server)
2. Use `UiHelpers` for UI interactions (click buttons, set text, find controls)
3. Test through actual UI control tree, not just ViewModels
4. Use `window.Flush()` to force layout updates in headless mode

**Acceptance Checklist:**
- [ ] `BlueprintEditorUiTests_StateDrag_RepositionsStateAndUpdatesArrows` — drag state, verify position + arrow reconnection
- [ ] `BlueprintEditorUiTests_InlineNameEdit_RenamesState` — single-click, type name, verify rename
- [ ] `BlueprintEditorUiTests_TransitionCreation_TwoClicks_CreatesArrow` — add transition mode, click two states
- [ ] `BlueprintEditorUiTests_LoopCreation_SameStateTwice_CreatesLoop` — add transition, click same state twice
- [ ] `TabCloseTests_TabCloseButton_ClosesTabAndDisposes` — click X on tab, verify tab count decreases
- [ ] `KeyboardShortcutsTests_ExitWithCtrlW_ExitsApp` — Ctrl+W triggers exit
- [ ] `ArrowHitTestTests_ArrowHeadClick_OpensPopOver` — click arrowhead, verify PopOver opens
- [ ] `ArrowHitTestTests_ArrowBodyClickInRemoveMode_RemovesArrow` — remove mode + click arrow body
- [ ] `DetailsPopOverTests_ChevronClick_ShowsMetadata` — click chevron, verify metadata visible
- [ ] All new tests pass: `dotnet test ISMA.Tests.Integration` 100% green

---

## Implementation Priority Order

| # | Task | Priority | Effort | Depends On |
|---|------|----------|--------|------------|
| 1 | Fix state dimensions/colors | HIGH | 1h | — |
| 2 | Add tab close buttons | HIGH | 2h | — |
| 3 | Fix keyboard shortcuts | MEDIUM | 30m | — |
| 4 | Fix toggle button labels | MEDIUM | 1h | — |
| 5 | Fix arrow hit testing | MEDIUM | 2h | Task 1 |
| 6 | Add Details PopOver | MEDIUM | 2h | — |
| 7 | Remove state content preview | LOW | 15m | — |
| 8 | Write integration tests | HIGH | 4h | Tasks 1-6 |

**Total estimated effort: ~13 hours**

---

## Previously Completed Tasks (for reference)

### Server-Driven Syntax Highlighting ✅ COMPLETED
Implemented via `ServerDrivenHighlightingTransformer` — `DocumentColorizingTransformer` that applies server tokens to AvaloniaEdit rendering pipeline.

### Loop Arrow Double-Click → Text Editor Tab ✅ COMPLETED
Implemented in `BlueprintEditorView.axaml.cs:451-479` (`OnLoopArrowHeadClicked`).

### CSV Export with FileDialog ✅ COMPLETED
Implemented in `SimulationResultService.cs` — `ShowExportDialog` with FileDialog + async export.

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
