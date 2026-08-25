using System.Collections.Immutable;
using System.Text;
using ISMA.Domain.Models;

namespace ISMA.Domain.Conversion;

/// <summary>
/// Converts a <see cref="BlueprintModel"/> to LISMA source, ported 1:1 from the
/// original Kotlin <c>LismaCodegen.kt</c>: the main state text first, then one
/// <c>state &lt;target&gt; (&lt;predicate&gt;) { text } from &lt;sources&gt;;</c> block per
/// (target, predicate) group, then loop transactions as pseudo-state pairs.
/// </summary>
public static class BlueprintToLismaConverter
{
    private const string LismaTrue = "1 > 0";

    public static LismaTextModel ConvertToLisma(BlueprintModel model)
    {
        var regions = new List<CodeRegion>();
        var sb = new StringBuilder();

        var stateNames = new Dictionary<Guid, string>
        {
            [model.Main.Id] = model.Main.Name,
            [model.Init.Id] = model.Init.Name
        };
        foreach (var state in model.States)
        {
            stateNames[state.Id] = state.Name;
        }

        // User state name -> text (main/init are not part of this map, as in the original).
        var statesMap = new Dictionary<string, string>();
        foreach (var state in model.States)
        {
            statesMap[state.Name] = state.Text;
        }

        sb.AppendLine(model.Main.Text);

        var stateBlockModels = new Dictionary<string, StateBlockModel>();
        foreach (var tx in model.Transactions)
        {
            if (!stateNames.TryGetValue(tx.EndStateId, out var endStateName))
                continue;
            if (!stateNames.TryGetValue(tx.StartStateId, out var startStateName))
                continue;

            var predicate = string.IsNullOrWhiteSpace(tx.Predicate) ? LismaTrue : tx.Predicate;
            var key = CreateTransactionKey(endStateName, predicate);
            if (!stateBlockModels.TryGetValue(key, out var blockModel))
            {
                blockModel = new StateBlockModel(endStateName, key,
                    statesMap.TryGetValue(endStateName, out var text) ? text : "");
                stateBlockModels[key] = blockModel;
            }

            blockModel.InputStates.Add(startStateName);
        }

        foreach (var block in stateBlockModels.Values)
        {
            AppendFragment(sb, regions, block.StateName, block.ToString(), extraBlankLines: 1);
        }

        foreach (var loop in model.LoopTransactions)
        {
            if (!stateNames.TryGetValue(loop.StateId, out var stateName))
                continue;
            AppendFragment(sb, regions, stateName, LoopToLisma(loop, stateName, statesMap), extraBlankLines: 2);
        }

        return new LismaTextModel(sb.ToString(), regions);
    }

    private static string LoopToLisma(BlueprintLoopTransactionModel loop, string stateName, Dictionary<string, string> statesMap)
    {
        var pseudoStateName = $"{stateName}_pseudo_1";
        return new StringBuilder()
            .AppendLine($"state {pseudoStateName} ({loop.Predicate.Trim()}) {{")
            .AppendLine(loop.Text)
            .AppendLine($"}} from {stateName};")
            .AppendLine()
            .AppendLine($"state {stateName} ({LismaTrue}) {{")
            .AppendLine(statesMap.TryGetValue(stateName, out var text) ? text : "")
            .AppendLine($"}} from {pseudoStateName};")
            .AppendLine()
            .ToString();
    }

    private static void AppendFragment(StringBuilder sb, List<CodeRegion> regions, string name, string fragmentText, int extraBlankLines)
    {
        var startLine = LineCount(sb) + 1;
        sb.AppendLine(fragmentText);
        for (var i = 0; i < extraBlankLines; i++)
        {
            sb.AppendLine();
        }

        var fragmentLines = fragmentText.Split('\n').Length - (fragmentText.EndsWith('\n') ? 1 : 0);
        regions.Add(new CodeRegion(name, startLine, startLine + fragmentLines - 1));
    }

    private static int LineCount(StringBuilder sb) =>
        sb.ToString().Count(c => c == '\n');

    private static string CreateTransactionKey(string targetStateName, string predicate) =>
        $"{targetStateName.Trim()} ({predicate.Trim()})";

    private sealed class StateBlockModel(string stateName, string transactionKey, string text)
    {
        public string StateName { get; } = stateName;

        public List<string> InputStates { get; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder()
                .AppendLine($"state {transactionKey} {{")
                .AppendLine(text)
                .Append('}');

            if (InputStates.Count > 0)
            {
                sb.Append(" from ");
                sb.Append(string.Join(",", InputStates));
            }

            sb.Append(';');
            return sb.ToString();
        }
    }
}
