using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ISMA.Domain.Dtos;
using Avalonia.Media;

namespace ISMA.App.Services;

/// <summary>
/// AvaloniaEdit DocumentColorizingTransformer that applies server-driven syntax tokens
/// as colored spans in the text editor.
///
/// Server tokens are offset-based (start + length + kind), while AvaloniaEdit's
/// IHighlightingDefinition is regex-based. This transformer bridges the gap by
/// directly modifying the TextRunProperties of VisualLineText elements to match
/// the server-provided token colors.
/// </summary>
public class ServerDrivenHighlightingTransformer : DocumentColorizingTransformer
{
    private readonly IReadOnlyList<SyntaxTokenDto> _tokens;

    public ServerDrivenHighlightingTransformer(IReadOnlyList<SyntaxTokenDto> tokens)
    {
        _tokens = tokens ?? Array.Empty<SyntaxTokenDto>();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_tokens.Count == 0)
            return;

        var lineStart = line.Offset;
        var lineEnd = lineStart + line.Length;

        var relevantTokens = _tokens
            .Where(t => t.Start >= lineStart && t.Start + t.Length <= lineEnd)
            .OrderBy(t => t.Start)
            .ToList();

        if (relevantTokens.Count == 0)
            return;

        foreach (var token in relevantTokens)
        {
            var color = GetColorForKind(token.Kind);
            ChangeLinePart(token.Start, token.Start + token.Length, visualLineElement =>
            {
                var props = visualLineElement.TextRunProperties as VisualLineElementTextRunProperties;
                props?.SetForegroundBrush(color);
            });
        }
    }

    private Avalonia.Media.IBrush GetColorForKind(SyntaxTokenKind kind)
    {
        return kind switch
        {
            SyntaxTokenKind.Keyword => Avalonia.Media.Brushes.Orange,
            SyntaxTokenKind.Comment => Avalonia.Media.Brushes.Gray,
            SyntaxTokenKind.Number => Avalonia.Media.Brushes.Blue,
            SyntaxTokenKind.Text => Avalonia.Media.Brushes.Green,
            _ => Avalonia.Media.Brushes.Black
        };
    }
}
