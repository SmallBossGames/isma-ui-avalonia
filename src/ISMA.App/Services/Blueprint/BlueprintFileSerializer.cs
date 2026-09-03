using System.Collections.Immutable;
using System.Text.Json;
using ISMA.BlueprintEditor.Models;

namespace ISMA.App.Services.Blueprint;

/// <summary>
/// Serializes <see cref="BlueprintModel"/> to the original .iscm2 file format:
/// camelCase properties, name-based state references, no version/id fields.
/// </summary>
public static class BlueprintFileSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Serializes the model to the .iscm2 JSON format.
    /// </summary>
    public static string ToJson(BlueprintModel model)
    {
        var file = new BlueprintFileModel
        {
            Main = MapState(model.Main),
            Init = MapState(model.Init),
            States = model.States.Select(MapState).ToList(),
            Transactions = model.Transactions.Select(tx => new BlueprintFileTransactionModel
            {
                StartStateName = tx.StartStateName,
                EndStateName = tx.EndStateName,
                Predicate = tx.Predicate,
                Alias = tx.Alias
            }).ToList(),
            LoopTransactions = model.LoopTransactions.Select(loop => new BlueprintFileLoopTransactionModel
            {
                StateName = loop.StateName,
                Predicate = loop.Predicate,
                Alias = loop.Alias,
                Text = loop.Text
            }).ToList()
        };

        return JsonSerializer.Serialize(file, Options);
    }

    /// <summary>
    /// Deserializes the .iscm2 JSON format. Missing state references are dropped.
    /// </summary>
    public static BlueprintModel FromJson(string json)
    {
        var file = JsonSerializer.Deserialize<BlueprintFileModel>(json, Options) ?? new BlueprintFileModel();

        var main = new BlueprintStateModel(file.Main.CanvasPositionX, file.Main.CanvasPositionY, file.Main.Name, file.Main.Text);
        var init = new BlueprintStateModel(file.Init.CanvasPositionX, file.Init.CanvasPositionY, file.Init.Name, file.Init.Text);
        var states = file.States
            .Select(s => new BlueprintStateModel(s.CanvasPositionX, s.CanvasPositionY, s.Name, s.Text))
            .ToImmutableArray();

        var knownNames = new HashSet<string> { main.Name, init.Name };
        foreach (var state in states)
        {
            knownNames.Add(state.Name);
        }

        var transactions = file.Transactions
            .Where(tx => knownNames.Contains(tx.StartStateName) && knownNames.Contains(tx.EndStateName))
            .Select(tx => new BlueprintTransactionModel(tx.StartStateName, tx.EndStateName, tx.Predicate, tx.Alias))
            .ToImmutableArray();

        var loops = file.LoopTransactions
            .Where(loop => knownNames.Contains(loop.StateName))
            .Select(loop => new BlueprintLoopTransactionModel(loop.StateName, loop.Predicate, loop.Alias, loop.Text))
            .ToImmutableArray();

        return new BlueprintModel(main, init, states, transactions, loops);
    }

    private static BlueprintFileStateModel MapState(BlueprintStateModel state) => new()
    {
        CanvasPositionX = state.CanvasPositionX,
        CanvasPositionY = state.CanvasPositionY,
        Name = state.Name,
        Text = state.Text
    };

    /// <summary>
    /// .iscm2 blueprint project file format, compatible with the original ISMA UI.
    /// States are referenced by name.
    /// </summary>
    private sealed class BlueprintFileModel
    {
        public BlueprintFileStateModel Main { get; set; } = new();
        public BlueprintFileStateModel Init { get; set; } = new();
        public List<BlueprintFileStateModel> States { get; set; } = new();
        public List<BlueprintFileTransactionModel> Transactions { get; set; } = new();
        public List<BlueprintFileLoopTransactionModel> LoopTransactions { get; set; } = new();
    }

    /// <summary>
    /// State entry in the .iscm2 file format.
    /// </summary>
    private sealed class BlueprintFileStateModel
    {
        public double CanvasPositionX { get; set; }
        public double CanvasPositionY { get; set; }
        public string Name { get; set; } = "";
        public string Text { get; set; } = "";
    }

    /// <summary>
    /// Transition entry in the .iscm2 file format.
    /// </summary>
    private sealed class BlueprintFileTransactionModel
    {
        public string StartStateName { get; set; } = "";
        public string EndStateName { get; set; } = "";
        public string Predicate { get; set; } = "";
        public string Alias { get; set; } = "";
    }

    /// <summary>
    /// Loop transaction entry in the .iscm2 file format.
    /// </summary>
    private sealed class BlueprintFileLoopTransactionModel
    {
        public string StateName { get; set; } = "";
        public string Predicate { get; set; } = "";
        public string Alias { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
