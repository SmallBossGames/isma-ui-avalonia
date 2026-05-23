using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Search;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;

namespace ISMA.App.Services;

public class TextEditorFactory : ITextEditorFactory
{
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
    }

    public void AddSearchPanel(object editor)
    {
        if (editor is not TextEditor te) return;
        SearchPanel.Install(te);
    }

    public void AddLineNumberMargin(object editor)
    {
        // Line numbers are already enabled via ShowLineNumbers property
    }

    public void DisposeInstance(object editor)
    {
        if (editor is TextEditor te)
        {
            te.TextChanged -= null!;
            te.Text = "";
            te.SyntaxHighlighting = null;
        }
    }
}
