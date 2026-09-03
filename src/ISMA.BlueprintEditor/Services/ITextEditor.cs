using Avalonia.Controls;

namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// A text editor instance embedded in the blueprint editor's state/loop body tabs.
/// Implemented by the host application (see <see cref="ITextEditorFactory"/>).
/// </summary>
public interface ITextEditor : IDisposable
{
    /// <summary>The control to embed in the editor tab.</summary>
    Control Node { get; }

    /// <summary>The editor text (two-way with the bound state/loop body).</summary>
    string Text { get; set; }

    /// <summary>Raised when the editor text changes.</summary>
    event EventHandler? TextChanged;
}
