using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class IsmaTextEditorView : UserControl
{
    private TextEditor? _textEditor;
    private LismaProjectViewModel? _currentVm;
    private LismaProjectViewModel? _lastDataContextVm;

    public TextEditor? TextEditor => _textEditor;

    public IsmaTextEditorView()
    {
        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("Editor");
    }

    private void OnCutClicked(object? sender, RoutedEventArgs e)
    {
        _textEditor?.Cut();
    }

    private void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        _textEditor?.Copy();
    }

    private void OnPasteClicked(object? sender, RoutedEventArgs e)
    {
        _textEditor?.Paste();
    }

    private void OnSelectAllClicked(object? sender, RoutedEventArgs e)
    {
        _textEditor?.SelectAll();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_textEditor is null)
            return;

        if (_currentVm is not null)
        {
            _currentVm.CutRequested -= OnCutRequested;
            _currentVm.CopyRequested -= OnCopyRequested;
            _currentVm.PasteRequested -= OnPasteRequested;

            _textEditor.TextChanged -= OnEditorTextChanged;
        }

        if (DataContext is LismaProjectViewModel vm)
        {
            _currentVm = vm;

            _currentVm.CutRequested += OnCutRequested;
            _currentVm.CopyRequested += OnCopyRequested;
            _currentVm.PasteRequested += OnPasteRequested;

            _textEditor.TextChanged -= OnEditorTextChanged;
            if (vm != _lastDataContextVm)
            {
                _textEditor.Text = vm.FullText;
                _lastDataContextVm = vm;
            }
            vm.SetEditorInstance(_textEditor);
            _textEditor.TextChanged += OnEditorTextChanged;
        }
        else
        {
            _currentVm = null;
            _lastDataContextVm = null;
        }
    }

    private void OnCutRequested()
    {
        var editor = _textEditor ?? (_currentVm?.EditorContent as TextEditor);
        if (editor is not null && editor.SelectionLength > 0)
        {
            var newText = editor.Document.Text.Remove(editor.SelectionStart, editor.SelectionLength);
            editor.Document.Text = newText;
            if (_currentVm is not null)
                _currentVm.FullText = newText;
        }
    }

    private void OnCopyRequested()
    {
        var editor = _textEditor ?? (_currentVm?.EditorContent as TextEditor);
        editor?.Copy();
    }

    private void OnPasteRequested()
    {
        var editor = _textEditor ?? (_currentVm?.EditorContent as TextEditor);
        editor?.Paste();
    }

    private void OnEditorTextChanged(object? sender, System.EventArgs e)
    {
        if (_textEditor is null || _currentVm is null)
            return;

        _currentVm.FullText = _textEditor.Text;
        _ = _currentVm.UpdateSyntaxHighlighting(_textEditor.Text);
    }
}
