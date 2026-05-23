using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ISMA.Domain.Contracts;

namespace ISMA.App.Services;

public class TextEditorFactory : ITextEditorFactory
{
    public object CreateTextEditor(string text, Action<string>? onTextChanged)
    {
        var textBox = new TextBox
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            AcceptsReturn = true,
            AcceptsTab = true,
            Background = Brushes.White,
            Foreground = Brushes.Black,
            MinHeight = 200
        };

        textBox.TextChanged += (s, e) =>
        {
            onTextChanged?.Invoke(textBox.Text);
        };

        return textBox;
    }

    public void DisposeInstance(object editor)
    {
        if (editor is TextBox tb)
        {
            tb.TextChanged -= null!;
            tb.Text = "";
        }
    }
}
