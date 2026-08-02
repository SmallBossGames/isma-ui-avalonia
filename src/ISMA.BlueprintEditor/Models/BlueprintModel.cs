using System.Collections.Immutable;

namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// The aggregate root for the entire blueprint (finite state machine).
/// Contains the main state, init state, user states, transitions, and loop transitions.
/// Immutable data model suitable for serialization.
/// </summary>
public record BlueprintModel
{
    /// <summary>
    /// Format version number.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Unique blueprint identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The main state node (permanent, non-deletable).
    /// </summary>
    public BlueprintStateModel Main { get; init; } = new();

    /// <summary>
    /// The initialization state node (permanent, non-deletable).
    /// </summary>
    public BlueprintStateModel Init { get; init; } = new();

    /// <summary>
    /// Additional user-defined states.
    /// </summary>
    public ImmutableArray<BlueprintStateModel> States { get; init; } = ImmutableArray<BlueprintStateModel>.Empty;

    /// <summary>
    /// Transitions between states.
    /// </summary>
    public ImmutableArray<BlueprintTransactionModel> Transactions { get; init; } = ImmutableArray<BlueprintTransactionModel>.Empty;

    /// <summary>
    /// Loopback transitions on single states.
    /// </summary>
    public ImmutableArray<BlueprintLoopTransactionModel> LoopTransactions { get; init; } = ImmutableArray<BlueprintLoopTransactionModel>.Empty;

    /// <summary>
    /// Creates an empty blueprint with default Main and Init states.
    /// </summary>
    public static BlueprintModel Empty => new();
}
