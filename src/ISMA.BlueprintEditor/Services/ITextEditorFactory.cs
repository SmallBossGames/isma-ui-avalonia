using Avalonia.Controls;

namespace ISMA.BlueprintEditor.Services;

public interface ITextEditorFactory
{
    Control CreateTextEditor(string text, Action<string> onTextChanged);
    void DisposeInstance(Control node);
}
