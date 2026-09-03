using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using ISMA.App.ViewModels;
using ISMA.TextEditor;

namespace ISMA.App.Views;

/// <summary>
/// Thin host for LISMA text projects: syncs <see cref="LismaProjectViewModel.FullText"/>
/// with the <see cref="IsmaTextEditor"/> and forwards Cut/Copy/Paste requests.
/// </summary>
public partial class IsmaTextEditorView : UserControl
{
    private IsmaTextEditor? _ismaEditor;
    private AvaloniaEdit.TextEditor? _textEditor;
    private LismaProjectViewModel? _currentVm;
    private LismaProjectViewModel? _lastDataContextVm;
    private bool _suppressEditorSync;

    public AvaloniaEdit.TextEditor? TextEditor => _textEditor;

    public IsmaTextEditorView()
    {
        InitializeComponent();

        _ismaEditor = this.FindControl<IsmaTextEditor>("Editor");
        _textEditor = _ismaEditor?.Editor;
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
            _currentVm.PropertyChanged -= OnVmPropertyChanged;

            _textEditor.TextChanged -= OnEditorTextChanged;
        }

        if (DataContext is LismaProjectViewModel vm)
        {
            _currentVm = vm;

            _currentVm.CutRequested += OnCutRequested;
            _currentVm.CopyRequested += OnCopyRequested;
            _currentVm.PasteRequested += OnPasteRequested;
            _currentVm.PropertyChanged += OnVmPropertyChanged;

            _textEditor.TextChanged -= OnEditorTextChanged;
            if (vm != _lastDataContextVm)
            {
                _ismaEditor!.Text = vm.FullText;
                _lastDataContextVm = vm;
            }
            vm.SetEditorInstance(_ismaEditor);
            _textEditor.TextChanged += OnEditorTextChanged;
        }
        else
        {
            _currentVm = null;
            _lastDataContextVm = null;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LismaProjectViewModel.FullText))
            return;

        if (_textEditor is null || _currentVm is null)
            return;

        if (_textEditor.Text != _currentVm.FullText)
        {
            _suppressEditorSync = true;
            _ismaEditor!.Text = _currentVm.FullText;
            _suppressEditorSync = false;
        }
    }

    private void OnCutRequested()
    {
        var editor = _textEditor;
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
        _textEditor?.Copy();
    }

    private void OnPasteRequested()
    {
        _textEditor?.Paste();
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_textEditor is null || _currentVm is null)
            return;

        if (_suppressEditorSync)
            return;

        _currentVm.FullText = _textEditor.Text;
        _ = _currentVm.UpdateSyntaxHighlighting(_textEditor.Text);
    }
}
