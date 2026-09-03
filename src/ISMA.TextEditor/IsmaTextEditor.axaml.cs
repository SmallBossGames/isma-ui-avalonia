using Avalonia.Controls;
using AvaloniaEdit;
using ISMA.Domain.Dtos;
using TextEditor = AvaloniaEdit.TextEditor;

namespace ISMA.TextEditor;

/// <summary>
/// The shared LISMA text editor: an AvaloniaEdit <see cref="TextEditor"/> with
/// line numbers, a cut/copy/paste/select-all context menu, and a server-driven
/// syntax highlighting API. Ported from the original ISMA text-editor module.
/// </summary>
public partial class IsmaTextEditor : UserControl, IDisposable
{
    private ServerDrivenHighlightingTransformer? _highlightingTransformer;
    private bool _updating;
    private bool _disposed;
    private readonly EventHandler _editorTextChanged;

    /// <summary>Creates the editor.</summary>
    public IsmaTextEditor()
    {
        InitializeComponent();

        _editorTextChanged = (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            TextChanged?.Invoke(this, EventArgs.Empty);
        };

        Editor.TextArea.Document.TextChanged += _editorTextChanged;
    }

    /// <summary>The inner AvaloniaEdit editor.</summary>
    public AvaloniaEdit.TextEditor Editor => InnerEditor;

    /// <summary>Raised whenever the editor text changes (user or programmatic).</summary>
    public event EventHandler? TextChanged;

    /// <summary>The editor text (two-way with the inner editor).</summary>
    public string Text
    {
        get => Editor.Text;
        set
        {
            _updating = true;
            Editor.Text = value;
            _updating = false;
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Applies server-driven syntax highlighting: replaces the previous
    /// highlighting transformer with one built from the given tokens.
    /// </summary>
    /// <param name="tokens">The server-provided syntax tokens.</param>
    /// <param name="source">The source text the tokens were computed for.</param>
    public void SetServerDrivenHighlighting(SyntaxTokenDto[] tokens, string source)
    {
        var textView = Editor.TextArea.TextView;

        if (_highlightingTransformer is { } old)
        {
            textView.LineTransformers.Remove(old);
        }

        _highlightingTransformer = new ServerDrivenHighlightingTransformer(tokens);
        textView.LineTransformers.Add(_highlightingTransformer);
        textView.Redraw();
    }

    /// <summary>Releases the editor (detaches from the inner editor).</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Editor.TextArea.Document.TextChanged -= _editorTextChanged;
        _highlightingTransformer = null;
    }
}
