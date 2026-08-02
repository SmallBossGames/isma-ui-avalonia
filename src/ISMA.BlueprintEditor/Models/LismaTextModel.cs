using System.Collections.Immutable;

namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A contiguous block of source code with semantic meaning (line range + name).
/// Used for mapping generated Lisma source text back to blueprint states.
/// </summary>
public record CodeRegion
{
    /// <summary>
    /// Human-readable region name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Starting line number (0-based).
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// Ending line number (0-based, inclusive).
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// Default fragment representing the main state.
    /// </summary>
    public static CodeRegion DefaultFragment => new() { Name = "Main", StartLine = 0, EndLine = 0 };
}

/// <summary>
/// Represents the generated Lisma PDE source code with code region mappings.
/// Immutable data model suitable for serialization.
/// </summary>
public record LismaTextModel
{
    /// <summary>
    /// The complete source text.
    /// </summary>
    public string FullText { get; init; } = "";

    /// <summary>
    /// Mapped source code regions with semantic meaning.
    /// </summary>
    public ImmutableArray<CodeRegion> Regions { get; init; } = ImmutableArray<CodeRegion>.Empty;
}
