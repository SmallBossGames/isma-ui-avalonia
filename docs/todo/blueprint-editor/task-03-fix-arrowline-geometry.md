# Task 3 — Fix ArrowLine.cs geometry and styling

## Problem

`ArrowLine.cs` deviates from the spec in multiple rendering aspects:

1. **Arrow line stroke**: Spec says `ARROW_LINE_STROKE = 3.0`. Current code uses `1.5`.
2. **Arrowhead stroke**: Spec says `ARROWHEAD_STROKE = 3.0`. Current code uses `1.0`.
3. **Arrow geometry**: Spec uses atan2-based perpendicular offset calculation:
   ```
   angle = atan2(x, y) + PI / 2  // perpendicular angle
   offsetX = 10.0 * sin(angle)
   offsetY = 10.0 * cos(angle)
   ```
   Current code uses simple direction-vector offset (parallel to line, not perpendicular).
4. **Arrowhead shape**: Spec says `Polygon(7.0, -7.0, -7.0, 0.0, 7.0, 7.0)` — a 14x14 isosceles triangle. Current code uses a generic rotated polygon.
5. **Label font**: Spec says Arial 16pt (`ARROW_LABEL_FONT_SIZE`). Current code uses 11pt.
6. **Label position**: Spec uses perpendicular offset from midpoint: `textOffsetX = 75.0 * sin(angle)`, `textOffsetY = 50.0 * cos(angle)`. Current code places label at the midpoint directly.
7. **Arrowhead size constant**: `ArrowheadSize = 14.0` is correct per spec (`ARROWHEAD_WIDTH = 7` means 14 total width).

## Expectations

- Arrow line rendered with stroke width 3.0
- Arrowhead rendered with stroke width 3.0, correct 14x14 isosceles triangle shape
- Line endpoints computed using atan2-based perpendicular offset
- Arrowhead rotated to match line angle correctly
- Label rendered at Arial 16pt, positioned perpendicular to line at midpoint
- Arrowhead hit testing updated to match new geometry

## Implementation

1. Update constants: `ArrowOffset = 10.0`, `ArrowheadSize = 7.0` (half-width per spec), `ArrowLineStroke = 3.0`, `ArrowheadStroke = 3.0`, `LabelFontSize = 16`
2. Rewrite `Render()`:
   - Compute `direction = end - start`, `length`, `unitDir`
   - Compute `angle = atan2(direction.X, direction.Y) + PI / 2` (perpendicular)
   - Compute `offsetX = ArrowOffset * sin(angle)`, `offsetY = ArrowOffset * cos(angle)`
   - Line: `startOffset = start + Vector(offsetX, offsetY)`, `endOffset = end - Vector(direction.X, direction.Y) * (length - ArrowOffset) / length + Vector(offsetX, offsetY)`
   - Actually, follow spec exactly: lineStartX = startX - layoutX + offsetX, etc.
   - Arrowhead: draw 14x14 isosceles triangle at endOffset, rotated to match line angle
3. Rewrite `DrawArrowhead()`: use spec polygon `Polygon(7.0, -7.0, -7.0, 0.0, 7.0, 7.0)` rotated to arrow angle
4. Rewrite label position: midpoint + perpendicular offset (75 * sin(angle), 50 * cos(angle))
5. Update `OnPointerPressed()` hit testing to use new arrowhead position calculation

## Tests

- Integration: `BlueprintCanvasUiTests` — verify arrow stroke width is 3.0 (visual check via rendering)
- Integration: `BlueprintCanvasUiTests` — verify arrow label font is 16pt
- Integration: `BlueprintCanvasUiTests` — verify arrow geometry is perpendicular-offset (arrows don't pass through state borders)

## Acceptance Criteria

- [ ] Arrow line stroke width is 3.0
- [ ] Arrowhead stroke width is 3.0
- [ ] Arrowhead shape is 14x14 isosceles triangle per spec
- [ ] Line uses atan2-based perpendicular offset
- [ ] Arrowhead rotation matches line angle
- [ ] Label font size is 16pt
- [ ] Label positioned at perpendicular offset from midpoint
- [ ] Hit testing works correctly with new geometry
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
