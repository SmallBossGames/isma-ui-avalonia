using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintProjectViewModel : ObservableObject, IProjectViewModel
{
    private BlueprintModel _model;
    private BlueprintEditorViewModel? _editorViewModel;
    private readonly IProjectFileService _projectFileService;
    private readonly ITextEditorFactory _editorFactory;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    public object? EditorContent => _editorViewModel;

    public event Action? NameChanged;

    public BlueprintProjectViewModel(
        IProjectFileService projectFileService,
        ITextEditorFactory editorFactory)
    {
        _projectFileService = projectFileService;
        _editorFactory = editorFactory;
        _model = BlueprintModel.Empty;
        _name = "Untitled Blueprint";
        _filePath = null;
    }

    public BlueprintProjectViewModel(
        IProjectFileService projectFileService,
        ITextEditorFactory editorFactory,
        BlueprintModel model,
        string? filePath)
    {
        _projectFileService = projectFileService;
        _editorFactory = editorFactory;
        _model = model;
        _filePath = filePath;
        _name = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "Untitled Blueprint";
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
            return await SaveAsAsync();

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_model, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
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

    private async Task<bool> OpenAsync()
    {
        var paths = await _projectFileService.Open((object?)null);
        if (paths.Count == 0) return false;

        var path = paths[0];
        LoadFromFile(path);
        return true;
    }

    public void LoadFromFile(string path)
    {
        FilePath = path;
        Name = Path.GetFileNameWithoutExtension(path);
        IsDirty = false;
        NameChanged?.Invoke();

        try
        {
            var json = File.ReadAllText(path);
            var loadedModel = System.Text.Json.JsonSerializer.Deserialize<BlueprintModel>(json);
            if (loadedModel != null)
            {
                _model = loadedModel;
            }
        }
        catch
        {
            _model = BlueprintModel.Empty;
        }
    }

    public async Task<LismaProjectViewModel?> ConvertToLismaAsync(
        ISimulationServerFacade serverFacade,
        ISyntaxHighlighter syntaxHighlighter)
    {
        var lismaModel = BlueprintToLismaConverter.ConvertToLisma(_model);
        var lismaProject = new LismaProjectViewModel(
            serverFacade,
            _editorFactory,
            _projectFileService,
            syntaxHighlighter,
            lismaModel,
            FilePath?.Replace(".scisma", ".iscm2"));

        IsDirty = false;
        return lismaProject;
    }

    public BlueprintModel GetBlueprintModel() => _model;

    public LismaTextModel ConvertToLisma()
    {
        return BlueprintToLismaConverter.ConvertToLisma(_model);
    }

    public void SetEditorViewModel(BlueprintEditorViewModel vm)
    {
        _editorViewModel = vm;
    }

    public void Dispose()
    {
        _editorViewModel?.Dispose();
    }

    public void TriggerCut() { }
    public void TriggerCopy() { }
    public void TriggerPaste() { }
    public void TriggerSelectAll() { }
    public void SetContent(string content) { }
}
