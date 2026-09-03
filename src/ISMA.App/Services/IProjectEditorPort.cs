using ISMA.BlueprintEditor.Views;

namespace ISMA.App.Services;

/// <summary>
/// Port that creates and destroys blueprint editor instances, keeping the
/// editor module decoupled from the rest of the application.
/// </summary>
public interface IProjectEditorPort
{
    /// <summary>
    /// Creates a new blueprint editor instance.
    /// </summary>
    IsmaBlueprintEditor CreateBlueprintEditor();

    /// <summary>
    /// Disposes a blueprint editor instance created by <see cref="CreateBlueprintEditor"/>.
    /// </summary>
    void DisposeEditor(IsmaBlueprintEditor editor);
}
