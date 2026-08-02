namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A single state node in a blueprint (finite state machine).
/// Immutable data model suitable for serialization.
/// </summary>
public record BlueprintStateModel
{
    /// <summary>
    /// Unique identifier for this state.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// X position on the editor canvas.
    /// </summary>
    public double CanvasPositionX { get; init; }

    /// <summary>
    /// Y position on the editor canvas.
    /// </summary>
    public double CanvasPositionY { get; init; }

    /// <summary>
    /// Display name of the state.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Body text/code content of the state.
    /// </summary>
    public string Text { get; init; } = "";
}
