using ISMA.BlueprintEditor.Services;
using ISMA.BlueprintEditor.Views;

namespace ISMA.App.Services;

/// <summary>
/// Default <see cref="IProjectEditorPort"/>: creates <see cref="IsmaBlueprintEditor"/>
/// instances backed by the injected blueprint text-editor factory.
/// </summary>
public sealed class ProjectEditorPortImpl : IProjectEditorPort
{
    private readonly ITextEditorFactory _editorFactory;

    public ProjectEditorPortImpl(ITextEditorFactory editorFactory)
    {
        _editorFactory = editorFactory;
    }

    public IsmaBlueprintEditor CreateBlueprintEditor() => new IsmaBlueprintEditor(_editorFactory);

    public void DisposeEditor(IsmaBlueprintEditor editor) => editor.DisposeEditor();
}
