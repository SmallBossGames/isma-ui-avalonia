using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using ISMA.App.Services;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class IsmaTextEditorView : UserControl
{
    private EditorPlatformService? _editorPlatformService;
    private TextEditorFactory? _textEditorFactory;
    private TextEditor? _textEditor;

    public IsmaTextEditorView()
    {
        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("Editor");
    }

    public IsmaTextEditorView(EditorPlatformService editorPlatformService, TextEditorFactory textEditorFactory) : this()
    {
        _editorPlatformService = editorPlatformService;
        _textEditorFactory = textEditorFactory;

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
            _editorPlatformService?.SetFocusedEditor(_textEditor);
        }
    }
}
