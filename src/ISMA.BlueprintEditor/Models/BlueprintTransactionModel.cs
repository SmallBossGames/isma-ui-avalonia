namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A directed transition between two states in a blueprint.
/// Immutable data model suitable for serialization.
/// </summary>
public record BlueprintTransactionModel
{
    /// <summary>
    /// Unique identifier for this transition.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Source state identifier.
    /// </summary>
    public Guid StartStateId { get; init; }

    /// <summary>
    /// Destination state identifier.
    /// </summary>
    public Guid EndStateId { get; init; }

    /// <summary>
    /// Guard condition expression for this transition.
    /// </summary>
    public string Predicate { get; init; } = "";

    /// <summary>
    /// Optional display name for this transition.
    /// </summary>
    public string Alias { get; init; } = "";
}
