using System.Threading.Tasks;
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
    private LismaProjectViewModel? _currentVm;

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

        // Unwire handler from old VM
        if (_textEditor is not null && _currentVm is not null)
        {
            _textEditor.TextChanged -= OnEditorTextChanged;
        }

        if (DataContext is LismaProjectViewModel vm && _textEditor is not null)
        {
            _textEditor.TextChanged -= OnEditorTextChanged;
            _textEditor.Text = vm.FullText;
            vm.SetEditorInstance(_textEditor);
            _textEditor.TextChanged += OnEditorTextChanged;
            _editorPlatformService?.SetFocusedEditor(_textEditor);
            _currentVm = vm;
        }
        else
        {
            _currentVm = null;
        }
    }

    private void OnEditorTextChanged(object? sender, System.EventArgs e)
    {
        if (_textEditor is null || _currentVm is null)
            return;

        _currentVm.FullText = _textEditor.Text;
        _ = _currentVm.UpdateSyntaxHighlighting(_textEditor.Text);
    }
}
