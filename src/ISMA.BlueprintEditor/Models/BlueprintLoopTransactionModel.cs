namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A loopback transition from a state back to itself.
/// Immutable data model suitable for serialization.
/// </summary>
public record BlueprintLoopTransactionModel
{
    /// <summary>
    /// Unique identifier for this loop.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The state this loop belongs to.
    /// </summary>
    public Guid StateId { get; init; }

    /// <summary>
    /// Loop guard condition.
    /// </summary>
    public string Predicate { get; init; } = "";

    /// <summary>
    /// Optional display name for this loop.
    /// </summary>
    public string Alias { get; init; } = "";

    /// <summary>
    /// Loop body text.
    /// </summary>
    public string Text { get; init; } = "";
}
