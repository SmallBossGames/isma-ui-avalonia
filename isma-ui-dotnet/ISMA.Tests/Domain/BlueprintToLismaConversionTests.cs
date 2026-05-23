using Xunit;
using FluentAssertions;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class BlueprintToLismaConversionTests
{
    [Fact]
    public void Convert_EmptyBlueprint_GeneratesMainAndInitStates()
    {
        var model = BlueprintModel.Empty;

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.FullText.Should().Contain("state main {");
        result.FullText.Should().Contain("state init {");
        result.FullText.Should().Contain("}");

        result.Regions.Should().HaveCount(2);
        result.Regions[0].Name.Should().Be("main");
        result.Regions[1].Name.Should().Be("init");
    }

    [Fact]
    public void Convert_SingleUserState_GeneratesMainInitAndUserState()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "userState", "user code");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.FullText.Should().Contain("state main {");
        result.FullText.Should().Contain("state init {");
        result.FullText.Should().Contain("state userState {");
        result.FullText.Should().Contain("user code");
        result.FullText.Should().Contain("}");

        result.Regions.Should().HaveCount(3);
        result.Regions[0].Name.Should().Be("main");
        result.Regions[1].Name.Should().Be("init");
        result.Regions[2].Name.Should().Be("userState");
    }

    [Fact]
    public void Convert_MultipleStatesWithTransitions_GeneratesCorrectOutput()
    {
        var model = BlueprintModel.Empty;
        var state1 = new BlueprintStateModel(100, 200, "state1", "code1");
        var state2 = new BlueprintStateModel(150, 250, "state2", "code2");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(state1).Add(state2),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var tx1 = new BlueprintTransactionModel
        {
            StartStateName = "state1",
            EndStateName = "state2",
            Predicate = "cond1"
        };
        var tx2 = new BlueprintTransactionModel
        {
            StartStateName = "state2",
            EndStateName = "state1",
            Predicate = "cond2"
        };
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States,
            Transactions = model.Transactions.Add(tx1).Add(tx2),
            LoopTransactions = model.LoopTransactions
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.FullText.Should().Contain("state main {");
        result.FullText.Should().Contain("state init {");
        result.FullText.Should().Contain("state state1 {");
        result.FullText.Should().Contain("state state2 {");

        result.FullText.Should().Contain("from state1");
        result.FullText.Should().Contain("from state2");

        result.Regions.Should().HaveCount(6);
    }

    [Fact]
    public void Convert_LoopTransitions_GeneratesPseudoStatePattern()
    {
        var model = BlueprintModel.Empty;
        var loopState = new BlueprintLoopTransactionModel
        {
            StateName = "state1",
            Predicate = "loopPred",
            Alias = "",
            Text = ""
        };
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States,
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions.Add(loopState)
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.FullText.Should().Contain("state state1_pseudo_1 (loopPred) {");
        result.FullText.Should().Contain("  from state1;");
        result.FullText.Should().Contain("state state1 (1 > 0) {");
        result.FullText.Should().Contain("  from state1_pseudo_1;");
    }

    [Fact]
    public void Convert_MultipleTransitionsToSameTargetWithSamePredicate_MergesIntoSingleRegion()
    {
        var model = BlueprintModel.Empty;
        var state1 = new BlueprintStateModel(100, 200, "state1", "");
        var state2 = new BlueprintStateModel(150, 250, "state2", "");
        var targetState = new BlueprintStateModel(200, 300, "target", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(state1).Add(state2).Add(targetState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var tx1 = new BlueprintTransactionModel
        {
            StartStateName = "state1",
            EndStateName = "target",
            Predicate = "cond"
        };
        var tx2 = new BlueprintTransactionModel
        {
            StartStateName = "state2",
            EndStateName = "target",
            Predicate = "cond"
        };
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States,
            Transactions = model.Transactions.Add(tx1).Add(tx2),
            LoopTransactions = model.LoopTransactions
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.Regions.Count(r => r.Name.Contains("target (cond)")).Should().Be(1);
        result.FullText.Should().Contain("from state1");
    }

    [Fact]
    public void Convert_TransitionsWithDifferentPredicates_CreatesSeparateRegions()
    {
        var model = BlueprintModel.Empty;
        var state1 = new BlueprintStateModel(100, 200, "state1", "");
        var targetState = new BlueprintStateModel(200, 300, "target", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(state1).Add(targetState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var tx1 = new BlueprintTransactionModel
        {
            StartStateName = "state1",
            EndStateName = "target",
            Predicate = "cond1"
        };
        var tx2 = new BlueprintTransactionModel
        {
            StartStateName = "state1",
            EndStateName = "target",
            Predicate = "cond2"
        };
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States,
            Transactions = model.Transactions.Add(tx1).Add(tx2),
            LoopTransactions = model.LoopTransactions
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.Regions.Count(r => r.Name.Contains("target (cond1)")).Should().Be(1);
        result.Regions.Count(r => r.Name.Contains("target (cond2)")).Should().Be(1);
    }

    [Fact]
    public void Convert_StateWithText_IncludesTextInOutput()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "myState", "line1\nline2\nline3");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.FullText.Should().Contain("state myState {");
        result.FullText.Should().Contain("line1");
        result.FullText.Should().Contain("line2");
        result.FullText.Should().Contain("line3");
    }

    [Fact]
    public void Convert_ReturnsNonEmptyFullText()
    {
        var model = BlueprintModel.Empty;
        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        result.FullText.Should().NotBeNullOrEmpty();
        result.Regions.Should().NotBeEmpty();
    }

    [Fact]
    public void Convert_AllRegionsHaveValidLineNumbers()
    {
        var model = BlueprintModel.Empty;
        var state1 = new BlueprintStateModel(100, 200, "state1", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(state1),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var result = BlueprintToLismaConverter.ConvertToLisma(model);

        foreach (var region in result.Regions)
        {
            region.StartLine.Should().BeGreaterThanOrEqualTo(0);
            region.EndLine.Should().BeGreaterThanOrEqualTo(region.StartLine);
        }
    }
}
