using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ISMA.Domain.Dtos;

namespace ISMA.TextEditor;

/// <summary>
/// AvaloniaEdit DocumentColorizingTransformer that applies server-driven syntax tokens
/// as colored spans in the text editor, matching the original app's style classes:
/// keyword = orange bold, comment = gray italic, number = blue, default = black.
///
/// Server tokens are offset-based (start + length + kind), while AvaloniaEdit's
/// IHighlightingDefinition is regex-based. This transformer bridges the gap by
/// directly modifying the TextRunProperties of VisualLineText elements to match
/// the server-provided token colors.
/// </summary>
public class ServerDrivenHighlightingTransformer(IReadOnlyList<SyntaxTokenDto> tokens) : DocumentColorizingTransformer
{
    protected override void ColorizeLine(DocumentLine line)
    {
        if (tokens.Count == 0)
            return;

        var lineStart = line.Offset;
        var lineEnd = lineStart + line.Length;

        var relevantTokens = tokens
            .Where(t => t.Start >= lineStart && t.Start + t.Length <= lineEnd)
            .OrderBy(t => t.Start)
            .ToList();

        if (relevantTokens.Count == 0)
            return;

        foreach (var token in relevantTokens)
        {
            var (color, weight, style) = GetStyleForKind(token.Kind);
            ChangeLinePart(token.Start, token.Start + token.Length, visualLineElement =>
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
