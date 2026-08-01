using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ISMA.App.Services;

public class TextEditorFactory : ITextEditorFactory
{
    private readonly ConcurrentDictionary<TextEditor, EventHandler> _eventHandlers = new();

    public object CreateTextEditor(string text, Action<string>? onTextChanged, string? highlightingDefinitionName = null)
    {
        var editor = new TextEditor
        {
            FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New"),
            FontSize = 12,
            ShowLineNumbers = true,
            Background = Brushes.White,
            Foreground = Brushes.Black
        };

        if (!string.IsNullOrEmpty(highlightingDefinitionName))
        {
            editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(highlightingDefinitionName);
        }

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
        if (editor is not TextEditor te) return;
        te.Options.HighlightCurrentLine = true;

        var fallback = LismaSyntaxHelper.GetFallbackHighlighting();

        te.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList()
            .ForEach(t => te.TextArea.TextView.LineTransformers.Remove(t));

        if (tokens != null && tokens.Length > 0)
        {
            var transformer = new ServerDrivenHighlightingTransformer(tokens);
            te.TextArea.TextView.LineTransformers.Insert(0, transformer);
            te.SyntaxHighlighting = fallback;
        }
        else
        {
            te.SyntaxHighlighting = fallback;
        }
    }

    public void AddSearchPanel(object editor)
    {
        if (editor is not TextEditor te) return;
        // SearchPanel.Install(te); // Disabled for now
    }

    public void AddLineNumberMargin(object editor)
    {
        // Line numbers are already enabled via ShowLineNumbers property
    }

    public void DisposeInstance(object editor)
    {
        if (editor is not TextEditor te) return;

        if (_eventHandlers.TryRemove(te, out var handler))
        {
            te.TextChanged -= handler;
        }

        te.SyntaxHighlighting = null;
    }
}
