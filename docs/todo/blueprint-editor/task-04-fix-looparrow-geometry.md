# Task 4 — Fix LoopArrow.cs geometry and styling

## Problem

`LoopArrow.cs` deviates from the spec in multiple rendering aspects:

1. **Circle center position**: Spec says `LOOP_CIRCLE_CENTER_X = 60` (offset within arrow's local coordinate space). Current code uses `center.X + r` where r=40, so offset is 40, not 60.
2. **Arrowhead position**: Spec says `LOOP_ARROWHEAD_X = 100` (at right side of circle). Current code places arrowhead at 135 degrees (top-left) of circle.
3. **Arrowhead shape**: Spec says `Polygon(0,-7 / 7,0 / -7,0)` — points right. Current code uses a generic rotated polygon.
4. **Label position**: Spec says X=120 (right of circle), Y offset=-10. Current code uses `circleCenter.X + r + 10` and `circleCenter.Y - 5`.
5. **Label font**: Spec says Arial 16pt. Current code uses 11pt.
6. **Stroke width**: Spec says 3.0. Current code uses 1.5.
7. **Arrow geometry binding**: Spec says `LayoutX = stateBox.centerXProperty()` and `LayoutY = stateBox.centerYProperty()`. Current control is rendered via `Render()` with absolute positioning computed from state center, but the Canvas positioning is not bound to state centers.
8. **Loop arrow Canvas positioning**: The AXAML uses `ItemsControl` on a `Canvas` but the `LoopArrow` control's position is not set via `Canvas.Left`/`Canvas.Top` — it relies on `Render()` drawing at an offset from the state center.

## Expectations

- Circle center at (stateCenterX + 60, stateCenterY - 40) in the arrow's local coordinate space
- Arrowhead at position (100, 0) relative to circle center, pointing right
- Arrowhead shape is `Polygon(0,-7, 7,0, -7,0)` pointing right
- Label at (120, -10) relative to circle center
- Label font size is 16pt
- Stroke width is 3.0
- Loop arrow is positioned on Canvas at the state's center point

## Implementation

1. Update constants: `LoopRadius = 40.0`, `LoopCircleCenterX = 60.0`, `LoopArrowheadX = 100.0`, `LoopLabelX = 120.0`, `LoopLabelYOffset = -10.0`, `ArrowheadSize = 7.0`, `StrokeWidth = 3.0`, `LabelFontSize = 16`
2. Rewrite `Render()`:
   - Circle center: `new Point(center.X + LoopCircleCenterX, center.Y - LoopRadius)`
   - Arrowhead: at `new Point(center.X + LoopArrowheadX, center.Y + LoopLabelYOffset)`, pointing right
   - Draw arrowhead as right-pointing triangle: `Polygon(0,-7, 7,0, -7,0)` translated to arrowhead position
   - Label: at `new Point(center.X + LoopLabelX, center.Y + LoopLabelYOffset)`
3. Update `DrawArrowhead()` to draw right-pointing triangle
4. Update `OnPointerPressed()` hit testing for new arrowhead and circle positions
5. In AXAML data template, set `Canvas.Left` and `Canvas.Top` on the `LoopArrow` to bind to state center position

## Tests

- Integration: `BlueprintCanvasUiTests` — verify loop arrow circle position relative to state
- Integration: `BlueprintCanvasUiTests` — verify arrowhead points right
- Integration: `BlueprintCanvasUiTests` — verify label position
- Integration: `BlueprintCanvasUiTests` — verify stroke width is 3.0

## Acceptance Criteria

- [ ] Circle center offset is (60, -40) from state center
- [ ] Arrowhead at (100, 0) relative to circle, pointing right
- [ ] Arrowhead shape is right-pointing triangle per spec
- [ ] Label at (120, -10) relative to circle center
- [ ] Label font size is 16pt
- [ ] Stroke width is 3.0
- [ ] Hit testing works correctly
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
