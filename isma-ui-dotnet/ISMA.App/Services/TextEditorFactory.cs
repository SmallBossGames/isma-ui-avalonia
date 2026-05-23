using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;

namespace ISMA.App.Services;

public class TextEditorFactory : ITextEditorFactory
{
    private static bool _highlightingRegistered;

    public object CreateTextEditor(string text, Action<string>? onTextChanged, string? highlightingDefinitionName = null)
    {
        RegisterLismaHighlighting();

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

        editor.TextChanged += (s, e) =>
        {
            onTextChanged?.Invoke(editor.Text);
        };

        return editor;
    }

    public void SetSyntaxHighlighting(object editor, SyntaxTokenDto[] tokens, string source)
    {
        if (editor is not TextEditor te) return;

        te.Options.HighlightCurrentLine = true;

        if (tokens == null || tokens.Length == 0)
        {
            RemoveDecorator(te);
            return;
        }

        var existing = te.Tag as ServerSideHighlightingDecorator;
        if (existing != null)
        {
            existing.Detach(te);
        }

        var decorator = new ServerSideHighlightingDecorator(tokens, source);
        decorator.Attach(te);
        te.Tag = decorator;
    }

    public void AddSearchPanel(object editor)
    {
        if (editor is not TextEditor te) return;
        AvaloniaEdit.Search.SearchPanel.Install(te);
    }

    public void AddLineNumberMargin(object editor)
    {
        if (editor is not TextEditor te) return;
        // Line numbers are already enabled via ShowLineNumbers property
    }

    public void DisposeInstance(object editor)
    {
        if (editor is TextEditor te)
        {
            RemoveDecorator(te);
            te.Text = "";
            te.SyntaxHighlighting = null;
        }
    }

    private static void RemoveDecorator(TextEditor te)
    {
        var existing = te.Tag as ServerSideHighlightingDecorator;
        if (existing != null)
        {
            existing.Detach(te);
            te.Tag = null;
        }
    }

    private static void RegisterLismaHighlighting()
    {
        if (_highlightingRegistered) return;
        _highlightingRegistered = true;

        try
        {
            var assembly = typeof(TextEditorFactory).Assembly;
            using var stream = assembly.GetManifestResourceStream("ISMA.App.Assets.LISMA.xshd");
            if (stream != null)
            {
                using var reader = new System.IO.StreamReader(stream);
                var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterDefinition(definition.Name, definition);
            }
        }
        catch
        {
            // LISMA highlighting definition not found in embedded resources
        }
    }
}
