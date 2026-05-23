using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class BlueprintModel
{
    public BlueprintStateModel Main { get; set; }
    public BlueprintStateModel Init { get; set; }
    public ImmutableArray<BlueprintStateModel> States { get; set; }
    public ImmutableArray<BlueprintTransactionModel> Transactions { get; set; }
    public ImmutableArray<BlueprintLoopTransactionModel> LoopTransactions { get; set; }

    public static BlueprintModel Empty => new()
    {
        Main = new BlueprintStateModel(10, 10, "main", ""),
        Init = new BlueprintStateModel(10, 100, "init", ""),
        States = ImmutableArray<BlueprintStateModel>.Empty,
        Transactions = ImmutableArray<BlueprintTransactionModel>.Empty,
        LoopTransactions = ImmutableArray<BlueprintLoopTransactionModel>.Empty
    };
}
