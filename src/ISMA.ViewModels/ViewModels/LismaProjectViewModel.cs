using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class LismaProjectViewModel : ObservableObject, IProjectViewModel
{
    private readonly ISimulationServerFacade _serverFacade;
    private readonly ITextEditorFactory _editorFactory;
    private readonly IProjectFileService _projectFileService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly IModelErrorService? _errorService;
    private LismaTextModel _model;
    private object? _editorInstance;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string _fullText;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isHighlighting;

    private ObservableCollection<SyntaxTokenDto> _highlightTokens = new();
    public ObservableCollection<SyntaxTokenDto> HighlightTokens => _highlightTokens;

    private int _highlightVersion;

    public object? EditorContent => _editorInstance;

    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action? NameChanged;
    public event Action<string>? ContentChanged;

    public LismaProjectViewModel(
        ISimulationServerFacade serverFacade,
        ITextEditorFactory editorFactory,
        IProjectFileService projectFileService,
        ISyntaxHighlighter syntaxHighlighter,
        IModelErrorService? errorService = null)
    {
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _projectFileService = projectFileService;
        _syntaxHighlighter = syntaxHighlighter;
        _errorService = errorService;
        _model = new LismaTextModel("", Array.Empty<CodeRegion>());
        _name = "Untitled";
        FullText = string.Empty;
    }

    public LismaProjectViewModel(
        ISimulationServerFacade serverFacade,
        ITextEditorFactory editorFactory,
        IProjectFileService projectFileService,
        ISyntaxHighlighter syntaxHighlighter,
        LismaTextModel model,
        string? filePath,
        IModelErrorService? errorService = null)
    {
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _projectFileService = projectFileService;
        _syntaxHighlighter = syntaxHighlighter;
        _errorService = errorService;
        _model = model;
        _filePath = filePath;
        _name = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "Untitled";
        _fullText = model.FullText;
    }

    public void SetIsHighlighting(bool value)
    {
        IsHighlighting = value;
    }

    public async Task ValidateAsync()
    {
        var result = await _serverFacade.ValidateModel(FullText);
        var errors = result.Errors.ToArray();
        if (errors.Length > 0)
        {
            var errorInfos = errors.Select(e => new ErrorInfo
            {
                Row = e.Row,
                Position = e.Column,
                FragmentName = "Main",
                Message = e.Message
            }).ToList();

            if (_errorService != null)
            {
                _errorService.PutErrorList(errorInfos);
            }
        }
        else if (_errorService != null)
        {
            _errorService.ClearErrors();
        }
    }

    public void SetContent(string text)
    {
        FullText = text;
        IsDirty = true;
        _model = new LismaTextModel(text, _model.Regions);
        ContentChanged?.Invoke(text);
    }

    public void LoadFromFile(string path)
    {
        FilePath = path;
        Name = Path.GetFileNameWithoutExtension(path);
        FullText = File.ReadAllText(path);
        _model = new LismaTextModel(FullText, Array.Empty<CodeRegion>());
        IsDirty = false;
        NameChanged?.Invoke();
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
            return await SaveAsAsync();

        try
        {
            await File.WriteAllTextAsync(FilePath, FullText);
            IsDirty = false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAsAsync()
    {
        return await _projectFileService.SaveAs(this);
    }

    public async Task<bool> OpenAsync()
    {
        var paths = await _projectFileService.Open((object?)null);
        if (paths.Count == 0) return false;

        var path = paths[0];
        LoadFromFile(path);
        return true;
    }

    public void SetEditorInstance(object editor)
    {
        if (_editorInstance != null && _editorInstance != editor)
        {
            _editorFactory.DisposeInstance(_editorInstance);
        }
        _editorInstance = editor;
    }

    public void ResetEditor()
    {
        if (_editorInstance != null)
        {
            _editorFactory.DisposeInstance(_editorInstance);
            _editorInstance = null;
        }
    }

    public LismaTextModel GetModel() => _model;

    public async Task UpdateSyntaxHighlighting(string source)
    {
        try
        {
            var version = ++_highlightVersion;
            var tokens = await _syntaxHighlighter.Highlight(source);

            await Task.Delay(100);

            if (version != _highlightVersion)
                return;

            _highlightTokens = new ObservableCollection<SyntaxTokenDto>(tokens);
            if (_editorInstance is not null)
                _editorFactory.SetSyntaxHighlighting(_editorInstance, tokens, source);
        }
        catch
        {
        }
    }

    public void TriggerCut()
    {
        if (_editorInstance is null)
        {
            CutRequested?.Invoke();
            return;
        }

        var documentObj = _editorInstance.GetType().GetProperty("Document")?.GetValue(_editorInstance);
        if (documentObj is null)
        {
            CutRequested?.Invoke();
            return;
        }

        var documentType = documentObj.GetType();
        var selectionStart = (int)(_editorInstance.GetType().GetProperty("SelectionStart")?.GetValue(_editorInstance) ?? 0);
        var selectionLength = (int)(_editorInstance.GetType().GetProperty("SelectionLength")?.GetValue(_editorInstance) ?? 0);

        if (selectionLength <= 0)
        {
            CutRequested?.Invoke();
            return;
        }

        var currentText = (string)(documentType.GetProperty("Text")?.GetValue(documentObj) ?? string.Empty);
        var newText = currentText.Remove(selectionStart, selectionLength);
        documentType.GetProperty("Text")?.SetValue(documentObj, newText);

        FullText = newText;
        CutRequested?.Invoke();
    }
    public void TriggerCopy() => CopyRequested?.Invoke();
    public void TriggerPaste() => PasteRequested?.Invoke();

    public void TriggerSelectAll()
    {
        _editorInstance?.GetType().GetMethod("SelectAll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            ?.Invoke(_editorInstance, null);
    }

    public Action? OnBeforeDispose { get; set; }

    public void Dispose()
    {
        OnBeforeDispose?.Invoke();
        ResetEditor();
    }
}
