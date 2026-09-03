using System.Collections.Immutable;
using ISMA.BlueprintEditor.Constants;

namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// The persistable blueprint (state machine) aggregate root.
/// Main and init are separate fields; user states live in <see cref="States"/>.
/// </summary>
/// <param name="Main">The Main state.</param>
/// <param name="Init">The init state.</param>
/// <param name="States">User-defined states.</param>
/// <param name="Transactions">Directed transitions between states.</param>
/// <param name="LoopTransactions">Self-transitions (loops) with their own body.</param>
public sealed record BlueprintModel(
    BlueprintStateModel Main,
    BlueprintStateModel Init,
    ImmutableArray<BlueprintStateModel> States,
    ImmutableArray<BlueprintTransactionModel> Transactions,
    ImmutableArray<BlueprintLoopTransactionModel> LoopTransactions)
{
    /// <summary>An empty blueprint with default Main (10,10) and init (10,100) states.</summary>
    public static BlueprintModel Empty { get; } = new(
        new BlueprintStateModel(10, 10, StateNames.Main, ""),
        new BlueprintStateModel(10, 100, StateNames.Init, ""),
        ImmutableArray<BlueprintStateModel>.Empty,
        ImmutableArray<BlueprintTransactionModel>.Empty,
        ImmutableArray<BlueprintLoopTransactionModel>.Empty);
}
