using System.Collections.Immutable;
using System.Text.Json;
using ISMA.Domain.Models;

namespace ISMA.Domain.Conversion;

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
        var stateNames = new Dictionary<Guid, string>
        {
            [model.Main.Id] = model.Main.Name,
            [model.Init.Id] = model.Init.Name
        };
        foreach (var state in model.States)
        {
            stateNames[state.Id] = state.Name;
        }

        var file = new BlueprintFileModel
        {
            Main = MapState(model.Main),
            Init = MapState(model.Init),
            States = model.States.Select(MapState).ToList(),
            Transactions = model.Transactions
                .Where(tx => stateNames.ContainsKey(tx.StartStateId) && stateNames.ContainsKey(tx.EndStateId))
                .Select(tx => new BlueprintFileTransactionModel
                {
                    StartStateName = stateNames[tx.StartStateId],
                    EndStateName = stateNames[tx.EndStateId],
                    Predicate = tx.Predicate,
                    Alias = tx.Alias
                })
                .ToList(),
            LoopTransactions = model.LoopTransactions
                .Where(loop => stateNames.ContainsKey(loop.StateId))
                .Select(loop => new BlueprintFileLoopTransactionModel
                {
                    StateName = stateNames[loop.StateId],
                    Predicate = loop.Predicate,
                    Alias = loop.Alias,
                    Text = loop.Text
                })
                .ToList()
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
            .ToList();

        var nameToId = new Dictionary<string, Guid>
        {
            [main.Name] = main.Id,
            [init.Name] = init.Id
        };
        foreach (var state in states)
        {
            nameToId[state.Name] = state.Id;
        }

        var transactions = file.Transactions
            .Where(tx => nameToId.TryGetValue(tx.StartStateName, out var startId) && nameToId.TryGetValue(tx.EndStateName, out var endId))
            .Select(tx => new BlueprintTransactionModel
            {
                StartStateId = nameToId[tx.StartStateName],
                EndStateId = nameToId[tx.EndStateName],
                Predicate = tx.Predicate,
                Alias = tx.Alias
            })
            .ToList();

        var loops = file.LoopTransactions
            .Where(loop => nameToId.TryGetValue(loop.StateName, out var _))
            .Select(loop => new BlueprintLoopTransactionModel
            {
                StateId = nameToId[loop.StateName],
                Predicate = loop.Predicate,
                Alias = loop.Alias,
                Text = loop.Text
            })
            .ToList();

        return new BlueprintModel
        {
            Main = main,
            Init = init,
            States = states.ToImmutableArray(),
            Transactions = transactions.ToImmutableArray(),
            LoopTransactions = loops.ToImmutableArray()
        };
    }

    private static BlueprintFileStateModel MapState(BlueprintStateModel state) => new()
    {
        CanvasPositionX = state.CanvasPositionX,
        CanvasPositionY = state.CanvasPositionY,
        Name = state.Name,
        Text = state.Text
    };
}
