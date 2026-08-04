# Blueprint Editor UX Reference

## Purpose

This document describes the complete user experience of the visual statechart (blueprint) editor. It covers every interaction: canvas, states, arrows, popover, toolbar, modes, and LISMA conversion. Use this as a specification when implementing a replacement UI with feature parity.

## Editor Layout

```
┌────────────────────────────────────────────────────────────────────┐
│                                                                    │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Tab: [Diagram]                                               │  │
│  │                                                              │  │
│  │  ┌────────────────────────────────────────────────────────┐  │  │
│  │  │                                                        │  │  │
│  │  │              Canvas (scrollable area)                  │  │  │
│  │  │                                                        │  │  │
│  │  │   ┌──────────┐         ┌──────────┐                   │  │  │
│  │  │   │ Main     │────────▶│ State 1  │────┐              │  │  │
│  │  │   │ (Green)  │         │ (Coral)  │    │              │  │  │
│  │  │   └──────────┘         └──────────┘    │              │  │  │
│  │  │        │                              │              │  │  │
│  │  │        ▼                              ▼              │  │  │
│  │  │   ┌──────────┐         ┌──────────┐                 │  │  │
│  │  │   │ init     │         │ State 2  │◀───────────────┘  │  │  │
│  │  │   │ (Blue)   │         │ (Coral)  │  (loop arrow)     │  │  │
│  │  │   └──────────┘         └──────────┘                   │  │  │
│  │  │                                                        │  │  │
│  │  └────────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
├────────────────────────────────────────────────────────────────────┤
│ [New state] [New transition] | [Remove state] [Remove transition] │
└────────────────────────────────────────────────────────────────────┘
```

## Toolbar

The toolbar is fixed at the bottom of the editor. It contains five buttons:

| Button | Command | Description |
|--------|---------|-------------|
| **New state** | `AddState` | Creates a new state with auto-generated name ("State 1", "State 2", ...) |
| **New transition** | `ToggleAddTransition` | Enters AddTransition mode; text changes to "Stop adding transaction" |
| **Remove state** | `ToggleRemoveState` | Enters RemoveState mode; text changes to "Stop remove state" |
| **Remove transition** | `ToggleRemoveTransition` | Enters RemoveTransition mode; text changes to "Stop remove transition" |

A `Separator` is placed between the transition and remove buttons.

## Canvas

The canvas is a scrollable area where states and transitions are rendered. It supports:

- **Pan:** Scroll the viewport
- **Zoom:** (not yet implemented)
- **State dragging:** Click and drag a state to reposition it

### Canvas Coordinates

The canvas uses a standard 2D coordinate system:
- Origin (0, 0) is at the top-left corner
- X increases to the right
- Y increases downward (Y-down coordinate system, same as JavaFX)

## States

### Visual Appearance

| State Type | Background Color | Height | Editable Name |
|------------|-----------------|--------|---------------|
| **Main** | LightGreen (#90EE90) | 60px | Yes |
| **Init** | LightBlue (#ADD8E6) | 60px | No |
| **Regular** | Coral (#FF7F50) | 65px | Yes |

All states have:
- Rounded corners (radius: 20px)
- Black border (stroke: 1px)
- Centered state name (font: Arial, 16px, bold)
- Minimum width: 110px

### State Interactions

| Action | Effect |
|--------|--------|
| **Single-click** (idle mode) | Enters inline name edit mode (if editable) |
| **Double-click** | Opens text editor tab for LISMA body text |
| **Drag** | Moves the state to a new position (updates X/Y coordinates) |
| **Single-click** (AddTransition mode) | Selects the state as the first endpoint of a transition |
| **Single-click** (RemoveState mode) | Removes the state (except Main and Init) |

### Inline Name Editing

When a state enters edit mode:
1. A `TextBox` overlay appears over the state, centered horizontally
2. The state name is pre-filled in the textbox
3. The user can type a new name
4. The name is committed on:
   - **Enter key** — commits the new name
   - **Lost focus** — commits the new name
5. The name is cancelled on:
   - **Escape key** — reverts to the previous name

**Name uniqueness:** State names must be unique. If a user enters a duplicate name, the edit is cancelled and the original name is restored.

### State Naming Convention

Auto-generated names follow the pattern: `"{default} {N}"` where N is an incrementing integer starting from 1:
- "State 1", "State 2", "State 3", ...

The counter never decreases, even if high-numbered states are deleted.

## Transitions

### Visual Appearance

- **Line:** Black, 3px stroke thickness, with 10px perpendicular offset from the direct state-to-state line
- **Arrowhead:** Black filled triangle (7px half-width), rotated to match the line direction
- **Label:** Black text (16px font), positioned at offset (75px, 50px) from the line center

### Inter-State Transitions

**Creation flow (two-click):**
1. Click "New transition" button (toolbar)
2. Click the **source state** (first click selects it)
3. Click the **target state** (second click creates the transition)
4. If both clicks are on the same state, a **loop arrow** is created instead
5. After creation, the editor returns to Idle mode

**Editing:**
- Click the arrowhead to open the Edit Arrow Popover
- Edit the **Alias** (optional display name) and **Predicate** (guard condition)
- The popover closes when focus is lost or when clicking outside

**Deletion:**
- In RemoveTransition mode, click a transition arrow to remove it

### Self-Loop Transitions

Self-loop arrows are drawn as a circle to the right of the state, with an arrowhead at the top of the circle pointing downward.

**Creation:** Same as inter-state transitions, but both clicks are on the same state.

**Editing:** Same as inter-state transitions — click the arrowhead to open the popover.

## Edit Arrow Popover

When a user clicks an arrowhead, a floating popover appears:

```
┌────────────────────────────────────────────────────────────┐
│  Alias (optional)                                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [Alias text here                               ]    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  Predicate                                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [Predicate text here                           ]    │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

**Positioning:** Centered horizontally at the click X coordinate, placed slightly above the click Y coordinate.

**Styling:**
- White background
- Gray border (1px)
- Corner radius: 5px
- Padding: 10px
- Drop shadow (radius: 20px)

**Behavior:**
- Bidirectional binding: changes in the textboxes update the ViewModel immediately
- Closing: occurs when focus is lost (click outside) or when the popover is programmatically closed
- The popover is automatically removed from the visual tree when closed

## Editor Modes

The editor operates in one of four modes, controlled by toolbar buttons.

### Idle Mode (Default)

- **Toolbar buttons:** "New state", "New transition", "Remove state", "Remove transition"
- **State click:** Enters inline name edit mode (if editable)
- **State double-click:** Opens text editor tab
- **Arrowhead click:** Opens Edit Arrow Popover

### AddTransition Mode

- **Toolbar buttons:** "New state", "Stop adding transaction", "Remove state", "Remove transition"
- **State click (first):** Selects the state as the source endpoint
- **State click (second, different state):** Creates an inter-state transition, returns to Idle
- **State click (second, same state):** Creates a loop arrow, returns to Idle
- **Button click:** Returns to Idle mode

### RemoveState Mode

- **Toolbar buttons:** "New state", "New transition", "Stop remove state", "Remove transition"
- **State click:** Removes the state and all its associated transitions
- **Main/Init states:** Protected from removal (click has no effect)
- **Button click:** Returns to Idle mode

### RemoveTransition Mode

- **Toolbar buttons:** "New state", "New transition", "Remove state", "Stop remove transition"
- **Transition click:** Removes the transition
- **Button click:** Returns to Idle mode

## LISMA Conversion

### Overview

The blueprint editor converts the visual statechart to LISMA DSL text via the `BlueprintModel.ToLismaText()` method. This is called at compile/snapshot time, not during editing.

### Output Format

**Main state (first):**
```
<Main state text>
```

**Regular state blocks (grouped by target state + predicate):**
```
state <stateName> { <state text> } from <startState1>,<startState2>,...;
```

**Loop transactions (expanded into pseudo-states):**
```
state <stateName>_pseudo_1 (<predicate>) { <loop text> } from <stateName>;
state <stateName> (1 > 0) { <original state text> } from <stateName>_pseudo_1;
```

### Grouping Rules

1. Transitions targeting the same state with the same predicate are **grouped** into a single `state` block
2. Multiple source states are listed in the `from` clause, comma-separated
3. If no input states exist for a state block, the output is: `state <name>;`
4. Loop transactions create **two pseudo-states** to ensure predicate ordering

### Example

**Input blueprint:**
- Main state: "Main body"
- State "A" (from Main, predicate: "x > 0"): "A body"
- State "B" (from Main, predicate: "x > 0"): "B body"
- State "B" (from A, predicate: "x > 0"): "B body" (same predicate as from Main)
- Loop on "A" (predicate: "y < 10"): "A loop body"

**Output LISMA:**
```
Main body

state B { B body } from Main,A;

state A_pseudo_1 (y < 10) { A loop body } from A;
state A (1 > 0) { A body } from A_pseudo_1;
```

Note: States "A" and "B" from Main with predicate "x > 0" are **not grouped** because they have different target state names. Only transitions targeting the **same** state with the **same** predicate are grouped.

## Keyboard Shortcuts

| Key | Context | Action |
|-----|---------|--------|
| **Enter** | Inline name editing | Commit new name |
| **Escape** | Inline name editing | Cancel edit, revert name |

## Error Handling

### Name Uniqueness

If a user enters a duplicate state name:
1. The `StateViewModel.Name` setter checks uniqueness via `IsNameUnique` callback
2. If not unique, the name is reverted to the previous value
3. No error message is shown — the edit silently fails

### Protected States

Main and Init states cannot be removed:
- In RemoveState mode, clicking Main or Init has no effect
- The `IsmaBlueprintViewModel.RemoveState()` method checks for these names before proceeding

## Performance Considerations

### Canvas Node Synchronization

CanvasView uses dictionary-based lookup (`Dictionary<StateViewModel, StateBox>`) for O(1) node access. When the ViewModel collection changes:
1. New nodes are created and added to `Children`
2. Removed nodes are removed from `Children` and `Cleanup()` is called to remove event handlers

### Arrow Geometry Recalculation

When a state is dragged:
1. `StateViewModel.X` and `StateViewModel.Y` are updated
2. All `TransactionArrow` and `LoopTransactionArrow` controls that reference this state recalculate their geometry
3. The recalculation uses `ArrowGeometry.CalculateArrowGeometry()` which is pure math (no UI operations)

### Memory Management

All controls implement a `Cleanup()` method that removes event handlers to prevent memory leaks:
- `StateBox.Cleanup()` — removes PointerPressed/Released/Moved handlers, TextBox event handlers
- `TransactionArrow.Cleanup()` — removes PointerPressed handler, disposes PopOver
- `LoopTransactionArrow.Cleanup()` — same as TransactionArrow
- `EditArrowPopOver.Dispose()` — removes PointerPressed handler

## Testing

### Unit Tests (planned)

| Test Class | Tests | Coverage |
|------------|-------|----------|
| `ArrowGeometryTest` | 8 tests | All directional arrow calculations (N, S, E, W, NE, NW, SE, SW) |
| `NameChangingMonitorTest` | 11 tests | Registration, uniqueness, counter management, edge cases |
| `EditorModeTest` | 7 tests | Mode states, transitions, `isNotEditingMode()` |
| `CanvasViewModelTest` | 6 tests | Cascading removal, name registration, collection management |

### Integration Tests (planned)

| Test Scenario | Description |
|---------------|-------------|
| State creation | Click "New state" → verify StateBox appears |
| State dragging | Drag state → verify X/Y update, arrows recalculate |
| Transition creation | Two-click flow → verify TransactionArrow appears |
| Loop creation | Single-click on same state twice → verify LoopTransactionArrow |
| Inline editing | Click state → type name → Enter → verify name update |
| Popover editing | Click arrowhead → type in popover → verify ViewModel update |
| Remove state | RemoveState mode → click state → verify state + arrows removed |
| Serialization round-trip | ToBlueprintModel → FromBlueprintModel → verify data preserved |

## Accessibility

### Current State

The blueprint editor currently has minimal accessibility support:
- No automation IDs on controls
- No keyboard navigation for canvas operations
- No screen reader labels on states/arrows

### Planned Improvements

- Add `AutomationId` properties to StateBox, TransactionArrow, LoopTransactionArrow
- Add keyboard shortcuts for toolbar operations (e.g., Ctrl+N for new state)
- Add screen reader announcements for mode changes and state/transition creation
