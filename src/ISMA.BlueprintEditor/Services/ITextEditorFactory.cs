using System.Collections.Immutable;

namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// Syntax token kind for syntax highlighting.
/// </summary>
public enum SyntaxTokenKind
{
    Unspecified,
    Keyword,
    Comment,
    Number,
    Text
}

/// <summary>
/// Represents a syntax token in source code.
/// </summary>
public record SyntaxTokenDto
{
    public int Start { get; init; }
    public int Length { get; init; }
    public SyntaxTokenKind Kind { get; init; }
}

/// <summary>
/// Service Provider Interface (SPI) for creating and managing text editor instances.
/// Implemented by the consuming application to provide text editors for state/loop text content.
/// </summary>
public interface ITextEditorFactory
{
    /// <summary>
    /// Creates a new text editor instance.
    /// </summary>
    /// <param name="text">Initial text content.</param>
    /// <param name="onTextChanged">Optional callback when text changes.</param>
    /// <param name="highlightingDefinitionName">Optional syntax highlighting definition name.</param>
    /// <returns>Opaque editor instance handle.</returns>
    object CreateTextEditor(string text, Action<string>? onTextChanged, string? highlightingDefinitionName = null);

    /// <summary>
    /// Sets syntax highlighting tokens on an editor instance.
    /// </summary>
    /// <param name="editor">The editor instance.</param>
    /// <param name="tokens">Syntax tokens to apply.</param>
    /// <param name="source">Original source text for position validation.</param>
    void SetSyntaxHighlighting(object editor, SyntaxTokenDto[] tokens, string source);

    /// <summary>
    /// Adds a search panel to the editor instance.
    /// </summary>
    /// <param name="editor">The editor instance.</param>
    void AddSearchPanel(object editor);

    /// <summary>
    /// Adds a line number margin to the editor instance.
    /// </summary>
    /// <param name="editor">The editor instance.</param>
    void AddLineNumberMargin(object editor);

    /// <summary>
    /// Disposes of the editor instance and frees resources.
    /// </summary>
    /// <param name="editor">The editor instance to dispose.</param>
    void DisposeInstance(object editor);
}
