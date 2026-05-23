using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;
using ISMA.Domain.Dtos;

namespace ISMA.App.Services;

/// <summary>
/// Converts server-side syntax tokens into AvaloniaEdit text decorations.
/// Uses a TextView element generator to overlay colored spans on the text.
/// </summary>
public class ServerSideHighlightingDecorator : TextViewElementGenerator, ITextViewConnect
{
    private readonly SyntaxTokenDto[] _tokens;
    private readonly string _source;
    private TextEditor? _textEditor;
    private TextView? _textView;

    public ServerSideHighlightingDecorator(SyntaxTokenDto[] tokens, string source)
    {
        _tokens = tokens;
        _source = source;
    }

    public void Attach(TextEditor textEditor)
    {
        _textEditor = textEditor;
        textEditor.TextArea.TextView.ElementGenerators.Add(this);
    }

    public void Detach(TextEditor textEditor)
    {
        if (_textView != null)
        {
            _textView.ElementGenerators.Remove(this);
        }
        _textEditor = null;
        _textView = null;
    }

    public void Attach(TextView textView)
    {
        _textView = textView;
        textView.ElementGenerators.Add(this);
    }

    public void Detach(TextView textView)
    {
        textView.ElementGenerators.Remove(this);
    }

    public override ITextElementGenerator? GetElement(ITextRunConstructionContext context, int offset, double remainingWidth, out double generatedWidth)
    {
        var tokenIndex = FindTokenAt(offset);
        if (tokenIndex < 0 || tokenIndex >= _tokens.Length)
        {
            generatedWidth = 0;
            return null;
        }

        var token = _tokens[tokenIndex];
        var color = GetColorForTokenKind(token.Kind);

        var backgroundBrush = Avalonia.Media.SolidColorBrush.Parse(color.Background);
        var foregroundBrush = Avalonia.Media.SolidColorBrush.Parse(color.Foreground);

        var lineStart = GetCurrentLineStart(context, offset);
        var lineEnd = GetCurrentLineEnd(context, offset);
        var tokenStart = token.Start - lineStart;
        var tokenLength = Math.Min(token.Length, lineEnd - token.Start);

        if (tokenStart < 0 || tokenLength <= 0)
        {
            generatedWidth = 0;
            return null;
        }

        var segment = new TextSegment
        {
            StartOffset = token.Start,
            EndOffset = token.Start + tokenLength
        };

        var generator = new TokenHighlightGenerator(segment, foregroundBrush, backgroundBrush, _textView);
        generatedWidth = tokenLength * context.GetCharacterWidth('x');
        return generator;
    }

    private int FindTokenAt(int offset)
    {
        for (var i = _tokens.Length - 1; i >= 0; i--)
        {
            var token = _tokens[i];
            if (offset >= token.Start && offset < token.Start + token.Length)
            {
                return i;
            }
        }
        return -1;
    }

    private int GetCurrentLineStart(ITextRunConstructionContext context, int offset)
    {
        var document = _textEditor?.Document;
        if (document == null) return offset;

        var line = document.GetLineByOffset(offset);
        return line.Offset;
    }

    private int GetCurrentLineEnd(ITextRunConstructionContext context, int offset)
    {
        var document = _textEditor?.Document;
        if (document == null) return offset;

        var line = document.GetLineByOffset(offset);
        return line.Offset + line.Length;
    }

    private static (string Foreground, string Background) GetColorForTokenKind(SyntaxTokenKind kind)
    {
        return kind switch
        {
            SyntaxTokenKind.Keyword => ("#FF8C00", "Transparent"),
            SyntaxTokenKind.Comment => ("#808080", "Transparent"),
            SyntaxTokenKind.Number => ("#4169E1", "Transparent"),
            SyntaxTokenKind.Text => ("#000000", "Transparent"),
            SyntaxTokenKind.Unspecified => ("#000000", "Transparent"),
            _ => ("#000000", "Transparent")
        };
    }

    public bool IsValidForContext(ITextRunConstructionContext context)
    {
        return true;
    }

    private sealed class TokenHighlightGenerator : ITextElementGenerator
    {
        private readonly TextSegment _segment;
        private readonly Avalonia.Media.Brush _foreground;
        private readonly Avalonia.Media.Brush _background;
        private readonly TextView? _textView;

        public TokenHighlightGenerator(TextSegment segment, Avalonia.Media.Brush foreground, Avalonia.Media.Brush background, TextView? textView)
        {
            _segment = segment;
            _foreground = foreground;
            _background = background;
            _textView = textView;
        }

        public double GetHeight(ITextRunConstructionContext context, double verticalOffset, double width)
        {
            return 0;
        }

        public double GetWidth(ITextRunConstructionContext context, double verticalOffset, double width)
        {
            return 0;
        }

        public int GetNextSegmentEnd(int currentOffset, out TextDecorationLocation nextDecorationOffset)
        {
            nextDecorationOffset = TextDecorationLocation.After;
            return _segment.EndOffset;
        }

        public TextRun CreateTextRun(int startOffset, ITextRunConstructionContext context, TextDecorationLocation decorationLocation)
        {
            if (decorationLocation != TextDecorationLocation.Before && decorationLocation != TextDecorationLocation.Inside)
            {
                return new UnknownTextRun(startOffset, null);
            }

            if (startOffset < _segment.StartOffset || startOffset >= _segment.EndOffset)
            {
                return new UnknownTextRun(startOffset, null);
            }

            var runLength = Math.Min(_segment.EndOffset, startOffset + 1) - startOffset;

            if (_background != null && _background != Avalonia.Media.Brushes.Transparent)
            {
                return new BackgroundPrimitiveRun(
                    new Avalonia.Media.SolidColorBrush(_background),
                    runLength);
            }

            return new UnknownTextRun(startOffset, null);
        }
    }
}
