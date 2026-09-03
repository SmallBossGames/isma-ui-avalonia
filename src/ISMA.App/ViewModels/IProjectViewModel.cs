namespace ISMA.App.ViewModels;

public interface IProjectViewModel : IDisposable
{
    string Name { get; }
    string? FilePath { get; set; }
    object? EditorContent { get; }
    event Action? NameChanged;
    void TriggerCut();
    void TriggerCopy();
    void TriggerPaste();
    void TriggerSelectAll();
    void SetContent(string content);
}
