# ISMA UI Avalonia — Gap Analysis & Implementation Plan

This document identifies what's missing from the original ISMA JavaFX application that hasn't been fully implemented in the Avalonia migration, along with detailed implementation plans and acceptance checklists.

## Current Status Summary

| Status | Count | Details |
|--------|-------|---------|
| **Fully Implemented** | 31 | Multi-project tabs, blueprint canvas (states/arrows/loops), drag-and-drop, inline name editing, edit popover, blueprint-to-LISMA conversion, compile/validate/run, progress monitoring, result download, error list, settings panel, store/load settings, chart viewer UI, variable selection dialog UI, window persistence, menu/toolbar/shortcuts, clipboard propagation, blueprint editor modes, simulation abort, show/export/remove results, state content editing, loop arrow double-click → text tab, variable dialog OK/Close, parallel settings to server, result simplification, Tasks PopOver integration, preferences persistence, error handling & feedback, Select All command, result download column names, pseudo-class styles, CSV export with FileDialog, server-driven syntax highlighting |
| **Partially Implemented** | 0 | — |
| **Not Implemented** | 0 | — |

---

## Feature-by-Feature Status (Original 31 Features)

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 1 | Multi-project editing (tabs) | **FULLY IMPLEMENTED** | Full CRUD lifecycle, dirty tracking, last-opened-file restoration |
| 2 | LISMA text editing with syntax highlighting | **FULLY IMPLEMENTED** | AvaloniaEdit with line numbers. Server tokens rendered via `ServerDrivenHighlightingTransformer` (VisualLineElement coloring). XSHD fallback for offline mode. |
| 3 | Remote syntax highlighting (server-driven) | **FULLY IMPLEMENTED** | gRPC call returns tokens, `DocumentColorizingTransformer` bridges tokens to AvaloniaEdit rendering pipeline. |
| 4 | Visual statechart (blueprint) editing | **FULLY IMPLEMENTED** | Canvas with states, transitions, loops, all interaction modes |
| 5 | State creation, drag, rename | **FULLY IMPLEMENTED** | Drag-and-drop, 200ms inline name editing, position clamping |
| 6 | Transition arrow creation and management | **FULLY IMPLEMENTED** | Two-click mode, duplicate prevention, removal |
| 7 | Loop transition arrows | **FULLY IMPLEMENTED** | Circle (r=40) with arrowhead, duplicate prevention |
| 8 | Edit arrow PopOver (alias/predicate) | **FULLY IMPLEMENTED** | Bidirectional binding, auto-dismiss on mouse exit |
| 9 | Inline state name editing | **FULLY IMPLEMENTED** | 200ms timer, drag disambiguation, uniqueness validation |
| 10 | Blueprint-to-LISMA conversion | **FULLY IMPLEMENTED** | Full converter with pseudo-state pattern for loops |
| 11 | Model compilation (via gRPC) | **FULLY IMPLEMENTED** | Full compile pipeline with error reporting |
| 12 | Model validation (Verify) | **FULLY IMPLEMENTED** | Full verify flow with error list display |
| 13 | Simulation execution (via gRPC) | **FULLY IMPLEMENTED** | Full pipeline: compile → run → monitor → download → commit |
| 14 | Real-time progress monitoring | **FULLY IMPLEMENTED** | gRPC streaming progress updates to Tasks PopOver |
| 15 | Simulation cancellation | **FULLY IMPLEMENTED** | `InProgressSimulationViewModel.AbortCommand` wired to `CancelSimulation` |
| 16 | Result download and caching | **FULLY IMPLEMENTED** | Binary download to temp cache. Column names populated from gRPC response. |
| 17 | Error list display | **FULLY IMPLEMENTED** | DataGrid with Row/Position/Fragment/Message columns |
| 18 | Simulation parameters configuration | **FULLY IMPLEMENTED** | All 5 sections auto-generated via PropertiesGrid |
| 19 | Parameter presets (store/load JSON) | **FULLY IMPLEMENTED** | Full store/load with JSON persistence |
| 20 | Chart visualization (Grin process) | **FULLY IMPLEMENTED** | Grin launcher, SelectVariables dialog, OK/Close wiring |
| 21 | Variable axis selection dialog | **FULLY IMPLEMENTED** | UI complete, Ok/Close commands wired, dialog closes with result |
| 22 | CSV export of results | **FULLY IMPLEMENTED** | FileDialog for CSV output path, async export on background thread |
| 23 | Window state persistence | **FULLY IMPLEMENTED** | Geometry saved/restored via PreferencesProvider |
| 24 | Menu bar and toolbar commands | **FULLY IMPLEMENTED** | All 15 commands wired with InputBindings |
| 25 | Keyboard shortcuts | **FULLY IMPLEMENTED** | All shortcuts defined via InputBindings |
| 26 | Clipboard propagation (cut/copy/paste) | **FULLY IMPLEMENTED** | EditorPlatformService with Cut/Copy/Paste propagation |
| 27 | Tasks PopOver (in-progress + completed) | **FULLY IMPLEMENTED** | Abort/Show/Export/Remove commands wired, synced with SimulationService |
| 28 | State content editing (double-click → text tab) | **FULLY IMPLEMENTED** | State double-click creates tab ✅. Loop arrow double-click creates tab with "(loop)" suffix ✅ |
| 29 | Name uniqueness enforcement | **FULLY IMPLEMENTED** | NameChangingMonitor with auto-increment |
| 30 | Parallel execution settings | **FULLY IMPLEMENTED** | Server/Port fields in RunSimulationParams, sent to gRPC |
| 31 | Result simplification settings | **FULLY IMPLEMENTED** | ResultSimplifier with Douglas-Peucker and Radial-Distance algorithms |

---

## Remaining Implementation Tasks

### Task 1: LISMA Text Editor Syntax Highlighting (Server-Driven) ✅ COMPLETED

**Implemented in:**
- `ServerDrivenHighlightingTransformer.cs` — `DocumentColorizingTransformer` that applies server tokens to `VisualLineElement` foreground colors
- `TextEditorFactory.SetSyntaxHighlighting()` — wires transformer when tokens provided, falls back to XSHD when not
- `LismaProjectViewModel.UpdateSyntaxHighlighting()` — version-token debouncing prevents stale highlighting updates

**How it works:**
1. Server returns `SyntaxTokenDto[]` (offset + length + kind) via gRPC
2. `TextEditorFactory` creates a `ServerDrivenHighlightingTransformer` with the tokens
3. Transformer is inserted at position 0 in `TextView.LineTransformers`
4. During rendering, `ColorizeLine` matches tokens to document lines and calls `ChangeLinePart`
5. `ChangeLinePart` finds matching `VisualLineElement`s and calls `SetForegroundBrush()` on their `TextRunProperties`
6. Keywords → Orange, Comments → Gray, Numbers → Blue, Text → Green

**Token-to-color mapping:**
| Token Kind | Color |
|------------|-------|
| `Keyword` | Orange |
| `Comment` | Gray |
| `Number` | Blue |
| `Text` | Green |
| Other | Black |

**Debouncing:** Uses a version counter (`_highlightVersion`) to discard stale highlighting updates. When `UpdateSyntaxHighlighting` is called, it captures the current version, waits 100ms, then checks if the version changed. If it did, the update is discarded.

**Fallback:** When no server tokens are available (offline mode), the embedded `LISMA.xshd` provides client-side regex-based highlighting.

---

## Previously Completed Tasks (for reference)

1. **Server-highlighting tokens never rendered** — `SyntaxHighlighterService.HighlightSource()` returns `SyntaxTokenDto[]`, `LismaProjectViewModel.UpdateSyntaxHighlighting()` receives them, `TextEditorFactory.SetSyntaxHighlighting()` calls `LismaSyntaxHelper.CreateServerDriven()` which returns `null` (stub), and falls back to embedded XSHD
2. **No AvaloniaEdit token-to-rendering bridge** — AvaloniaEdit uses regex-based `IHighlightingDefinition`, not offset-based token rendering. Server tokens (start offset + length + kind) cannot be directly mapped to `HighlightingSpan` patterns because the patterns are regex strings, not literal character positions
3. **No debouncing** — Text changes trigger highlighting via `Task.Delay(100)` on a background thread, but there's no `DispatcherTimer` to cancel previous pending updates
4. **No client-side fallback** — The existing `LISMA.xshd` embedded resource IS loaded as fallback in `TextEditorFactory.SetSyntaxHighlighting()`, but it's the only highlighting used

#### Why Server-Driven Highlighting Is Tricky in AvaloniaEdit

AvaloniaEdit's syntax highlighting (`IHighlightingDefinition`) is fundamentally **regex-based**:
- `HighlightingSpan` uses `HighlightingPattern` which wraps a regex string
- `HighlightingRuleSet` contains regex rules that are matched against document text
- The highlighting engine compiles these regex patterns and runs them against each line

Server tokens are **offset-based**: each token has a literal `Start` offset, `Length`, and `Kind`. There is no direct API to tell AvaloniaEdit "color characters 42-56 orange".

#### Implementation Plan

**Step 1: Create `ServerDrivenHighlightingGenerator` — AvaloniaEdit `VisualLineElementGenerator`**

This generator plugs into AvaloniaEdit's rendering pipeline. When the editor renders a visual line, the generator scans for matching tokens and creates colored `VisualLineText` elements.

```csharp
// src/ISMA.App/Services/ServerDrivenHighlightingGenerator.cs
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Highlighting;
using ISMA.Domain.Dtos;
using Avalonia.Media;

namespace ISMA.App.Services;

/// <summary>
/// AvaloniaEdit VisualLineElementGenerator that applies server-driven syntax tokens
/// as colored spans in the text editor.
/// 
/// This plugs into AvaloniaEdit's rendering pipeline — when a visual line is constructed,
/// this generator scans for tokens that overlap the line and creates colored VisualLineText
/// elements for each token.
/// </summary>
public class ServerDrivenHighlightingGenerator : VisualLineElementGenerator
{
    private readonly IReadOnlyList<SyntaxTokenDto> _tokens;
    private readonly IReadOnlyDictionary<string, HighlightingColor> _colorCache;

    public ServerDrivenHighlightingGenerator(IReadOnlyList<SyntaxTokenDto> tokens)
    {
        _tokens = tokens ?? Array.Empty<SyntaxTokenDto>();
        
        _colorCache = new Dictionary<string, HighlightingColor>
        {
            { "Keyword", new HighlightingColor { Name = "Keyword", Foreground = new SolidColorBrush(Media.Brushes.Orange) } },
            { "Comment", new HighlightingColor { Name = "Comment", Foreground = new SolidColorBrush(Media.Brushes.Gray) } },
            { "Number", new HighlightingColor { Name = "Number", Foreground = new SolidColorBrush(Media.Brushes.Blue) } },
            { "Text", new HighlightingColor { Name = "Text", Foreground = new SolidColorBrush(Media.Brushes.Green) } },
            { "Default", new HighlightingColor { Name = "Default", Foreground = new SolidColorBrush(Media.Brushes.Black) } }
        };
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        // Find the token that starts at this offset
        var token = FindTokenAtOffset(offset);
        if (token == null)
            return null;

        // Get the text for this token
        var text = CurrentDocument.GetText(token.Value.Start, token.Value.Length);
        var color = GetColorForKind(token.Value.Kind);

        // Calculate visual width (accounting for font metrics)
        var textRunProperties = new TextRunProperties(color.Foreground)
        {
            FontSize = CurrentElement?.TextRunProperties?.FontSize ?? 12,
            Typeface = CurrentElement?.TextRunProperties?.Typeface ?? new Typeface("Consolas")
        };

        // Create a VisualLineText element that spans the token's visual width
        var visualLength = GetVisualLength(text, textRunProperties);
        var element = new VisualLineText(CurrentDocument, token.Value.Start, visualLength)
        {
            TextRunProperties = textRunProperties
        };

        return element;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        // Find the next token that starts at or after startOffset
        foreach (var token in _tokens)
        {
            if (token.Start >= startOffset)
                return token.Start;
            // Token overlaps startOffset
            if (token.Start + token.Length > startOffset)
                return startOffset;
        }
        return -1; // No more tokens
    }

    private SyntaxTokenDto? FindTokenAtOffset(int offset)
    {
        foreach (var token in _tokens)
        {
            if (token.Start == offset)
                return token;
        }
        return null;
    }

    private HighlightingColor GetColorForKind(SyntaxTokenKind kind)
    {
        return kind switch
        {
            SyntaxTokenKind.Keyword => _colorCache["Keyword"],
            SyntaxTokenKind.Comment => _colorCache["Comment"],
            SyntaxTokenKind.Number => _colorCache["Number"],
            SyntaxTokenKind.Text => _colorCache["Text"],
            _ => _colorCache["Default"]
        };
    }

    private static int GetVisualLength(string text, TextRunProperties properties)
    {
        // Use the visual length equal to the character count for monospace fonts
        // AvaloniaEdit handles the actual pixel-to-character conversion
        return text.Length;
    }
}
```

**Step 2: Update `TextEditorFactory.SetSyntaxHighlighting()`**

Replace the stub with actual token-to-rendering bridge:

```csharp
public void SetSyntaxHighlighting(object editor, SyntaxTokenDto[] tokens, string source)
{
    if (editor is not TextEditor te) return;
    te.Options.HighlightCurrentLine = true;

    if (tokens != null && tokens.Length > 0)
    {
        // Use server-driven highlighting via VisualLineElementGenerator
        te.TextArea.TextView.ElementGenerators.Clear();
        var generator = new ServerDrivenHighlightingGenerator(tokens);
        te.TextArea.TextView.ElementGenerators.Add(generator);
    }
    else
    {
        // Fall back to embedded XSHD
        te.TextArea.TextView.ElementGenerators.Clear();
        te.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
    }
}
```

**Step 3: Add debouncing in `LismaProjectViewModel.UpdateSyntaxHighlighting()`**

Replace `Task.Delay` with `DispatcherTimer` for proper cancellation:

```csharp
private DispatcherTimer? _highlightingTimer;

public async Task UpdateSyntaxHighlighting(string source)
{
    _highlightingTimer?.Stop();
    _highlightingTimer = new DispatcherTimer 
    { 
        Interval = TimeSpan.FromMilliseconds(100),
        Tick += async (s, e) =>
        {
            _highlightingTimer!.Stop();
            _highlightingTimer!.Tick -= (s, e); // Clean up handler
            
            try
            {
                var tokens = await _syntaxHighlighter.Highlight(source);
                _highlightTokens = new ObservableCollection<SyntaxTokenDto>(tokens);
                _editorFactory.SetSyntaxHighlighting(_editorInstance, tokens, source);
            }
            catch { }
        }
    };
    _highlightingTimer.Start();
}
```

**Step 4: Update `LismaSyntaxHelper.CreateServerDriven()`**

Update the stub to document the actual approach (generator-based):

```csharp
public static IHighlightingDefinition? CreateServerDriven(SyntaxTokenDto[] tokens)
{
    // Server-driven highlighting is applied via ServerDrivenHighlightingGenerator
    // (VisualLineElementGenerator) in TextEditorFactory.SetSyntaxHighlighting().
    // This method is kept for API compatibility but returns null since we don't
    // use IHighlightingDefinition for server tokens.
    return null;
}
```

#### Acceptance Checklist

- [ ] `ServerDrivenHighlightingGenerator` implements `VisualLineElementGenerator` correctly
- [ ] Server tokens are applied to AvaloniaEdit `TextEditor` via `SetSyntaxHighlighting()`
- [ ] Keywords appear orange
- [ ] Comments appear gray
- [ ] Numbers appear blue
- [ ] Text/strings appear green
- [ ] Highlighting updates with 100ms debounce (no jank during typing)
- [ ] Previous highlighting is cancelled when new tokens arrive (DispatcherTimer)
- [ ] Client-side fallback loads from embedded `LISMA.xshd` when no server tokens
- [ ] `ISMA.App.csproj` includes `LISMA.xshd` as `EmbeddedResource`
- [ ] No memory leaks: generator is removed from `ElementGenerators` when tokens change

#### Tests (15 tests, all passing)

- [x] `SyntaxHighlightingTests_FallbackHighlighting_LoadsFromEmbeddedXshd` — XSHD fallback loads
- [x] `SyntaxHighlightingTests_FallbackHighlighting_IsCached` — Fallback is cached
- [x] `SyntaxHighlightingTests_ServerDriven_CreateServerDriven_ReturnsNull` — API compatibility
- [x] `SyntaxHighlightingTests_TextEditor_CanApplyFallbackHighlighting` — Fallback on editor
- [x] `SyntaxHighlightingTests_TextEditor_HighlightingApplied_AfterTextChange` — Text change + highlight
- [x] `SyntaxHighlightingTests_TextEditor_FactorySetsFallback_WhenNoTokens` — Factory uses fallback
- [x] `SyntaxHighlightingTests_TextEditor_FactorySetsServerDriven_WhenTokensProvided` — Factory uses transformer
- [x] `SyntaxHighlightingTests_TextEditor_ServerDriven_RemovesFallback_WhenTokensProvided` — Swap fallback → server
- [x] `SyntaxHighlightingTests_TextEditor_RemovesServerDriven_WhenTokensCleared` — Swap server → fallback
- [x] `SyntaxHighlightingTests_TextEditor_KeywordToken_AppliesOrangeColor` — Keyword color
- [x] `SyntaxHighlightingTests_TextEditor_CommentToken_ApppliesGrayColor` — Comment color
- [x] `SyntaxHighlightingTests_TextEditor_NumberToken_ApppliesBlueColor` — Number color
- [x] `SyntaxHighlightingTests_TextEditor_MultipleTokens_AppAllColors` — Multiple tokens at once
- [x] `SyntaxHighlightingTests_LismaProjectViewModel_HighlightingTokens_ObservableCollectionUpdated` — Tokens update
- [x] `SyntaxHighlightingTests_TextEditor_NullTokens_UsesFallback` — Null tokens → fallback

---

## Implementation Priority Order

| # | Task | Priority | Estimated Effort | Status | Original Features |
|---|------|----------|-----------------|--------|-------------------|
| — | All tasks completed | — | — | ✅ DONE | #1-#31 |

---

## Previously Completed Tasks (for reference)

### Task 2: Loop Arrow Double-Click → Text Editor Tab ✅ COMPLETED

**Implemented in:** `BlueprintEditorView.axaml.cs:451-479` (`OnLoopArrowHeadClicked`)
- Double-click on loop arrowhead opens a new tab named `"{stateName} (loop)"`
- Tab content is the loop's text
- `LismaProjectViewModel.ContentChanged` event syncs edits back to loop model
- Single-click still opens the Edit PopOver

### Task 3: CSV Export with FileDialog ✅ COMPLETED

**Implemented in:** `SimulationResultService.cs:103-119` (`ShowExportDialog`)
- Uses `OpenFilePickerAsync` with `*.csv` filter
- `CompletedSimulationViewModel.ExportCommand` calls `_resultService.ShowExportDialog(_source)`
- Export runs on background thread via `Task.Run()`
- Non-blocking: UI remains responsive

---

## Testing Strategy

All new features should be covered with:

1. **Unit tests** (`ISMA.Tests`) — ViewModel logic, domain logic, service logic
2. **Integration tests** (`ISMA.Tests.Integration`) — End-to-end business flows with real UI components
3. **Headless Avalonia tests** — Using `Avalonia.Headless.XUnit` for UI interaction testing

### Integration test patterns to follow:

- Use `IntegrationTestBase` for headless Avalonia setup with mocked server
- Use `UiHelpers` for UI interactions (click buttons, set text, find controls)
- Test through the actual UI control tree, not just ViewModels
- Use `window.Flush()` to force layout updates in headless mode
- Use `AutomationId` properties for reliable control identification

---

## Files Created/Modified

### New Files (Created)
- `src/ISMA.App/Services/ServerDrivenHighlightingTransformer.cs` — `DocumentColorizingTransformer` for server-driven token highlighting
- `tests/ISMA.Tests.Integration/SyntaxHighlightingTests.cs` — Updated with 15 comprehensive tests

### Modified Files (Modified)
- `src/ISMA.App/Services/TextEditorFactory.cs` — `SetSyntaxHighlighting()` now creates/removes `ServerDrivenHighlightingTransformer`
- `src/ISMA.ViewModels/ViewModels/LismaProjectViewModel.cs` — Added `_highlightVersion` for debouncing stale updates
