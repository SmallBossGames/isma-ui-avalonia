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

        // Transactions
        var transactionGroups = new Dictionary<string, (string startState, List<string> predicates)>();
        foreach (var tx in model.Transactions)
        {
            var key = CreateTransactionKey(tx.EndStateName, tx.Predicate);
            if (!transactionGroups.TryGetValue(key, out var group))
            {
                group = (tx.StartStateName, new List<string>());
                transactionGroups[key] = group;
            }
            group.predicates.Add(tx.Predicate);
        }

        foreach (var (key, (startState, predicates)) in transactionGroups)
        {
            var txLines = new List<string>();
            txLines.Add($"state \"{key}\" {{");
            txLines.Add($"  from {startState};");
            txLines.Add("}");
            var txStart = lineIndex;
            lines.AddRange(txLines);
            lineIndex += txLines.Count;
            regions.Add(new CodeRegion(key, txStart, lineIndex - 1));
        }

        // Loop transactions
        foreach (var loop in model.LoopTransactions)
        {
            var pseudoName = $"{loop.StateName}_pseudo_1";
            var pseudoLines = new List<string>();
            pseudoLines.Add($"state {pseudoName} ({loop.Predicate}) {{");
            pseudoLines.Add($"  from {loop.StateName};");
            pseudoLines.Add("}");
            var pseudoStart = lineIndex;
            lines.AddRange(pseudoLines);
            lineIndex += pseudoLines.Count;
            regions.Add(new CodeRegion(pseudoName, pseudoStart, lineIndex - 1));

            var loopStateLines = new List<string>();
            loopStateLines.Add($"state {loop.StateName} (1 > 0) {{");
            loopStateLines.Add($"  from {pseudoName};");
            loopStateLines.Add("}");
            var loopStart = lineIndex;
            lines.AddRange(loopStateLines);
            lineIndex += loopStateLines.Count;
            regions.Add(new CodeRegion(loop.StateName, loopStart, lineIndex - 1));
        }

        var fullText = string.Join('\n', lines);
        return new LismaTextModel(fullText, regions);
    }

    private static string CreateTransactionKey(string targetStateName, string predicate)
    {
        return $"{targetStateName} ({predicate})";
    }
}
