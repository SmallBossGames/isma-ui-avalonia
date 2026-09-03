global using global::Xunit;
using System.Collections.Immutable;
using System.Text.Json;
using FluentAssertions;
using ISMA.App.Services.Blueprint;
using ISMA.BlueprintEditor.Models;

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

        var initToUp = model.Transactions.First(t => t.StartStateName == "init" && t.EndStateName == "Up");
        initToUp.Predicate.Should().Be("y < 0");
    }

    [Fact]
    public void FromJson_OriginalFile_TransactionReferencesMatchStateNames()
    {
        var model = BlueprintFileSerializer.FromJson(OriginalSampleJson);

        var knownNames = new[] { model.Main.Name, model.Init.Name }
            .Concat(model.States.Select(s => s.Name));

        foreach (var tx in model.Transactions)
        {
            knownNames.Should().Contain(tx.StartStateName);
            knownNames.Should().Contain(tx.EndStateName);
        }

        model.Transactions.First(t => t.StartStateName == "init" && t.EndStateName == "Up").Predicate.Should().Be("y < 0");
        model.Transactions.First(t => t.StartStateName == "Down").EndStateName.Should().Be("Up");
        model.Transactions.First(t => t.StartStateName == "Up" && t.EndStateName == "Down").Predicate.Should().Be("v < 0");
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

        foreach (var tx in model.Transactions)
        {
            restored.Transactions.Should().Contain(t =>
                t.StartStateName == tx.StartStateName &&
                t.EndStateName == tx.EndStateName &&
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

        return new BlueprintModel(
            main,
            init,
            ImmutableArray.Create(a, b),
            ImmutableArray.Create(
                new BlueprintTransactionModel("init", "State A", "x > 0", "go"),
                new BlueprintTransactionModel("State A", "State B", "x < 0")),
            ImmutableArray.Create(
                new BlueprintLoopTransactionModel("State A", "loop > 0", "spin", "loop code")));
    }
}
