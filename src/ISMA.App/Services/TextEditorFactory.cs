using System;
using System.Collections.Concurrent;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.TextEditor;

namespace ISMA.App.Services;

/// <summary>
/// App-level text editor factory for LISMA text projects: creates
/// <see cref="IsmaTextEditor"/> instances and applies server-driven syntax
/// highlighting to them.
/// </summary>
public class TextEditorFactory : ITextEditorFactory
{
    private readonly ConcurrentDictionary<IsmaTextEditor, EventHandler> _eventHandlers = new();

    public object CreateTextEditor(string text, Action<string>? onTextChanged, string? highlightingDefinitionName = null)
    {
        var editor = new IsmaTextEditor();

        if (!string.IsNullOrEmpty(text))
        {
            editor.Text = text;
        }

        EventHandler handler = (s, e) =>
        {
            onTextChanged?.Invoke(editor.Text);
        };

        editor.TextChanged += handler;
        _eventHandlers.TryAdd(editor, handler);

        return editor;
    }

    public void SetSyntaxHighlighting(object editor, SyntaxTokenDto[] tokens, string source)
    {
        var inner = editor switch
        {
            IsmaTextEditor wrapper => wrapper.Editor,
            AvaloniaEdit.TextEditor textEditor => textEditor,
            _ => null
        };

        if (inner is null)
        {
            return;
        }

        // The embedded LISMA XSHD always serves as the base highlighting.
        inner.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();

        var textView = inner.TextArea.TextView;
        textView.Options.HighlightCurrentLine = true;

        var transformers = textView.LineTransformers;
        for (var i = transformers.Count - 1; i >= 0; i--)
        {
            if (transformers[i] is ServerDrivenHighlightingTransformer)
            {
                transformers.RemoveAt(i);
            }
        }

        if (tokens is { Length: > 0 })
        {
            transformers.Add(new ServerDrivenHighlightingTransformer(tokens));
        }

        textView.Redraw();
    }

    public void AddSearchPanel(object editor)
    {
        // Search panel is not part of the current editor surface.
    }

    public void AddLineNumberMargin(object editor)
    {
        // Line numbers are already enabled on IsmaTextEditor.
    }

    public void DisposeInstance(object editor)
    {
        if (editor is not IsmaTextEditor ismaEditor) return;

        if (_eventHandlers.TryRemove(ismaEditor, out var handler))
        {
            ismaEditor.TextChanged -= handler;
        }

        ismaEditor.Dispose();
    }
}
