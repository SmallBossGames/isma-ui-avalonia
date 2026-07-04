using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class BlueprintModel
{
    public BlueprintStateModel Main { get; init; }
    public BlueprintStateModel Init { get; init; }
    public ImmutableArray<BlueprintStateModel> States { get; init; }
    public ImmutableArray<BlueprintTransactionModel> Transactions { get; init; }
    public ImmutableArray<BlueprintLoopTransactionModel> LoopTransactions { get; init; }

    public BlueprintModel()
    {
        Main = new BlueprintStateModel(10, 10, "main", "");
        Init = new BlueprintStateModel(10, 100, "init", "");
        States = ImmutableArray<BlueprintStateModel>.Empty;
        Transactions = ImmutableArray<BlueprintTransactionModel>.Empty;
        LoopTransactions = ImmutableArray<BlueprintLoopTransactionModel>.Empty;
    }

    public BlueprintModel(BlueprintStateModel main, BlueprintStateModel init)
    {
        Main = main;
        Init = init;
    }

    public static BlueprintModel Empty => new()
    {
        Main = new BlueprintStateModel(10, 10, "main", ""),
        Init = new BlueprintStateModel(10, 100, "init", ""),
        States = ImmutableArray<BlueprintStateModel>.Empty,
        Transactions = ImmutableArray<BlueprintTransactionModel>.Empty,
        LoopTransactions = ImmutableArray<BlueprintLoopTransactionModel>.Empty
    };
}
