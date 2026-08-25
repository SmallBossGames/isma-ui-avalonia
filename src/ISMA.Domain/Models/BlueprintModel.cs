using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class BlueprintModel
{
    /// <summary>
    /// Format version of the blueprint file.
    /// </summary>
    public int Version { get; init; } = 1;

    public Guid Id { get; init; } = Guid.NewGuid();
    public BlueprintStateModel Main { get; init; }
    public BlueprintStateModel Init { get; init; }
    public ImmutableArray<BlueprintStateModel> States { get; init; }
    public ImmutableArray<BlueprintTransactionModel> Transactions { get; init; }
    public ImmutableArray<BlueprintLoopTransactionModel> LoopTransactions { get; init; }

    public BlueprintModel()
    {
        Id = Guid.NewGuid();
        Main = new BlueprintStateModel(10, 10, "Main", "");
        Init = new BlueprintStateModel(10, 100, "init", "");
        States = ImmutableArray<BlueprintStateModel>.Empty;
        Transactions = ImmutableArray<BlueprintTransactionModel>.Empty;
        LoopTransactions = ImmutableArray<BlueprintLoopTransactionModel>.Empty;
    }

    public BlueprintModel(BlueprintStateModel main, BlueprintStateModel init)
    {
        Id = Guid.NewGuid();
        Main = main;
        Init = init;
    }

    public static BlueprintModel Empty => new();

    public bool HasLegacyGuids()
    {
        if (Version == 0) return true;
        if (Id == Guid.Empty) return true;
        if (Main.Id == Guid.Empty || Init.Id == Guid.Empty) return true;
        foreach (var state in States)
        {
            if (state.Id == Guid.Empty) return true;
        }
        foreach (var tx in Transactions)
        {
            if (tx.Id == Guid.Empty || tx.StartStateId == Guid.Empty || tx.EndStateId == Guid.Empty) return true;
        }
        foreach (var ltx in LoopTransactions)
        {
            if (ltx.Id == Guid.Empty || ltx.StateId == Guid.Empty) return true;
        }
        return false;
    }
}
