namespace ISMA.ViewModels.ViewModels;

public interface IProjectViewModel : IDisposable
{
    string Name { get; }
    string? FilePath { get; set; }
    object? EditorContent { get; }
    bool IsDirty { get; set; }
    event Action? NameChanged;
    void TriggerCut();
    void TriggerCopy();
    void TriggerPaste();
}
