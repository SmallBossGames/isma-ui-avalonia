# Task 5 — Fix EditArrowPopOverView styling and positioning

## Problem

`EditArrowPopOverView` deviates from the spec:

1. **Background**: Spec says `WHITE` fill (`#FFFFFF`). Current uses `SystemControlBackgroundChromeMediumHighBrush`.
2. **Shadow**: Spec says `DropShadow, radius 20, color LIGHTGRAY` (`#D3D3D3`). Current uses `BoxShadow="0 4 12 0 #D3D3D3"` which is close but the spec specifies radius 20.
3. **Layout**: Spec says `VBox` layout. Current uses `StackPanel` (correct for Avalonia).
4. **Padding**: Spec says 10 all sides. Current uses `Padding="10"` (correct).
5. **Corner radius**: Spec says `CornerRadii 5`. Current uses `CornerRadius="5"` (correct).
6. **Min width**: Spec says 300. Current uses `MinWidth="300"` (correct).
7. **PopOver placement**: Spec says `translateX = width / -2` (centered on click x), `translateY = y - 2` (2px above cursor). Current uses `Placement="Pointer"` with `VerticalOffset="10", HorizontalOffset="10"` — not centered, not above cursor.
8. **Data binding**: Spec says bidirectional binding between TextField and arrow data. Current uses `Mode=TwoWay` on TextBox bindings (correct).
9. **PopOver dismissal**: Spec says auto-removed on `MOUSE_EXITED`. Current has `OnPointerExited` that fires `DismissRequested` (correct).
10. **PopOver content**: Spec shows "Alias (optional)" and "Predicate" labels. Current has "Alias (optional):" and "Predicate" labels (close). Current also has OK/Cancel buttons which the spec doesn't mention — the spec uses click-away dismissal only.

## Expectations

- PopOver has white background with LIGHTGRAY DropShadow (radius 20)
- PopOver is centered horizontally on the click point, 2px above cursor
- PopOver retains OK/Cancel buttons for explicit dismissal
- Bidirectional binding works correctly for alias and predicate

## Implementation

1. In `EditArrowPopOverView.axaml`:
   - Change `Background` to `White` or `#FFFFFF`
   - Update `BoxShadow` to `0 0 20 5 #D3D3D3` (radius 20)
2. In `BlueprintEditorView.axaml.cs`:
   - Instead of using `Popup.Placement="Pointer"`, manually position the PopOver:
     - Calculate center: `popup.HorizontalOffset = -popOverView.ActualWidth / 2`
     - Calculate vertical: `popup.VerticalOffset = clickY - 2`
   - Or keep Placement="Pointer" but adjust offsets to center: `HorizontalOffset = 0` (centered by default with PlacementMode=Pointer)
3. In `BlueprintEditorView.axaml.cs` `OnArrowHeadClicked`:
   - Store the click position
   - Position the Popup manually based on click position
   - Open the PopOver

## Tests

- Integration: `BlueprintCanvasUiTests` — verify PopOver background is white
- Integration: `BlueprintCanvasUiTests` — verify PopOver is centered on click point
- Integration: `BlueprintCanvasUiTests` — verify PopOver dismisses on mouse exit
- Integration: `BlueprintCanvasUiTests` — verify alias/predicate changes propagate to arrow

## Acceptance Criteria

- [ ] PopOver background is white (#FFFFFF)
- [ ] PopOver has DropShadow with radius 20, color LIGHTGRAY
- [ ] PopOver is centered horizontally on click point
- [ ] PopOver is positioned 2px above cursor vertically
- [ ] Bidirectional binding works for alias
- [ ] Bidirectional binding works for predicate
- [ ] PopOver dismisses on mouse exit
- [ ] OK/Cancel buttons dismiss PopOver
- [ ] `dotnet build isma-ui-dotnet.slnx` passes
- [ ] `dotnet test --filter "Blueprint"` passes
