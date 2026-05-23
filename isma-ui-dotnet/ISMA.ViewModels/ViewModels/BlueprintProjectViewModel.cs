using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintProjectViewModel : ObservableObject, IProjectViewModel
{
    private BlueprintModel _model;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private BlueprintEditorViewModel? _editorViewModel;

    public BlueprintProjectViewModel()
    {
        _model = BlueprintModel.Empty;
        _name = "Untitled Blueprint";
        _filePath = null;
    }

    public BlueprintProjectViewModel(BlueprintModel model, string? filePath)
    {
        _model = model;
        _filePath = filePath;
        _name = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "Untitled Blueprint";
    }

    public object? EditorContent => EditorViewModel;

    public BlueprintModel GetBlueprintModel() => _model;

    public LismaTextModel ConvertToLisma()
    {
        return BlueprintToLismaConverter.ConvertToLisma(_model);
    }

    public void LoadFromFile(string path)
    {
        _filePath = path;
        _name = Path.GetFileNameWithoutExtension(path);
        NameChanged?.Invoke();
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(_filePath))
            return await SaveAsAsync();

        return true;
    }

    public async Task<bool> SaveAsAsync()
    {
        return false;
    }

    public void SetEditorViewModel(BlueprintEditorViewModel vm)
    {
        EditorViewModel = vm;
    }

    public event Action? NameChanged;

    public void Dispose()
    {
        EditorViewModel?.Dispose();
    }
}
