# Task 03: Icon Toolbar Buttons

**Scope:** `src/ISMA.App/Views/IsmaToolBarView.axaml`
**Effort:** Low
**Risk:** Very low

## Problem Description

The toolbar uses text buttons with labels like "New Text", "Open", "Save". The Kotlin spec defines Material Design glyphs for each button (e.g. `add_circle_outline`, `folder_open`, `save`, `check_circle`, `bookmark`). The project already uses icons in the MenuBar. The toolbar should use icon-only buttons with tooltips, matching the spec's compact design.

## Expectations

- All toolbar buttons display Material Design icons instead of text labels
- Each button has a tooltip matching the spec's tooltip definitions
- Button layout matches the spec's separator placement
- Buttons maintain their existing command bindings
- The toolbar is more compact (no text labels = narrower buttons)

## Implementation

### Key Design Decisions

- Check what icon library is already available in the project (Avalonia.MaterialDesign, Avalonia.Fonts.Inter, or embedded glyphs)
- If no icon library is present, use Unicode characters or embedded SVG icons as a fallback
- Use `Button` with `Content` set to the icon glyph, `ToolTip.Tip` for the tooltip text
- Keep separators (`Separator` controls) at the same positions as the spec

### Implementation Steps

1. **Identify available icon source** — check `Directory.Packages.props` and `src/ISMA.App/` for any icon packages (e.g. `Avalonia.MaterialDesign`, `Avalonia.Fonts.MaterialDesign`, `AvaloniaSVG`, etc.). Also check `GlobalStyles.axaml` for existing icon resources.

2. **If no icon package exists**, add `Avalonia.Fonts.MaterialDesign` to the project (or use Unicode fallback glyphs for the Material Design icon set).

3. **Rewrite `IsmaToolBarView.axaml`** — replace text button content with icon glyphs:
   - New model: `add_circle_outline`
   - New statechart: `add_box`
   - Open: `folder_open`
   - Save: `save`
   - Save all: `save_alt`
   - *(separator)*
   - Cut: `content_cut`
   - Copy: `content_copy`
   - Paste: `content_paste`
   - *(separator)*
   - Verify: `check_circle`
   - *(separator)*
   - Store Settings: `bookmark`
   - Load Settings: `bookmark_border`

4. **Add tooltips** — each button gets a `ToolTip.Tip` matching the spec:
   - "New model", "New statechart", "Open model", "Save current model", "Save all models"
   - "Cut", "Copy", "Paste"
   - "Verify"
   - "Store Settings", "Load Settings"

5. **Style buttons** — ensure icon buttons have appropriate sizing (e.g. `Fontsize="16"` or `Width="32"` for consistent icon buttons). Add hover/pressed styles if not already in GlobalStyles.

### High-Level Description

**Existing code:** Text buttons in IsmaToolBarView.axaml with labels like "New Text", "Open", "Save". Command bindings exist and work.

**Target approach:** Icon-only buttons using Material Design glyphs with tooltips. Same command bindings, same separator placement. More compact layout matching the Kotlin spec.

## Tests

### Test Scenarios

1. **All buttons present** — 11 buttons + 3 separators match the spec layout
2. **Tooltips display** — hovering over each button shows the correct tooltip text
3. **Commands still fire** — all toolbar buttons execute the same commands as before
4. **Icons visible** — glyphs render correctly at the chosen font size

## Verification

- [ ] `dotnet build isma-ui-dotnet.slnx` succeeds
- [ ] `dotnet test` passes
- [ ] `grep -c "Separator" src/ISMA.App/Views/IsmaToolBarView.axaml` returns >= 3
- [ ] `grep "ToolTip.Tip" src/ISMA.App/Views/IsmaToolBarView.axaml` returns >= 11
- [ ] No text button labels remain (grep for "New Text\|Open model\|Save current" returns 0)
- [ ] `grep "Content=" src/ISMA.App/Views/IsmaToolBarView.axaml` returns icon references, not text

## Acceptance Criteria

- [ ] All 11 toolbar buttons use Material Design icon glyphs
- [ ] All 3 separators present at correct positions (after SaveAll, after Paste, after Verify)
- [ ] All 11 buttons have correct tooltips per spec
- [ ] Command bindings unchanged — all actions work identically
- [ ] Icon buttons have consistent sizing
- [ ] Build succeeds with no warnings
- [ ] All existing tests pass
