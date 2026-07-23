using System.Text;
using ISMA.Domain.Models;

namespace ISMA.Domain.Conversion;

public static class BlueprintToLismaConverter
{
    public static LismaTextModel ConvertToLisma(BlueprintModel model)
    {
        var lines = new List<string>();
        var regions = new List<CodeRegion>();
        var lineIndex = 0;

        // Build Guid -> Name map from all states
        var stateMap = new Dictionary<Guid, string>
        {
            [model.Main.Id] = model.Main.Name,
            [model.Init.Id] = model.Init.Name
        };
        foreach (var state in model.States)
        {
            stateMap[state.Id] = state.Name;
        }

        // Main state
        var mainLines = new List<string>();
        mainLines.Add($"state {model.Main.Name} {{");
        if (!string.IsNullOrWhiteSpace(model.Main.Text))
        {
            foreach (var line in model.Main.Text.Split('\n'))
                mainLines.Add(line.TrimEnd());
        }
        mainLines.Add("}");
        var mainStart = lineIndex;
        lines.AddRange(mainLines);
        lineIndex += mainLines.Count;
        regions.Add(new CodeRegion(model.Main.Name, mainStart, lineIndex - 1));

        // Init state
        var initLines = new List<string>();
        initLines.Add($"state {model.Init.Name} {{");
        if (!string.IsNullOrWhiteSpace(model.Init.Text))
        {
            foreach (var line in model.Init.Text.Split('\n'))
                initLines.Add(line.TrimEnd());
        }
        initLines.Add("}");
        var initStart = lineIndex;
        lines.AddRange(initLines);
        lineIndex += initLines.Count;
        regions.Add(new CodeRegion(model.Init.Name, initStart, lineIndex - 1));

        // User states
        foreach (var state in model.States)
        {
            var stateLines = new List<string>();
            stateLines.Add($"state {state.Name} {{");
            if (!string.IsNullOrWhiteSpace(state.Text))
            {
                foreach (var line in state.Text.Split('\n'))
                    stateLines.Add(line.TrimEnd());
            }
            stateLines.Add("}");
            var stateStart = lineIndex;
            lines.AddRange(stateLines);
            lineIndex += stateLines.Count;
            regions.Add(new CodeRegion(state.Name, stateStart, lineIndex - 1));
        }

        // Transactions - group by target state and alias/predicate
        var transactionGroups = new Dictionary<string, (string startState, List<(string predicate, string alias)>)>();
        foreach (var tx in model.Transactions)
        {
            if (tx.StartStateId == Guid.Empty || tx.EndStateId == Guid.Empty)
                continue;

            if (!stateMap.TryGetValue(tx.StartStateId, out var startStateName) ||
                !stateMap.TryGetValue(tx.EndStateId, out var endStateName))
                continue;

            var displayKey = !string.IsNullOrEmpty(tx.Alias) ? tx.Alias : CreateTransactionKey(endStateName, tx.Predicate);
            var key = CreateTransactionKey(endStateName, tx.Predicate);

            if (!transactionGroups.TryGetValue(key, out var group))
            {
                group = (startStateName, new List<(string predicate, string alias)>());
                transactionGroups[key] = group;
            }
            group.Item2.Add((tx.Predicate, tx.Alias ?? ""));
        }

        foreach (var (key, (startState, transactions)) in transactionGroups)
        {
            var txLines = new List<string>();
            // Use the display key (alias if available, otherwise targetState (predicate))
            var displayKey = transactions.Count == 1 && !string.IsNullOrEmpty(transactions[0].alias)
                ? transactions[0].alias
                : key;
            txLines.Add($"state \"{displayKey}\" {{");
            txLines.Add($"  from {startState};");
            txLines.Add("}");
            var txStart = lineIndex;
            lines.AddRange(txLines);
            lineIndex += txLines.Count;
            regions.Add(new CodeRegion(displayKey, txStart, lineIndex - 1));
        }

        // Loop transactions - track which user states already had loop regions to avoid duplicates
        var statesWithLoopRegions = new HashSet<string>();
        foreach (var loop in model.LoopTransactions)
        {
            if (loop.StateId == Guid.Empty)
                continue;

            if (!stateMap.TryGetValue(loop.StateId, out var stateName))
                continue;

            var pseudoName = $"{stateName}_pseudo_1";
            var pseudoLines = new List<string>();
            pseudoLines.Add($"state {pseudoName} ({loop.Predicate}) {{");
            pseudoLines.Add($"  from {stateName};");
            pseudoLines.Add("}");
            var pseudoStart = lineIndex;
            lines.AddRange(pseudoLines);
            lineIndex += pseudoLines.Count;
            regions.Add(new CodeRegion(pseudoName, pseudoStart, lineIndex - 1));

            // Only add the loop state region if we haven't already added one for this state
            if (!statesWithLoopRegions.Contains(stateName))
            {
                var loopStateLines = new List<string>();
                loopStateLines.Add($"state {stateName} (1 > 0) {{");
                loopStateLines.Add($"  from {pseudoName};");
                loopStateLines.Add("}");
                var loopStart = lineIndex;
                lines.AddRange(loopStateLines);
                lineIndex += loopStateLines.Count;
                regions.Add(new CodeRegion(stateName, loopStart, lineIndex - 1));
                statesWithLoopRegions.Add(stateName);
            }
        }

        var fullText = string.Join('\n', lines);
        return new LismaTextModel(fullText, regions);
    }

    private static string CreateTransactionKey(string targetStateName, string predicate)
    {
        return $"{targetStateName} ({predicate})";
    }
}
