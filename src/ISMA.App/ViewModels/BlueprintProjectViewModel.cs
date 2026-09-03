using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.App.Services;
using ISMA.App.Services.Blueprint;
using ISMA.BlueprintEditor.Models;
using ISMA.BlueprintEditor.Views;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.App.ViewModels;

/// <summary>
/// View model for a blueprint (statechart) project. Hosts a lazily created
/// <see cref="IsmaBlueprintEditor"/> instance obtained through the
/// <see cref="IProjectEditorPort"/>.
/// </summary>
public partial class BlueprintProjectViewModel : ObservableObject, IProjectViewModel
{
    private readonly IProjectEditorPort _editorPort;
    private readonly IProjectFileService _projectFileService;
    private readonly IModelErrorService? _errorService;
    private IsmaBlueprintEditor? _editor;

    /// <summary>
    /// The blueprint editor control, created lazily on first access.
    /// </summary>
    public IsmaBlueprintEditor Editor => _editor ??= _editorPort.CreateBlueprintEditor();

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _filePath;

    /// <inheritdoc />
    public object? EditorContent => Editor;

    /// <summary>
    /// Occurs when the project name has changed.
    /// </summary>
    public event Action? NameChanged;

    /// <summary>
    /// Creates a new empty blueprint project.
    /// </summary>
    public BlueprintProjectViewModel(
        IProjectEditorPort editorPort,
        IProjectFileService projectFileService,
        IModelErrorService? errorService = null)
    {
        _editorPort = editorPort;
        _projectFileService = projectFileService;
        _errorService = errorService;
        _name = "New statechart";
        _filePath = null;
    }

    /// <summary>
    /// Creates a blueprint project initialized with the given model.
    /// </summary>
    /// <param name="editorPort">The port that creates blueprint editors.</param>
    /// <param name="projectFileService">The project file service.</param>
    /// <param name="model">The blueprint model to initialize with.</param>
    /// <param name="filePath">Optional file path for the project.</param>
    /// <param name="errorService">Optional error service for reporting errors.</param>
    public BlueprintProjectViewModel(
        IProjectEditorPort editorPort,
        IProjectFileService projectFileService,
        BlueprintModel model,
        string? filePath,
        IModelErrorService? errorService = null)
    {
        _editorPort = editorPort;
        _projectFileService = projectFileService;
        _errorService = errorService;
        var editor = _editorPort.CreateBlueprintEditor();
        editor.SetBlueprintModel(model);
        _editor = editor;
        _filePath = filePath;
        _name = !string.IsNullOrEmpty(filePath) ? Path.GetFileName(filePath) : "New statechart";
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
            var json = BlueprintFileSerializer.ToJson(Editor.GetBlueprintModel());
            await File.WriteAllTextAsync(FilePath, json);
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

    /// <summary>
    /// Loads a blueprint model from a .iscm2 file into the editor.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    public void LoadFromFile(string path)
    {
        FilePath = path;
        Name = Path.GetFileName(path);
        NameChanged?.Invoke();

        try
        {
            var json = File.ReadAllText(path);
            Editor.SetBlueprintModel(BlueprintFileSerializer.FromJson(json));
        }
        catch (Exception ex)
        {
            _errorService?.PutErrorList(new[] { new ErrorInfo { Row = 0, Position = 0, FragmentName = "Load", Message = ex.Message } });
            Editor.SetBlueprintModel(BlueprintModel.Empty);
        }
    }

    /// <summary>
    /// Gets the current blueprint model from the editor.
    /// </summary>
    /// <returns>The blueprint model.</returns>
    public BlueprintModel GetBlueprintModel() => Editor.GetBlueprintModel();

    /// <summary>
    /// Converts the current blueprint model to a Lisma text model.
    /// </summary>
    /// <returns>The Lisma text model.</returns>
    public LismaTextModel ConvertToLisma()
    {
        return LismaCodegen.ToLismaText(Editor.GetBlueprintModel());
    }

    /// <summary>
    /// Disposes the editor instance, if one was created.
    /// </summary>
    public void Dispose()
    {
        if (_editor is not null)
        {
            _editorPort.DisposeEditor(_editor);
            _editor = null;
        }
    }

    public void TriggerCut() { }
    public void TriggerCopy() { }
    public void TriggerPaste() { }
    public void TriggerSelectAll() { }
    public void SetContent(string content) { }
}
