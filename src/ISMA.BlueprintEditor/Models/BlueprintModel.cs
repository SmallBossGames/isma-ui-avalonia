using System.Text;
using ISMA.BlueprintEditor.Constants;

namespace ISMA.BlueprintEditor.Models;

public class BlueprintModel
{
    public BlueprintStateModel Main { get; }
    public BlueprintStateModel Init { get; }
    public IReadOnlyCollection<BlueprintStateModel> States { get; }
    public IReadOnlyCollection<BlueprintTransactionModel> Transactions { get; }
    public IReadOnlyCollection<BlueprintLoopTransactionModel> LoopTransactions { get; }

    public BlueprintModel(BlueprintStateModel main, BlueprintStateModel init, IEnumerable<BlueprintStateModel> states, IEnumerable<BlueprintTransactionModel> transactions, IEnumerable<BlueprintLoopTransactionModel>? loopTransactions = null)
    {
        Main = main;
        Init = init;
        States = states.ToList().AsReadOnly();
        Transactions = transactions.ToList().AsReadOnly();
        LoopTransactions = (loopTransactions ?? Array.Empty<BlueprintLoopTransactionModel>()).ToList().AsReadOnly();
    }

    public static BlueprintModel Empty => new(
        new BlueprintStateModel(0, 0, StateNames.Main, string.Empty),
        new BlueprintStateModel(0, 0, StateNames.Init, string.Empty),
        Array.Empty<BlueprintStateModel>(),
        Array.Empty<BlueprintTransactionModel>()
    );

    public LismaTextModel ToLismaText()
    {
        var sb = new StringBuilder();
        var regions = new List<CodeRegion>();

        // Append main state text as first fragment
        sb.Append(Main.Text);
        regions.Add(new CodeRegion(Main.Name, 0, sb.ToString().Split('\n').Length - 1));

        // Group transactions by target state + predicate
        var stateGroups = new Dictionary<(string endStateName, string predicate), (string text, List<string> sources)>();

        foreach (var tx in Transactions)
        {
            var key = (tx.EndStateName, tx.Predicate);
            if (!stateGroups.TryGetValue(key, out var value))
            {
                value = ("", new List<string>());
            }

            // Find the state to get its text
            var state = States.FirstOrDefault(s => s.Name == tx.EndStateName);
            if (state != null && !string.IsNullOrEmpty(state.Text))
            {
                value.text = state.Text;
            }

            if (!value.sources.Contains(tx.StartStateName))
            {
                value.sources.Add(tx.StartStateName);
            }

            stateGroups[key] = value;
        }

        foreach (var (key, value) in stateGroups)
        {
            var lineStart = sb.ToString().Split('\n').Length;

            if (string.IsNullOrEmpty(value.text))
            {
                sb.Append($"state {key.endStateName};");
            }
            else
            {
                sb.Append($"state {key.endStateName} {{ {value.text} }} from {string.Join(",", value.sources)};");
            }

            var lineEnd = sb.ToString().Split('\n').Length - 1;
            regions.Add(new CodeRegion(key.endStateName, lineStart, lineEnd));
        }

        // Handle loop transactions
        foreach (var loop in LoopTransactions)
        {
            var lineStart = sb.ToString().Split('\n').Length;

            // Create pseudo-state for predicate ordering
            var pseudoStateName = $"{loop.StateName}_pseudo_1";
            sb.Append($"state {pseudoStateName} ({loop.Predicate}) {{ {loop.Text} }} from {loop.StateName};");

            // Create original state referencing pseudo-state
            var originalLineStart = sb.ToString().Split('\n').Length;
            sb.Append($"state {loop.StateName} (1 > 0) {{ {loop.Text} }} from {pseudoStateName};");

            var lineEnd = sb.ToString().Split('\n').Length - 1;
            regions.Add(new CodeRegion(loop.StateName, originalLineStart, lineEnd));
        }

        return new LismaTextModel(sb.ToString(), regions);
    }
}
