global using global::Xunit;
using System.Collections.Immutable;
using FluentAssertions;
using ISMA.App.Services.Blueprint;
using ISMA.BlueprintEditor.Models;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

/// <summary>
/// Golden tests ported 1:1 from the original Kotlin LismaCodegenTest.
/// </summary>
public class BlueprintToLismaConversionTests
{
    private static BlueprintStateModel State(string name, string text = "") =>
        new(0, 0, name, text);

    private static BlueprintModel Build(
        string mainText,
        BlueprintStateModel[] states,
        (string Start, string End, string Predicate)[] txs,
        (string State, string Predicate, string Text)[]? loops = null)
    {
        var main = State("Main", mainText);
        var init = State("init");

        var transactions = txs
            .Select(t => new BlueprintTransactionModel(t.Start, t.End, t.Predicate))
            .ToImmutableArray();

        var loopTransactions = (loops ?? Array.Empty<(string, string, string)>())
            .Select(l => new BlueprintLoopTransactionModel(l.State, l.Predicate, "", l.Text))
            .ToImmutableArray();

        return new BlueprintModel(main, init, states.ToImmutableArray(), transactions, loopTransactions);
    }

    [Fact]
    public void StateBlocks_AreGeneratedWithCorrectLineRegions()
    {
        var model = Build("main body",
            new[] { State("A", "a body"), State("B", "b body") },
            new[] { ("Main", "A", ""), ("Main", "B", "x > 1") });

        var result = LismaCodegen.ToLismaText(model);

        result.FullText.Should().Be(
            "main body\n" +
            "state A (1 > 0) {\n" +
            "a body\n" +
            "} from Main;\n" +
            "\n" +
            "state B (x > 1) {\n" +
            "b body\n" +
            "} from Main;\n" +
            "\n");

        result.Regions.Should().HaveCount(2);
        result.Regions[0].Name.Should().Be("A");
        result.Regions[0].StartLine.Should().Be(2);
        result.Regions[0].EndLine.Should().Be(4);
        result.Regions[1].Name.Should().Be("B");
        result.Regions[1].StartLine.Should().Be(6);
        result.Regions[1].EndLine.Should().Be(8);
    }

    [Fact]
    public void TransactionsToSameStateWithSamePredicate_AreMerged()
    {
        var model = Build("",
            new[] { State("A", "a body") },
            new[] { ("Main", "A", ""), ("init", "A", "") });

        var result = LismaCodegen.ToLismaText(model);

        result.FullText.Should().Contain("} from Main,init;");
        result.Regions.Should().HaveCount(1);
    }

    [Fact]
    public void StateBlocks_AppearInFirstTransactionOrder()
    {
        var model = Build("",
            new[] { State("A"), State("B") },
            new[] { ("Main", "B", ""), ("Main", "A", "") });

        var result = LismaCodegen.ToLismaText(model);

        var bIndex = result.FullText.IndexOf("state B (", StringComparison.Ordinal);
        var aIndex = result.FullText.IndexOf("state A (", StringComparison.Ordinal);

        bIndex.Should().BeGreaterThanOrEqualTo(0);
        aIndex.Should().BeGreaterThanOrEqualTo(0);
        bIndex.Should().BeLessThan(aIndex);
    }

    [Fact]
    public void LoopTransaction_GeneratesPseudoStateAndRegion()
    {
        var model = Build("main body",
            new[] { State("A", "a body") },
            new[] { ("Main", "A", "") },
            new[] { ("A", "p > 0", "loop body") });

        var result = LismaCodegen.ToLismaText(model);

        result.FullText.Should().Be(
            "main body\n" +
            "state A (1 > 0) {\n" +
            "a body\n" +
            "} from Main;\n" +
            "\n" +
            "state A_pseudo_1 (p > 0) {\n" +
            "loop body\n" +
            "} from A;\n" +
            "\n" +
            "state A (1 > 0) {\n" +
            "a body\n" +
            "} from A_pseudo_1;\n" +
            "\n" +
            "\n" +
            "\n" +
            "\n");

        result.Regions.Should().HaveCount(2);
        result.Regions[0].Name.Should().Be("A");
        result.Regions[0].StartLine.Should().Be(2);
        result.Regions[0].EndLine.Should().Be(4);
        result.Regions[1].Name.Should().Be("A");
        result.Regions[1].StartLine.Should().Be(6);
        result.Regions[1].EndLine.Should().Be(13);
    }

    [Fact]
    public void FragmentNameByLine_MapsLinesToOwningFragment()
    {
        var model = Build("main body",
            new[] { State("A", "a body"), State("B", "b body") },
            new[] { ("Main", "A", ""), ("Main", "B", "x > 1") });

        var result = LismaCodegen.ToLismaText(model);

        result.FragmentNameByLine(2).Should().Be("A");
        result.FragmentNameByLine(4).Should().Be("A");
        result.FragmentNameByLine(6).Should().Be("B");
        result.FragmentNameByLine(8).Should().Be("B");
        result.FragmentNameByLine(1).Should().Be(LismaTextModel.DefaultFragment.Name);
        result.FragmentNameByLine(5).Should().Be(LismaTextModel.DefaultFragment.Name);
        result.FragmentNameByLine(99).Should().Be(LismaTextModel.DefaultFragment.Name);
    }

    [Fact]
    public void EmptyModel_ProducesOnlyMainText()
    {
        var result = LismaCodegen.ToLismaText(BlueprintModel.Empty);

        result.FullText.Should().Be("\n");
        result.Regions.Should().BeEmpty();
    }

    [Fact]
    public void BouncingBallSample_ProducesExpectedLisma()
    {
        var model = BlueprintFileSerializer.FromJson("""
            {"main":{"canvasPositionX":10,"canvasPositionY":10,"name":"Main","text":"v' = -g;\ny' = v;"},
             "init":{"canvasPositionX":10,"canvasPositionY":100,"name":"init","text":""},
             "states":[
               {"canvasPositionX":534.8,"canvasPositionY":4.8,"name":"Up","text":"set v = -v;"},
               {"canvasPositionX":535.6,"canvasPositionY":300.8,"name":"Down","text":""}
             ],
             "transactions":[
               {"startStateName":"init","endStateName":"Up","predicate":"y < 0"},
               {"startStateName":"init","endStateName":"Down","predicate":"v < 0"},
               {"startStateName":"Down","endStateName":"Up","predicate":"y < 0"},
               {"startStateName":"Up","endStateName":"Down","predicate":"v < 0"}
             ],
             "loopTransactions":[]}
            """);

        var result = LismaCodegen.ToLismaText(model);

        result.FullText.Should().Be(
            "v' = -g;\n" +
            "y' = v;\n" +
            "state Up (y < 0) {\n" +
            "set v = -v;\n" +
            "} from init,Down;\n" +
            "\n" +
            "state Down (v < 0) {\n" +
            "\n" +
            "} from init,Up;\n" +
            "\n");
    }
}
