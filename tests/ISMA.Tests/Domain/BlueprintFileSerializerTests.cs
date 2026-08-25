global using global::Xunit;
using System.Collections.Immutable;
using System.Text.Json;
using FluentAssertions;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class BlueprintFileSerializerTests
{
    private const string OriginalSampleJson = """
        {"main":{"canvasPositionX":10.0,"canvasPositionY":10.0,"name":"Main","text":"v' = -g;\ny' = v;\n\ny(t0) = 10;"},
        "init":{"canvasPositionX":10.0,"canvasPositionY":100.0,"name":"init","text":""},
        "states":[
          {"canvasPositionX":534.8,"canvasPositionY":4.8000000000000504,"name":"Up","text":"set v = -v;"},
          {"canvasPositionX":535.6,"canvasPositionY":300.7999999999999,"name":"Down","text":""}
        ],
        "transactions":[
          {"startStateName":"init","endStateName":"Up","predicate":"y < 0"},
          {"startStateName":"init","endStateName":"Down","predicate":"v < 0"},
          {"startStateName":"Down","endStateName":"Up","predicate":"y < 0"},
          {"startStateName":"Up","endStateName":"Down","predicate":"v < 0"}
        ],
        "loopTransactions":[]}
        """;

    [Fact]
    public void FromJson_OriginalBouncingBallFile_ParsesAllStatesAndTransactions()
    {
        var model = BlueprintFileSerializer.FromJson(OriginalSampleJson);

        model.Main.Name.Should().Be("Main");
        model.Main.CanvasPositionX.Should().Be(10);
        model.Main.CanvasPositionY.Should().Be(10);
        model.Main.Text.Should().Be("v' = -g;\ny' = v;\n\ny(t0) = 10;");

        model.Init.Name.Should().Be("init");
        model.Init.CanvasPositionY.Should().Be(100);

        model.States.Should().HaveCount(2);
        model.States[0].Name.Should().Be("Up");
        model.States[0].Text.Should().Be("set v = -v;");
        model.States[1].Name.Should().Be("Down");

        model.Transactions.Should().HaveCount(4);

        var initToUp = model.Transactions.First(t =>
            NameOf(t.StartStateId, model) == "init" && NameOf(t.EndStateId, model) == "Up");
        initToUp.Predicate.Should().Be("y < 0");
    }

    [Fact]
    public void FromJson_OriginalFile_TransactionReferencesResolveToStateIds()
    {
        var model = BlueprintFileSerializer.FromJson(OriginalSampleJson);

        var up = model.States.First(s => s.Name == "Up");
        var down = model.States.First(s => s.Name == "Down");

        model.Transactions.First(t =>
            NameOf(t.StartStateId, model) == "init" && NameOf(t.EndStateId, model) == "Up")
            .EndStateId.Should().Be(up.Id);
        model.Transactions.First(t => NameOf(t.StartStateId, model) == "Down")
            .EndStateId.Should().Be(up.Id);
        model.Transactions.First(t =>
            NameOf(t.StartStateId, model) == "Up" && NameOf(t.EndStateId, model) == "Down")
            .StartStateId.Should().Be(up.Id);
        down.Id.Should().NotBe(up.Id);
    }

    [Fact]
    public void ToJson_ProducesOriginalSchema_CamelCaseNameBased_NoVersionOrId()
    {
        var model = BlueprintFileSerializer.FromJson(OriginalSampleJson);

        var json = BlueprintFileSerializer.ToJson(model);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            new[] { "main", "init", "states", "transactions", "loopTransactions" });

        var main = root.GetProperty("main");
        main.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            new[] { "canvasPositionX", "canvasPositionY", "name", "text" });

        var tx = root.GetProperty("transactions")[0];
        tx.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            new[] { "startStateName", "endStateName", "predicate", "alias" });
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var model = BuildModelWithLoopAndAliases();

        var json = BlueprintFileSerializer.ToJson(model);
        var restored = BlueprintFileSerializer.FromJson(json);

        restored.Main.Name.Should().Be(model.Main.Name);
        restored.Main.Text.Should().Be(model.Main.Text);
        restored.Init.Name.Should().Be(model.Init.Name);
        restored.States.Should().HaveCount(model.States.Length);
        for (var i = 0; i < model.States.Length; i++)
        {
            restored.States[i].Name.Should().Be(model.States[i].Name);
            restored.States[i].Text.Should().Be(model.States[i].Text);
            restored.States[i].CanvasPositionX.Should().Be(model.States[i].CanvasPositionX);
            restored.States[i].CanvasPositionY.Should().Be(model.States[i].CanvasPositionY);
        }
        restored.Transactions.Should().HaveCount(model.Transactions.Length);
        restored.LoopTransactions.Should().HaveCount(model.LoopTransactions.Length);
        restored.LoopTransactions[0].Alias.Should().Be(model.LoopTransactions[0].Alias);
        restored.LoopTransactions[0].Text.Should().Be(model.LoopTransactions[0].Text);

        var restoredByName = new Dictionary<string, BlueprintStateModel>();
        restoredByName[restored.Main.Name] = restored.Main;
        restoredByName[restored.Init.Name] = restored.Init;
        foreach (var s in restored.States) restoredByName[s.Name] = s;

        foreach (var tx in model.Transactions)
        {
            var startName = NameOf(tx.StartStateId, model);
            var endName = NameOf(tx.EndStateId, model);
            restored.Transactions.Should().Contain(t =>
                NameOf(t.StartStateId, restored) == startName &&
                NameOf(t.EndStateId, restored) == endName &&
                t.Predicate == tx.Predicate &&
                t.Alias == tx.Alias);
        }
    }

    [Fact]
    public void FromJson_TransactionReferencingMissingState_IsDropped()
    {
        var json = """
            {"main":{"canvasPositionX":10,"canvasPositionY":10,"name":"Main","text":""},
             "init":{"canvasPositionX":10,"canvasPositionY":100,"name":"init","text":""},
             "states":[],
             "transactions":[{"startStateName":"Main","endStateName":"Ghost","predicate":"x > 0"}],
             "loopTransactions":[]}
            """;

        var model = BlueprintFileSerializer.FromJson(json);

        model.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void FromJson_UnknownKeys_AreIgnored()
    {
        var json = """
            {"version":99,"id":"abc","main":{"canvasPositionX":10,"canvasPositionY":10,"name":"Main","text":""},
             "init":{"canvasPositionX":10,"canvasPositionY":100,"name":"init","text":""},
             "states":[],"transactions":[],"loopTransactions":[]}
            """;

        var model = BlueprintFileSerializer.FromJson(json);

        model.Main.Name.Should().Be("Main");
    }

    private static BlueprintModel BuildModelWithLoopAndAliases()
    {
        var main = new BlueprintStateModel(10, 10, "Main", "main code");
        var init = new BlueprintStateModel(10, 100, "init", "");
        var a = new BlueprintStateModel(200, 10, "State A", "a code");
        var b = new BlueprintStateModel(200, 200, "State B", "");

        return new BlueprintModel
        {
            Main = main,
            Init = init,
            States = ImmutableArray.Create(a, b),
            Transactions = ImmutableArray.Create(
                new BlueprintTransactionModel { StartStateId = init.Id, EndStateId = a.Id, Predicate = "x > 0", Alias = "go" },
                new BlueprintTransactionModel { StartStateId = a.Id, EndStateId = b.Id, Predicate = "x < 0" }),
            LoopTransactions = ImmutableArray.Create(
                new BlueprintLoopTransactionModel { StateId = a.Id, Predicate = "loop > 0", Alias = "spin", Text = "loop code" })
        };
    }

    private static string NameOf(Guid stateId, BlueprintModel model)
    {
        if (stateId == model.Main.Id) return model.Main.Name;
        if (stateId == model.Init.Id) return model.Init.Name;
        return model.States.FirstOrDefault(s => s.Id == stateId)?.Name ?? "<unknown>";
    }
}
