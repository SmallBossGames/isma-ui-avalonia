global using global::Xunit;
using System.Collections.Immutable;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class BlueprintModelTests
{
    [Fact]
    public void EmptyModel_DefaultsAreCorrect()
    {
        var model = BlueprintModel.Empty;

        model.Main.CanvasPositionX.Should().Be(10);
        model.Main.CanvasPositionY.Should().Be(10);
        model.Main.Name.Should().Be("main");
        model.Main.Text.Should().Be("");

        model.Init.CanvasPositionX.Should().Be(10);
        model.Init.CanvasPositionY.Should().Be(100);
        model.Init.Name.Should().Be("init");
        model.Init.Text.Should().Be("");

        model.States.Should().BeEmpty();
        model.Transactions.Should().BeEmpty();
        model.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void CreateState_CreatesStateWithPositionAndName()
    {
        var state = new BlueprintStateModel(50, 100, "myState", "some code");

        state.CanvasPositionX.Should().Be(50);
        state.CanvasPositionY.Should().Be(100);
        state.Name.Should().Be("myState");
        state.Text.Should().Be("some code");
    }

    [Fact]
    public void CreateState_WithDefaultConstructor_UsesEmptyDefaults()
    {
        var state = new BlueprintStateModel();

        state.CanvasPositionX.Should().Be(0);
        state.CanvasPositionY.Should().Be(0);
        state.Name.Should().Be("");
        state.Text.Should().Be("");
    }

    [Fact]
    public void CreateTransaction_CreatesTransactionWithStartAndEndStates()
    {
        var transaction = new BlueprintTransactionModel
        {
            StartStateName = "main",
            EndStateName = "myState",
            Predicate = "condition",
            Alias = "myAlias"
        };

        transaction.StartStateName.Should().Be("main");
        transaction.EndStateName.Should().Be("myState");
        transaction.Predicate.Should().Be("condition");
        transaction.Alias.Should().Be("myAlias");
    }

    [Fact]
    public void CreateLoopTransaction_CreatesLoopWithStateAndPredicate()
    {
        var loop = new BlueprintLoopTransactionModel
        {
            StateName = "myState",
            Predicate = "loop condition",
            Alias = "loopAlias",
            Text = "loop code"
        };

        loop.StateName.Should().Be("myState");
        loop.Predicate.Should().Be("loop condition");
        loop.Alias.Should().Be("loopAlias");
        loop.Text.Should().Be("loop code");
    }

    [Fact]
    public void EmptyModel_AllCollectionsAreImmutable()
    {
        var model = BlueprintModel.Empty;

        model.States.IsDefault.Should().BeFalse();
        model.Transactions.IsDefault.Should().BeFalse();
        model.LoopTransactions.IsDefault.Should().BeFalse();

        model.States.Length.Should().Be(0);
        model.Transactions.Length.Should().Be(0);
        model.LoopTransactions.Length.Should().Be(0);
    }

    [Fact]
    public void AddStateToModel_CreatesNewModelWithAddedState()
    {
        var model = BlueprintModel.Empty;
        var newState = new BlueprintStateModel(100, 200, "testState", "");

        var updated = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(newState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        updated.States.Should().HaveCount(1);
        updated.States[0].Name.Should().Be("testState");
        updated.States[0].CanvasPositionX.Should().Be(100);
        updated.States[0].CanvasPositionY.Should().Be(200);
    }

    [Fact]
    public void AddTransactionToModel_CreatesNewModelWithAddedTransaction()
    {
        var model = BlueprintModel.Empty;
        var newTx = new BlueprintTransactionModel
        {
            StartStateName = "main",
            EndStateName = "init",
            Predicate = "condition"
        };

        var updated = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States,
            Transactions = model.Transactions.Add(newTx),
            LoopTransactions = model.LoopTransactions
        };

        updated.Transactions.Should().HaveCount(1);
        updated.Transactions[0].StartStateName.Should().Be("main");
        updated.Transactions[0].EndStateName.Should().Be("init");
    }
}
