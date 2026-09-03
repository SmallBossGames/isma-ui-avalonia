using System;
using Avalonia.Controls;
using ISMA.Domain.Contracts;
using ISMA.TextEditor;

namespace ISMA.App.Services;

/// <summary>
/// Implements the blueprint editor's text-editor seam: creates
/// <see cref="IsmaTextEditor"/> instances and wires server-driven LISMA syntax
/// highlighting on text change.
/// </summary>
public sealed class BlueprintTextEditorFactory : ISMA.BlueprintEditor.Services.ITextEditorFactory
{
    private readonly ISyntaxHighlighter _syntaxHighlighter;

    public BlueprintTextEditorFactory(ISyntaxHighlighter syntaxHighlighter)
    {
        _syntaxHighlighter = syntaxHighlighter;
    }

    public ISMA.BlueprintEditor.Services.ITextEditor CreateEditor() => new IsmaTextEditorAdapter(_syntaxHighlighter);

    private sealed class IsmaTextEditorAdapter : ISMA.BlueprintEditor.Services.ITextEditor
    {
        private readonly IsmaTextEditor _editor = new();
        private readonly ISyntaxHighlighter? _highlighter;
        private int _highlightVersion;

        public IsmaTextEditorAdapter(ISyntaxHighlighter? highlighter)
        {
            _highlighter = highlighter;
            _editor.TextChanged += OnEditorTextChanged;
        }

        public Control Node => _editor;

        public string Text
        {
            get => _editor.Text;
            set => _editor.Text = value;
        }

        public event EventHandler? TextChanged;

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            TextChanged?.Invoke(this, EventArgs.Empty);

            if (_highlighter is null)
                return;

            var highlighter = _highlighter;
            var version = ++_highlightVersion;
            var source = _editor.Text;
            _ = UpdateHighlightingAsync(version, source, highlighter);
        }

        private async Task UpdateHighlightingAsync(int version, string source, ISyntaxHighlighter highlighter)
        {
            try
            {
                var tokens = await highlighter.Highlight(source);
                if (version != _highlightVersion)
                    return;
                _editor.SetServerDrivenHighlighting(tokens, source);
            }
            catch
            {
                // Highlighting is best-effort; never break editing.
            }
        }

        public void Dispose()
        {
            _editor.TextChanged -= OnEditorTextChanged;
            _editor.Dispose();
        }
    }
}
