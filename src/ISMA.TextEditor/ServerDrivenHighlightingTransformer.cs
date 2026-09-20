using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ISMA.Domain.Dtos;

namespace ISMA.TextEditor;

/// <summary>
/// AvaloniaEdit DocumentColorizingTransformer that applies server-driven syntax tokens
/// as colored spans in the text editor, matching the original app's style classes:
/// keyword = orange bold, comment = gray italic, number = blue, default = black.
///
/// Server tokens are line-based (line + startChar + length + kind), while
/// AvaloniaEdit's IHighlightingDefinition is regex-based. This transformer bridges
/// the gap by directly modifying the TextRunProperties of VisualLineText elements
/// to match the server-provided token colors.
///
/// LSP token lines are 0-based, whereas <see cref="DocumentLine.LineNumber"/> is
/// 1-based — the two are reconciled in <see cref="ColorizeLine"/>.
///
/// The tokens are a snapshot computed for a specific document state. Because
/// AvaloniaEdit runs transformers during asynchronous layout passes, the document
/// may have changed by the time a line is colorized. The transformer therefore
/// verifies the document length and clamps every token to its line bounds, so a
/// stale snapshot degrades to missing highlighting instead of an out-of-range
/// exception in <see cref="DocumentColorizingTransformer.ChangeLinePart"/>.
/// </summary>
public class ServerDrivenHighlightingTransformer(IReadOnlyList<SyntaxTokenDto> tokens, TextDocument document, int expectedTextLength) : DocumentColorizingTransformer
{
    protected override void ColorizeLine(DocumentLine line)
    {
        // The tokens were computed for a document of a specific length. If the
        // document has changed since, the offsets no longer line up — bail out
        // rather than apply stale (potentially out-of-range) offsets.
        if (document.TextLength != expectedTextLength)
            return;

        // LSP token lines are 0-based; DocumentLine.LineNumber is 1-based.
        var lineIndex = line.LineNumber - 1;

        var relevantTokens = tokens
            .Where(t => t.Line == lineIndex)
            .OrderBy(t => t.StartChar)
            .ToList();

        if (relevantTokens.Count == 0)
            return;

        var lineStart = line.Offset;
        var lineEnd = lineStart + line.Length;

        foreach (var token in relevantTokens)
        {
            var start = lineStart + token.StartChar;
            var end = start + token.Length;

            // Defensive clamp: ChangeLinePart throws if the offsets leave the line.
            if (start < lineStart)
                start = lineStart;
            if (end > lineEnd)
                end = lineEnd;
            if (start >= end)
                continue;

            var (color, weight, style) = GetStyleForKind(token.Kind);
            ChangeLinePart(start, end, visualLineElement =>
            {
                var props = visualLineElement.TextRunProperties;
                props.SetForegroundBrush(color);
                var typeface = props.Typeface;
                props.SetTypeface(new Typeface(typeface.FontFamily, style, weight, typeface.Stretch));
            });
        }
    }

    private static (IBrush Color, FontWeight Weight, FontStyle Style) GetStyleForKind(SyntaxTokenKind kind)
    {
        return kind switch
        {
            SyntaxTokenKind.Keyword => (Brushes.Orange, FontWeight.Bold, FontStyle.Normal),
            SyntaxTokenKind.Comment => (Brushes.Gray, FontWeight.Normal, FontStyle.Italic),
            SyntaxTokenKind.Number => (Brushes.Blue, FontWeight.Normal, FontStyle.Normal),
            _ => (Brushes.Black, FontWeight.Normal, FontStyle.Normal)
        };
    }
}
