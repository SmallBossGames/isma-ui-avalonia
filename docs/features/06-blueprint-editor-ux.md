# UX Specification — Blueprint / Statechart Editor

## Purpose

This document provides a detailed specification of the Blueprint (Statechart) Editor in ISMA. It covers the visual canvas, state boxes, transition arrows, loop arrows, edit popover, toolbar, interaction modes, data model, and the conversion to LISMA text. Use this as the ground truth when implementing or modifying the visual statechart editor.

---

## 1. Overview

### 1.1 Assembly Structure

```
ISMA.Domain/
├── Models/
│   ├── BlueprintModel.cs                  # Serializable JSON model
│   ├── BlueprintStateModel.cs             # State data model
│   ├── BlueprintTransactionModel.cs       # Inter-state transition model
│   └── BlueprintLoopTransactionModel.cs   # Loop transition model
└── Conversion/
    └── BlueprintToLismaConverter.cs       # Blueprint → LISMA text

ISMA.ViewModels/
├── ViewModels/
│   ├── BlueprintEditorViewModel.cs        # Editor state machine + collections
│   ├── BlueprintStateViewModel.cs         # State box ViewModel
│   └── BlueprintEditorMode.cs             # Editor mode enum
└── Services/
    └── NameChangingMonitor.cs             # Unique name enforcement

ISMA.App/
├── Views/
│   └── BlueprintEditorView.axaml          # Canvas + toolbar + data templates
├── Controls/
│   ├── ArrowLine.cs                       # Custom control: transaction arrow
│   └── LoopArrow.cs                       # Custom control: loop arrow
└── Services/
    └── TextEditorFactory.cs               # SPI for text editor creation
```

The Blueprint Editor is a **visual finite-state machine editor**. Users create states as draggable boxes on a canvas, draw transitions between them, and define transition predicates (conditions). The visual statechart is compiled into LISMA text at build time via `BlueprintToLismaConverter`.

### 1.2 Container Hierarchy

```
BlueprintEditorView (Grid)
├── Row 0: Canvas (scrollable)
│   ├── ItemsControl: TransactionArrows (z-index: bottom)
│   ├── ItemsControl: LoopArrows (z-index: middle)
│   ├── ItemsControl: StateBoxes (z-index: top)
│   └── EditArrowPopOverView (floating, transient)
└── Row 1: StackPanel (toolbar at bottom)
    ├── "New state" button
    ├── "New transition" / "Stop adding transaction" toggle button
    ├── Separator
    ├── "Remove state" / "Stop remove state" toggle button
    └── "Remove transition" / "Stop remove transition" toggle button
```

---

## 2. Canvas

### 2.1 Properties

| Property | Value |
|----------|-------|
| Root type | `Canvas` (absolute positioning) |
| Scrolling | Via `ScrollViewer` wrapper |
| Scroll constraints | No min/max viewport size — scrolls to content |
| Coordinate origin | (0, 0) at top-left of canvas |
| Position clamping | All state positions: `max(position, 0.0)` — negative coordinates forbidden |

### 2.2 Rendering Order (z-index via ItemsControl order)

| Element | Layer |
|---------|-------|
| TransactionArrows ItemsControl | Bottom (drawn first) |
| LoopArrows ItemsControl | Middle |
| StateBoxes ItemsControl | Top (drawn last, on top of arrows) |

---

## 3. State Boxes

### 3.1 Visual Structure

```
┌────────────────────────────────────┐  ← rounded rectangle, corner radius = 20px
│                                    │
│        [ State Name Label ]        │  ← Inter 12pt, centered
│                                    │
└────────────────────────────────────┘
```

| Property | Default | Notes |
|----------|---------|-------|
| `Width` | 110 | Fixed for all states |
| `Height` | 65 | 60 for Main/Init states |
| `CornerRadius` | 20 | Corner rounding |
| Font | Inter 12pt | State name text |

### 3.2 State Types

| Type | Color | Position (x, y) | Editable | Draggable |
|------|-------|------------------|----------|-----------|
| **Main** | `#90EE90` (LightGreen) | (20, 10) | No | Yes |
| **Init** | `#ADD8E6` (LightBlue) | (10, 100) | No | Yes |
| **User** | `#F08080` (Coral) | (10, 200) | Yes | Yes |

**Main state:** name = `"Main"`, positioned at (20, 10)
**Init state:** name = `"init"`, positioned at (10, 100)

### 3.3 Inline Name Editing

When a user single-clicks an editable (user) state box:

1. A 200ms delay timer begins
2. If the mouse was **not** dragged during that period:
   - A `TextBox` appears inside the state box (replacing the `TextBlock`)
   - The text box is populated with the current `Name`
   - Focus is requested on the text box
3. When the text box loses focus:
   - The `Name` property is updated from the text box's content
   - The `TextBox` hides, the `TextBlock` reappears
4. The new name is validated against `NameChangingMonitor`:
   - If the name is **already taken**, the old name is **restored**
   - If unique, the monitor updates its registry

### 3.4 Double-Click Behavior

Double-clicking any state box (Main, Init, or User) opens a **text editor tab** in the main `TabControl`:

- Tab name: bound to the state's `Name`
- Tab content: the state's `Text` property (the LISMA body of that state)
- Changes in the text editor write back to `state.Text` via `ContentChanged` event
- Tab close: disposes the text editor instance

### 3.5 Drag Interaction

| Event | Handler |
|-------|---------|
| `PointerPressed` | Records `startX`, `startY`, sets `isDragging = false` |
| `PointerMoved` | If primary button is down: sets `isDragging = true` |
| `PointerReleased` | If `isDragging`: moves the state to the new position (clamped to ≥ 0) |

**Drag is only active when:** primary button is down AND NOT in remove-state mode.

### 3.6 Single-Click vs. Drag Disambiguation

The `BlueprintEditorView` uses a **200ms delayed timer** approach:

```
PointerPressed → reset isDragging = false, start 200ms timer
  ↓
PointerMoved → set isDragging = true
  ↓
200ms elapsed → if !isDragging → trigger single-click (inline name edit)
                if isDragging  → skip single-click (drag already handled)
PointerPressed with clickCount == 2 → cancel pending single-click, trigger double-click
```

### 3.7 Editability Bindings

User state `IsEditable` is dynamically bound:

```
IsEditable = !(IsRemoveStateMode OR IsAddTransitionMode)
```

When in add-transition or remove-state mode, inline name editing is disabled.

---

## 4. Transition Arrows (Inter-State)

### 4.1 Visual Structure

```
StateBox A ────────────► StateBox B
               [Predicate]
```

A line from the center of the source state to the center of the target state, with an arrowhead pointing at the target. The line is offset to avoid overlapping the state box borders.

### 4.2 Geometry Calculation

The line endpoints are computed in the `ArrowLine` custom control, triggered whenever any position property changes:

```
x = endX - startX    // delta X between state centers
y = endY - startY    // delta Y between state centers
angle = atan2(x, y) + PI / 2  // perpendicular angle

offsetDistance = 10.0
offsetX = offsetDistance * sin(angle)
offsetY = offsetDistance * cos(angle)

// Line endpoints (offset from state centers)
lineStartX = startX + offsetX
lineStartY = startY + offsetY
lineEndX   = endX + offsetX
lineEndY   = endY + offsetY

// Arrowhead position and rotation
arrowhead.TranslateX = offsetX
arrowhead.TranslateY = offsetY
arrowhead.Rotate = -angle / PI * 180.0

// Label offset (perpendicular, further out)
textOffsetX = 75.0 * sin(angle)
textOffsetY = 50.0 * cos(angle)
```

### 4.3 Arrowhead

- **Shape:** Triangle (3-point polygon)
- **Stroke width:** 3.0
- **Rotation:** dynamically rotated to match line angle
- **Click target:** the arrowhead has its own click handler
  - **Single-click on arrowhead:** opens `EditArrowPopOverView`

### 4.4 Label Display

The label shows the arrow's alias if present, otherwise the predicate:

```
displayedText = alias ?? predicate
```

- **Font:** Inter 12pt
- **Position:** centered, offset perpendicular from line midpoint

### 4.5 Click Handlers

| Target | Action |
|--------|--------|
| Arrow body (line area) | If in `IsRemoveTransitionMode` → remove the arrow |
| Arrowhead | Single-click: open `EditArrowPopOverView` |

### 4.6 Duplication Prevention

`AddTransactionArrow()` checks: `Transactions.Any(t => t.StartStateBox == start && t.EndStateBox == end)`. If a transition already exists between the two states, it is silently skipped.

---

## 5. Loop Transition Arrows (Self-Transitions)

### 5.1 Visual Structure

```
          ┌──────────┐
    ┌─────►│  Circle   │─────┐
    │      │ (r=40,    │     │
    │      └──────────┘     │
    │                       ▼
    └──────────► StateBox ◄──┘
```

A loop arrow draws a **circle** above the state, with an arrowhead pointing back into the state. The circle has radius 40.

### 5.2 Properties

| Property | Default | Notes |
|----------|---------|-------|
| Circle radius | 40 | Transparent fill, black stroke, stroke width 3 |
| Circle center X | 60 | Offset within the arrow's local coordinate space |
| Arrowhead X position | 100 | At the right side of the circle |
| Arrowhead shape | Triangle | Points right |
| Arrowhead stroke | 3 | |
| Label X position | 120 | To the right of the circle |
| Label Y offset | -10 | Slightly above center line |
| Layout X | Bound to `StateBox.CenterX` | Centered horizontally on the state |
| Layout Y | Bound to `StateBox.CenterY` | Centered vertically on the state |

### 5.3 Label Display

Same alias-or-predicate logic as inter-state arrows:

```
displayedText = alias ?? predicate
```

### 5.4 Click Handlers

| Target | Action |
|--------|--------|
| Arrow body (circle + line) | If in `IsRemoveTransitionMode` → remove the arrow |
| Arrowhead (single-click) | Open `EditArrowPopOverView` |
| Arrowhead (double-click) | Open text editor tab for loop content, tab name = `"{stateName} (loop)"` |

### 5.5 Loop Content Text

Unlike inter-state transitions (which have no text content), loop arrows carry a `Text` property:

- This is the LISMA body text for the loop pseudo-state
- Double-clicking the arrowhead opens a text editor tab named `"{stateName} (loop)"`
- Changes in the editor write back to `arrow.Text`

### 5.6 Duplication Prevention

`AddLoopTransactionArrow()` checks: `LoopTransactions.Any(l => l.StateBox == stateBox)`. Only one loop arrow per state is allowed.

---

## 6. Edit Arrow PopOver

### 6.1 Visual Structure

```
┌──────────────────────────────────────────────────┐
│  Alias (optional)                                │
│  ┌────────────────────────────────────────────┐  │  ← TextBox, minWidth 300
│  └────────────────────────────────────────────┘  │
│  Predicate                                       │
│  ┌────────────────────────────────────────────┐  │  ← TextBox, minWidth 300
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

| Property | Value |
|----------|-------|
| Layout | `StackPanel` (vertical) |
| Min width | 300 |
| Padding | 10 (all sides) |
| Background | White, CornerRadius 5 |
| Effect | DropShadow, radius 20, color LightGray |
| Horizontal position | Centered on click X |
| Vertical position | 2px above cursor |

### 6.2 Data Binding

Both text fields use **two-way binding**:

```
TextBox.Text ↔ EditArrowPopOverViewModel.Alias
TextBox.Text ↔ EditArrowPopOverViewModel.Predicate
```

Changes in either direction propagate immediately via CommunityToolkit.Mvvm source-generated bindings.

### 6.3 Dismissal

The PopOver is added to the canvas children and auto-removed when `PointerExited` fires on it:

```csharp
popover.PointerExited += (s, e) => Canvas.Children.Remove(popover);
```

This creates a "click-away" behavior: moving the mouse outside the PopOver dismisses it.

---

## 7. Toolbar

### 7.1 Layout

```
┌────────────────────────────────────────────────────────────────────┐
│ [New state] [New transition ▼] | [Remove state ▼] [Remove trans ▼]│
└────────────────────────────────────────────────────────────────────┘
```

The toolbar appears at the **bottom** of the `Grid`. It contains 4 buttons separated by a `Separator` between the "add" group and "remove" group.

### 7.2 Button Behaviors

#### "New state"

| Action | Effect |
|--------|--------|
| Click | 1. Reset all editor modes |
| | 2. Create a new `BlueprintStateViewModel` at position (10, 200) |
| | 3. Auto-generate name via `NameChangingMonitor.CreateNextDefaultName()` |
| | 4. Register name with monitor |
| | 5. Add to canvas |

**New state properties:**
- Color: `#F08080` (Coral)
- Name: `"New state N"` where N is the next available integer
- Position: X=10, Y=200
- Editable: yes
- Initial text: `""`

#### "New transition" / "Stop adding transaction"

Toggle button. Text changes based on mode:

| Mode | Text |
|------|------|
| Off | "New transition" |
| On | "Stop adding transaction" |

**When turned ON:**
1. Reset all editor modes first
2. Set `Mode = BlueprintEditorMode.AddTransition`
3. Reset `TransitionClickCount = 0`

**When turned OFF:**
1. Reset all editor modes

**During add-transaction mode:**
1. Click a state box → records it as `StatesToLink[0]`, increments counter to 1
2. Click a second state box → records as `StatesToLink[1]`, increments counter to 2
3. Counter reaches 2:
   - If both states are the **same** → create a `LoopArrow` (self-loop)
   - If the states are **different** → create an `ArrowLine` (inter-state)
4. Reset `Mode = Default` after each pair

**Important:** During add-transaction mode:
- User states become non-editable (`IsEditable` bound to `!(IsRemoveStateMode OR IsAddTransitionMode)`)
- Clicking states triggers transition creation (not name editing)
- The mode is automatically turned off after creating a transition

#### "Remove state" / "Stop remove state"

Toggle button.

| Mode | Text |
|------|------|
| Off | "Remove state" |
| On | "Stop remove state" |

**When turned ON:**
1. Reset all editor modes first
2. Set `Mode = BlueprintEditorMode.RemoveState`

**When turned OFF:**
1. Reset all editor modes

**During remove-state mode:**
- Clicking any state box triggers state removal
- The state is removed along with all associated transitions and loop arrows

**`RemoveFromCanvas()` on a StateBox:**
1. Find all transactions where this state is start or end
2. Remove each transaction (arrow + tracking entry)
3. Remove this state from `States` collection
4. Remove from canvas children

#### "Remove transition" / "Stop remove transition"

Toggle button.

| Mode | Text |
|------|------|
| Off | "Remove transition" |
| On | "Stop remove transition" |

**When ON:** clicking the body of any arrow (inter-state or loop) removes it.

**When ON:** clicking an arrowhead does **not** open the PopOver — the arrow body click handler fires first.

### 7.3 Mode Reset

`ResetEditorMode()` clears the mode flag:

```csharp
private void ResetEditorMode() => Mode = BlueprintEditorMode.Default;
```

Every toolbar button action calls `ResetEditorMode()` before setting or toggling its own mode.

---

## 8. Interaction Modes Summary

| Mode | Active States | Arrow Body Click | Arrowhead Click | State Box Single-Click | State Box Drag |
|------|--------------|------------------|-----------------|----------------------|----------------|
| **Default** | None | No effect | Open PopOver | Inline name edit | Yes |
| **Add Transition** | `Mode == AddTransition` | No effect | Open PopOver | Record as source/target | No |
| **Remove State** | `Mode == RemoveState` | No effect | Open PopOver | Remove state + arrows | No |
| **Remove Transition** | `Mode == RemoveTransition` | Remove arrow | Open PopOver | Inline name edit | Yes |

---

## 9. Data Model

### 9.1 Serializable Model (JSON persistence)

**File:** `ISMA.Domain/Models/BlueprintModel.cs`

```
BlueprintModel
├── Main: BlueprintStateModel        // Main state
├── Init: BlueprintStateModel        // Init state
├── States: BlueprintStateModel[]    // User-created states
├── Transactions: BlueprintTransactionModel[]  // Inter-state transitions
└── LoopTransactions: BlueprintLoopTransactionModel[]  // Self-loops

BlueprintStateModel
├── CanvasPositionX: double
├── CanvasPositionY: double
├── Name: string
├── Text: string
├── IsMain: bool
└── IsInit: bool

BlueprintTransactionModel
├── StartStateName: string       // Resolved by name at load time
├── EndStateName: string
├── Predicate: string
└── Alias: string = ""

BlueprintLoopTransactionModel
├── StateName: string
├── Predicate: string
├── Alias: string = ""
└── Text: string                 // Loop body content
```

### 9.2 Default Empty Model

```csharp
BlueprintModel.Empty = new(
    new BlueprintStateModel(10.0, 10.0, "Main", "", isMain: true, isInit: false),
    new BlueprintStateModel(10.0, 100.0, "init", "", isMain: false, isInit: true),
    Array.Empty<BlueprintStateModel>(),
    Array.Empty<BlueprintTransactionModel>(),
    Array.Empty<BlueprintLoopTransactionModel>()
);
```

### 9.3 Data Flow: Save (Canvas → Model → JSON)

1. `BlueprintProjectViewModel` exposes the current `BlueprintModel` via `GetBlueprintModel()`
2. Each `BlueprintStateViewModel` is converted: extracts `X`, `Y`, `Name`, `Text`
3. Each transaction is converted: extracts state **names** (not references), predicate, alias
4. Each loop is converted: extracts state name, predicate, alias, text
5. A `BlueprintModel` is assembled and returned
6. `ProjectFileService.SaveAsync()` serializes the model to JSON and writes to `.scisma` file

### 9.4 Data Flow: Load (JSON → Model → Canvas)

1. `ProjectFileService.OpenAsync()` reads JSON file and deserializes to `BlueprintModel`
2. `BlueprintProjectViewModel` receives the model
3. Main and Init state data is applied onto the fixed state boxes
4. New `BlueprintStateViewModel` instances are created from `model.States`
5. A name→StateBox map is built including Main, Init, and new states
6. For each `BlueprintTransactionModel`, the start/end states are looked up by name and an `ArrowLine` is created
7. For each `BlueprintLoopTransactionModel`, a `LoopArrow` is created
8. All arrows bind their geometry to the state box centers

---

## 10. Name Changing Monitor

### 10.1 Purpose

Ensures all state names are unique within the blueprint. Tracks registered names and auto-increments default name counters.

### 10.2 Algorithm

```
existedNames: HashSet<string>
nextNameCounter: int = 1
defaultNameRegex: "^New state (\\d+)$"
```

**`TryRegister(name)`:**
1. If `existedNames` contains `name` → return `false` (duplicate)
2. If `name` matches `defaultNameRegex`, extract the digit → set `nextNameCounter = max(nextNameCounter, digit + 1)`
3. Add `name` to `existedNames` → return `true`

**`TryUnregister(name)`:**
1. If `existedNames` contains `name` → remove it → return `true`
2. Otherwise → return `false`

**`CreateNextDefaultName()`:**
1. Return `"New state $nextNameCounter"`

### 10.3 Name Edit Rollback

When a user edits a state name via inline editing:
1. The current name is saved as `previousName` before edit begins
2. On focus loss, `TryRegister(newName)` is called
3. If registration fails (duplicate), `Name = previousName` restores the old name
4. If registration succeeds, `TryUnregister(previousName)` updates the registry

---

## 11. LISMA Conversion

### 11.1 Overview

`BlueprintToLismaConverter.Convert(BlueprintModel)` transforms the visual statechart into LISMA text. This runs at compile/snapshot time, not during editing.

### 11.2 Output Format — Regular Transactions

Each group of transitions targeting the same state with the same predicate produces a single `StateBlock`:

```
state "key" {
    <state text>
} from <startState1>,<startState2>,...;
```

Where `key` = `"{targetStateName} ({predicate})"` (trimmed).

Multiple transitions from different states to the same target state with the same predicate are **merged** into a single `from` clause: `from StateA,StateB,StateC;`

### 11.3 Output Format — Loop Transactions

Each loop transaction is expanded into **two pseudo-states**:

```
state <stateName>_pseudo_1 (<predicate>) {
    <loop text>
} from <stateName>;

state <stateName> (1 > 0) {
    <original state text>
} from <stateName>_pseudo_1;
```

The original state's predicate is replaced with `1 > 0` (always true), and a new pseudo-state is inserted between the state and itself to represent the loop.

### 11.4 Output Order

1. Main state text (first, as top-level content)
2. All state blocks from regular transactions (grouped by target + predicate)
3. All loop transaction expansions (one pseudo-state pair per loop)

Each section is followed by a blank line.

---

## 12. Canvas Coordinate System

### 12.1 Absolute Positioning

All elements use absolute `Canvas.SetLeft()` and `Canvas.SetTop()` on the canvas. There is no layout manager, no grid snapping, and no snapping to other elements.

### 12.2 Arrow Geometry Bindings

| Arrow Type | Layout X Binding | Layout Y Binding |
|------------|-----------------|-----------------|
| ArrowLine | `(endX - startX) / 2 + startX` | `(endY - startY) / 2 + startY` |
| LoopArrow | `StateBox.CenterX` | `StateBox.CenterY` |

### 12.3 State Center Calculation

```
CenterX = X + Width / 2   (= X + 55)
CenterY = Y + Height / 2  (= Y + 32.5 for user, 30 for Main/Init)
```

These are computed as derived properties that update automatically when `X`, `Y`, `Width`, or `Height` changes.

---

## 13. Text Editor Integration

### 13.1 Opening State Text Editor Tabs

Double-clicking a state box triggers `OpenStateTextEditorTab(state)`:

```
1. editorFactory.CreateTextEditor(state.Text, onTextChanged: text => state.Text = text)
2. projects.Add(new LismaProjectViewModel(state.Name, state.Text))
3. Tab close: disposes the text editor instance
```

### 13.2 Opening Loop Content Editor Tabs

Double-clicking a loop arrow's arrowhead triggers `OpenLoopContentEditorTab(arrow, stateBox)`:

```
1. editorFactory.CreateTextEditor(arrow.Text, onTextChanged: text => arrow.Text = text)
2. projects.Add(new LismaProjectViewModel($"{stateBox.Name} (loop)", arrow.Text))
3. Tab close: disposes the text editor instance
```

### 13.3 Text Editor Factory

`TextEditorFactory` is a service that creates AvaloniaEdit `TextEditor` instances:

```csharp
public interface ITextEditorFactory
{
    TextEditor CreateTextEditor(string text, Action<string>? onTextChanged = null);
}
```

The implementation in `ISMA.App` wraps AvaloniaEdit and provides per-project editor instances.

---

## 14. Project Lifecycle

### 14.1 Blueprint Project Creation

1. User clicks "New statechart" toolbar button or uses File → New Statechart (Ctrl+B)
2. `ProjectService.CreateNewBlueprint("New statechart")` is called
3. A new `BlueprintProjectViewModel` is created with `BlueprintModel.Empty`
4. The project is added to `Projects` collection
5. A new tab opens in the main `TabControl` showing the editor

### 14.2 Blueprint Project Save

1. User clicks Save (Ctrl+S) or Save All
2. `project.GetBlueprintModel()` is called → `BlueprintModel`
3. The model is serialized to JSON via `System.Text.Json`
4. JSON is written to the `.scisma` file

### 14.3 Blueprint Project Load

1. User opens a `.scisma` file (File → Open, filter `*.scisma`)
2. JSON is parsed into `BlueprintModel` via `System.Text.Json`
3. `project.SetBlueprintModel(model)` rebuilds the canvas from the model

### 14.4 Snapshot (Compile)

1. At compile/verification time, `project.GetLismaText()` is called
2. This calls `BlueprintToLismaConverter.Convert(project.Blueprint)`
3. The result is the generated LISMA text sent to the server

---

## 15. Color Palette

| Element | Color | Hex |
|---------|-------|-----|
| Main state | LightGreen | `#90EE90` |
| Init state | LightBlue | `#ADD8E6` |
| User states | Coral | `#F08080` |
| Selected state | Blue tint | `#ADD8E6` (LightBlue overlay) |
| Arrow lines | Black | `#000000` |
| Arrowhead | Black | `#000000` |
| PopOver background | White | `#FFFFFF` |
| PopOver shadow | LightGray | `#D3D3D3` |

---

## 16. Dimensions Reference

| Element | Width | Height | Notes |
|---------|-------|--------|-------|
| User state box | 110 | 65 | Fixed |
| Main/Init state box | 110 | 60 | Override |
| State box corner radius | 20 | 20 | Rounded corners |
| State name font size | 12 | — | Inter |
| Arrow label font size | 12 | — | Inter |
| Arrow line stroke width | 3 | — | |
| Arrowhead stroke width | 3 | — | |
| Loop circle radius | 40 | 40 | Transparent fill |
| PopOver min width | 300 | — | |
| PopOver padding | 10 | — | All sides |
| PopOver corner radius | 5 | — | |
| PopOver shadow radius | 20 | — | |
| Line perpendicular offset | 10 | — | From state center |
| Arrow text X offset | 75 | — | Perpendicular |
| Arrow text Y offset | 50 | — | Perpendicular |
| Loop label Y offset | -10 | — | From center line |

---

## 17. Known Limitations

| Limitation | Description |
|------------|-------------|
| **No snap-to-grid** | States can be placed at arbitrary pixel coordinates |
| **No auto-routing** | Arrows are straight lines from center to center, may pass through other states |
| **No zoom/pan** | Scrolling only; no magnification |
| **No undo/redo** | All edits are permanent once committed |
| **No arrow label inline editing** | Only via PopOver; no inline editing on canvas |
| **No color customization** | All colors are hardcoded |
| **No keyboard shortcuts** | The blueprint editor itself has no keyboard shortcuts; all navigation is mouse-driven |
| **No context menu** | Right-click has no handler |
| **PopOver click-away** | Moving mouse out of the PopOver dismisses it — may accidentally dismiss if cursor slips |
| **No multi-select** | No ability to select and move multiple states simultaneously |
