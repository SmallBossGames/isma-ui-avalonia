using System.Collections.Immutable;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class LismaProjectViewModel : ObservableObject, IProjectViewModel
{
    private readonly ISimulationServerFacade _serverFacade;
    private readonly ITextEditorFactory _editorFactory;
    private LismaTextModel _model;
    private object? _editorInstance;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string _fullText;

    public LismaProjectViewModel(ISimulationServerFacade serverFacade, ITextEditorFactory editorFactory)
    {
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _model = new LismaTextModel("", Array.Empty<CodeRegion>());
        _name = "Untitled";
        _fullText = string.Empty;
    }

    public LismaProjectViewModel(ISimulationServerFacade serverFacade, ITextEditorFactory editorFactory, LismaTextModel model, string? filePath)
    {
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _model = model;
        _filePath = filePath;
        _name = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "Untitled";
        _fullText = model.FullText;
    }

    public object? EditorContent => _editorInstance;

    public async Task ValidateAsync()
    {
        var result = await _serverFacade.ValidateModel(_fullText);
        var errors = result.Errors.ToArray();
        if (errors.Length > 0)
        {
            var errorInfos = errors.Select(e => new ErrorInfo
            {
                Row = e.Row,
                Position = e.Column,
                FragmentName = "Main",
                Message = e.Message
            }).ToImmutableArray();
        }
    }

    public void SetContent(string text)
    {
        FullText = text;
        _model = new LismaTextModel(text, _model.Regions);
    }

    public void LoadFromFile(string path)
    {
        _filePath = path;
        _name = Path.GetFileNameWithoutExtension(path);
        _fullText = File.ReadAllText(path);
        _model = new LismaTextModel(_fullText, Array.Empty<CodeRegion>());
        NameChanged?.Invoke();
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(_filePath))
            return await SaveAsAsync();

        File.WriteAllText(_filePath, _fullText);
        return true;
    }

    public async Task<bool> SaveAsAsync()
    {
        return false;
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

    public event Action? NameChanged;

    public void Dispose()
    {
        ResetEditor();
    }
}
