using System.Text;
using ISMA.BlueprintEditor.Models;
using ISMA.Domain.Models;

namespace ISMA.App.Services.Blueprint;

/// <summary>
/// Converts a <see cref="BlueprintModel"/> to LISMA source, ported 1:1 from the
/// original Kotlin <c>LismaCodegen.kt</c>: the main state text first, then one
/// <c>state &lt;target&gt; (&lt;predicate&gt;) { text } from &lt;sources&gt;;</c> block per
/// (target, predicate) group, then loop transactions as pseudo-state pairs.
/// </summary>
public static class LismaCodegen
{
    private const string LismaTrue = "1 > 0";

    /// <summary>
    /// Converts the blueprint model to a LISMA text model with code regions.
    /// </summary>
    public static LismaTextModel ToLismaText(BlueprintModel model)
    {
        var regions = new List<CodeRegion>();
        var sb = new StringBuilder();

        var knownNames = new HashSet<string> { model.Main.Name, model.Init.Name };
        foreach (var state in model.States)
        {
            knownNames.Add(state.Name);
        }

        // User state name -> text (main/init are not part of this map, as in the original).
        var statesMap = new Dictionary<string, string>();
        foreach (var state in model.States)
        {
            statesMap[state.Name] = state.Text;
        }

        sb.AppendLine(model.Main.Text);

        var stateBlockModels = new Dictionary<string, StateBlockModel>();
        // Explicit insertion order (matches the original's LinkedHashMap guarantee;
        // Dictionary order is not part of the contract).
        var blockOrder = new List<string>();
        foreach (var tx in model.Transactions)
        {
            if (!knownNames.Contains(tx.EndStateName))
                continue;
            if (!knownNames.Contains(tx.StartStateName))
                continue;

            var predicate = string.IsNullOrWhiteSpace(tx.Predicate) ? LismaTrue : tx.Predicate;
            var key = CreateTransactionKey(tx.EndStateName, predicate);
            if (!stateBlockModels.TryGetValue(key, out var blockModel))
            {
                blockModel = new StateBlockModel(tx.EndStateName, key,
                    statesMap.TryGetValue(tx.EndStateName, out var text) ? text : "");
                stateBlockModels[key] = blockModel;
                blockOrder.Add(key);
            }

            blockModel.InputStates.Add(tx.StartStateName);
        }

        foreach (var key in blockOrder)
        {
            var block = stateBlockModels[key];
            AppendFragment(sb, regions, block.StateName, block.ToString(), extraBlankLines: 1);
        }

        foreach (var loop in model.LoopTransactions)
        {
            if (!knownNames.Contains(loop.StateName))
                continue;
            AppendFragment(sb, regions, loop.StateName, LoopToLisma(loop, loop.StateName, statesMap), extraBlankLines: 2);
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
