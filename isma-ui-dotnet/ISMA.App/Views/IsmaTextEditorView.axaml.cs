using System.Timers;
using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using Avalonia.Interactivity;
using ISMA.App.Services;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class IsmaTextEditorView : UserControl
{
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ITextEditorFactory _editorFactory;
    private readonly EditorPlatformService _editorPlatformService;
    private Timer? _highlightTimer;
    private bool _isHighlighting;
    private string? _lastHighlightedText;

    public IsmaTextEditorView(
        ISyntaxHighlighter syntaxHighlighter,
        ITextEditorFactory editorFactory,
        EditorPlatformService editorPlatformService)
    {
        _syntaxHighlighter = syntaxHighlighter;
        _editorFactory = editorFactory;
        _editorPlatformService = editorPlatformService;
        InitializeComponent();
        SetupEditor();
    }

    private void SetupEditor()
    {
        _editorPlatformService.SetFocusedEditor(Editor);
        _editorFactory.AddSearchPanel(Editor);
        _editorFactory.AddLineNumberMargin(Editor);

        _highlightTimer = new Timer(100) { AutoReset = false };
        _highlightTimer.Elapsed += OnHighlightTimerElapsed;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is LismaProjectViewModel projectVm)
        {
            projectVm.CutRequested += OnCutRequested;
            projectVm.CopyRequested += OnCopyRequested;
            projectVm.PasteRequested += OnPasteRequested;
            projectVm.SetEditorInstance(Editor);
        }
    }

    private void OnTextCut(object? sender, TextCompositionEventArgs e)
    {
        _editorPlatformService.HandleCut();
    }

    private void OnTextCopied(object? sender, TextCompositionEventArgs e)
    {
        _editorPlatformService.HandleCopy();
    }

    private void OnTextPasted(object? sender, TextCompositionEventArgs e)
    {
        _editorPlatformService.HandlePaste();
    }

    private void OnTextEntering(object? sender, TextCompositionEventArgs e)
    {
        RestartHighlightTimer();
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LismaProjectViewModel projectVm)
        {
            projectVm.SetContent(Editor.Text);
        }
        RestartHighlightTimer();
    }

    private void RestartHighlightTimer()
    {
        _highlightTimer?.Stop();
        _highlightTimer?.Start();
    }

    private async void OnHighlightTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _highlightTimer?.Stop();

        var text = Editor.Text;
        if (text == _lastHighlightedText) return;

        if (DataContext is not LismaProjectViewModel projectVm) return;

        _isHighlighting = true;
        projectVm.SetIsHighlighting(true);
        _lastHighlightedText = text;

        try
        {
            var tokens = await _syntaxHighlighter.Highlight(text);
            _editorFactory.SetSyntaxHighlighting(Editor, tokens, text);
        }
        catch
        {
        }
        finally
        {
            _isHighlighting = false;
            projectVm.SetIsHighlighting(false);
        }
    }

    private void OnCutRequested()
    {
        _editorPlatformService.HandleCut();
    }

    private void OnCopyRequested()
    {
        _editorPlatformService.HandleCopy();
    }

    private void OnPasteRequested()
    {
        _editorPlatformService.HandlePaste();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _highlightTimer?.Stop();
        _highlightTimer?.Dispose();

        if (DataContext is LismaProjectViewModel projectVm)
        {
            projectVm.CutRequested -= OnCutRequested;
            projectVm.CopyRequested -= OnCopyRequested;
            projectVm.PasteRequested -= OnPasteRequested;
            projectVm.ResetEditor();
        }
    }
}
