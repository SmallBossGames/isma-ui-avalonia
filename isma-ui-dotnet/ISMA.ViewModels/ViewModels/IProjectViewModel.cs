namespace ISMA.ViewModels.ViewModels;

public interface IProjectViewModel : IDisposable
{
    string Name { get; }
    string? FilePath { get; set; }
    object? EditorContent { get; }
    event Action? NameChanged;
}
