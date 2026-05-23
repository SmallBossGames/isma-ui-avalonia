using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class LismaProjectViewModel : ObservableObject, IProjectViewModel
{
    private readonly ISimulationServerFacade _serverFacade;
    private readonly ITextEditorFactory _editorFactory;
    private readonly IProjectFileService _projectFileService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
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

    public object? EditorContent => _editorInstance;

    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action? NameChanged;

    public LismaProjectViewModel(
        ISimulationServerFacade serverFacade,
        ITextEditorFactory editorFactory,
        IProjectFileService projectFileService,
        ISyntaxHighlighter syntaxHighlighter)
    {
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _projectFileService = projectFileService;
        _syntaxHighlighter = syntaxHighlighter;
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
        string? filePath)
    {
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _projectFileService = projectFileService;
        _syntaxHighlighter = syntaxHighlighter;
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
        }
    }

    public void SetContent(string text)
    {
        FullText = text;
        IsDirty = true;
        _model = new LismaTextModel(text, _model.Regions);
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
            var tokens = await _syntaxHighlighter.Highlight(source);
            _editorFactory.SetSyntaxHighlighting(_editorInstance, tokens, source);
            _highlightTokens = new ObservableCollection<SyntaxTokenDto>(tokens);
        }
        catch
        {
        }
    }

    public void TriggerCut() => CutRequested?.Invoke();
    public void TriggerCopy() => CopyRequested?.Invoke();
    public void TriggerPaste() => PasteRequested?.Invoke();

    public void Dispose()
    {
        ResetEditor();
    }
}
