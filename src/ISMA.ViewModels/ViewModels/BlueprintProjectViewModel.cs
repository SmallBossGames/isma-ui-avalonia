using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintProjectViewModel : ObservableObject, IProjectViewModel
{
    [ObservableProperty]
    private BlueprintEditorViewModel _editorContent = null!;

    private readonly IProjectFileService _projectFileService;
    private readonly ITextEditorFactory _editorFactory;
    private readonly IModelErrorService? _errorService;
    private readonly IAutoSaveService? _autoSaveService;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    partial void OnIsDirtyChanged(bool value)
    {
        if (value && !string.IsNullOrEmpty(FilePath))
        {
            _autoSaveService?.TriggerSave();
        }
    }

    /// <inheritdoc />
    object? IProjectViewModel.EditorContent => EditorContent;



    /// <summary>
    /// Occurs when the project name has changed.
    /// </summary>
    public event Action? NameChanged;

    /// <summary>
    /// Creates a new empty blueprint project.
    /// </summary>
    public BlueprintProjectViewModel(
        IProjectFileService projectFileService,
        ITextEditorFactory editorFactory,
        IModelErrorService? errorService = null,
        IAutoSaveService? autoSaveService = null)
    {
        _projectFileService = projectFileService;
        _editorFactory = editorFactory;
        _errorService = errorService;
        _autoSaveService = autoSaveService;
        EditorContent = new BlueprintEditorViewModel();
        _name = "Untitled Blueprint";
        _filePath = null;
    }

    /// <summary>
    /// Creates a blueprint project initialized with the given model.
    /// </summary>
    /// <param name="projectFileService">The project file service.</param>
    /// <param name="editorFactory">The text editor factory.</param>
    /// <param name="model">The blueprint model to initialize with.</param>
    /// <param name="filePath">Optional file path for the project.</param>
    /// <param name="errorService">Optional error service for reporting errors.</param>
    /// <param name="autoSaveService">Optional auto-save service.</param>
    public BlueprintProjectViewModel(
        IProjectFileService projectFileService,
        ITextEditorFactory editorFactory,
        BlueprintModel model,
        string? filePath,
        IModelErrorService? errorService = null,
        IAutoSaveService? autoSaveService = null)
    {
        _projectFileService = projectFileService;
        _editorFactory = editorFactory;
        _errorService = errorService;
        _autoSaveService = autoSaveService;
        EditorContent = new BlueprintEditorViewModel(model);
        _filePath = filePath;
        _name = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "Untitled Blueprint";
    }

    /// <summary>
    /// Saves the blueprint model to the current file path. If no path is set, calls <see cref="SaveAsAsync"/>.
    /// </summary>
    /// <returns>True if the save succeeded; otherwise, false.</returns>
    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
            return await SaveAsAsync();

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(EditorContent.GetBlueprintModel(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
            IsDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            _errorService?.PutErrorList(new[] { new ErrorInfo { Row = 0, Position = 0, FragmentName = "Save", Message = ex.Message } });
            return false;
        }
    }

    /// <summary>
    /// Saves the blueprint model to a new or existing file via the file service.
    /// </summary>
    /// <returns>True if the save succeeded; otherwise, false.</returns>
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

    /// <summary>
    /// Loads a blueprint model from a JSON file and recreates the editor view model.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
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
                EditorContent.LoadFromModel(loadedModel);
            }
        }
        catch (Exception ex)
        {
            _errorService?.PutErrorList(new[] { new ErrorInfo { Row = 0, Position = 0, FragmentName = "Load", Message = ex.Message } });
            EditorContent.LoadFromModel(BlueprintModel.Empty);
        }
    }

    /// <summary>
    /// Converts the current blueprint model to a Lisma project.
    /// </summary>
    /// <param name="serverFacade">The simulation server facade.</param>
    /// <param name="syntaxHighlighter">The syntax highlighter.</param>
    /// <returns>A new Lisma project view model, or null if conversion failed.</returns>
    public async Task<LismaProjectViewModel?> ConvertToLismaAsync(
        ISimulationServerFacade serverFacade,
        ISyntaxHighlighter syntaxHighlighter)
    {
        var lismaModel = BlueprintToLismaConverter.ConvertToLisma(EditorContent.GetBlueprintModel());
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

    /// <summary>
    /// Gets the current blueprint model from the editor view model.
    /// </summary>
    /// <returns>The blueprint model.</returns>
    public BlueprintModel GetBlueprintModel() => EditorContent.GetBlueprintModel();

    /// <summary>
    /// Converts the current blueprint model to a Lisma text model.
    /// </summary>
    /// <returns>The Lisma text model.</returns>
    public LismaTextModel ConvertToLisma()
    {
        var model = EditorContent.GetBlueprintModel();
        return BlueprintToLismaConverter.ConvertToLisma(model);
    }

    /// <summary>
    /// Sets the editor view model directly.
    /// </summary>
    /// <param name="vm">The editor view model to set.</param>
    public void SetEditorViewModel(BlueprintEditorViewModel vm)
    {
        EditorContent = vm;
    }

    /// <summary>
    /// Disposes the editor view model.
    /// </summary>
    public void Dispose()
    {
        EditorContent.Dispose();
    }

    public void TriggerCut() { }
    public void TriggerCopy() { }
    public void TriggerPaste() { }
    public void TriggerSelectAll() { }
    public void SetContent(string content) { }
}
