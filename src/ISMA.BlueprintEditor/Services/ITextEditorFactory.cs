namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// Factory seam that keeps the blueprint editor module decoupled from the concrete
/// text editor implementation (provided by the host application).
/// </summary>
public interface ITextEditorFactory
{
    /// <summary>Creates a new text editor instance for a state/loop body tab.</summary>
    ITextEditor CreateEditor();
}
