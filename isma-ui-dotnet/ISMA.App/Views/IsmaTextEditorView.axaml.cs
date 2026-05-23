using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using ISMA.App.Services;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class IsmaTextEditorView : UserControl
{
    private readonly EditorPlatformService _editorPlatformService;
    private readonly TextEditorFactory _textEditorFactory;
    private TextEditor? _textEditor;

    public IsmaTextEditorView(EditorPlatformService editorPlatformService, TextEditorFactory textEditorFactory)
    {
        _editorPlatformService = editorPlatformService;
        _textEditorFactory = textEditorFactory;

        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("Editor");
        if (_textEditor is not null)
        {
            _editorPlatformService.SetFocusedEditor(_textEditor);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is LismaProjectViewModel vm && _textEditor is not null)
        {
            _textEditor.Text = vm.EditorContent as string ?? "";
            _editorPlatformService.SetFocusedEditor(_textEditor);
        }
    }
}
