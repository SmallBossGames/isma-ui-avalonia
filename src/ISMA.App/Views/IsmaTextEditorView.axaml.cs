using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using ISMA.App.Services;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class IsmaTextEditorView : UserControl
{
    private EditorPlatformService? _editorPlatformService;
    private TextEditor? _textEditor;

    public TextEditor? TextEditor => _textEditor;

    public IsmaTextEditorView()
    {
        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("Editor");
    }

    public IsmaTextEditorView(EditorPlatformService editorPlatformService) : this()
    {
        _editorPlatformService = editorPlatformService;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is LismaProjectViewModel vm && _textEditor is not null)
        {
            _textEditor.Text = vm.FullText;
            vm.SetEditorInstance(_textEditor);
            _editorPlatformService?.SetFocusedEditor(_textEditor);
        }
    }
}
