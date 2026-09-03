global using global::Xunit;
using FluentAssertions;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

/// <summary>
/// Ported from the original ISMA Kotlin/JavaFX <c>CanvasViewModelTest</c>.
/// </summary>
public class CanvasViewModelTests
{
    private static StateViewModel MakeState(CanvasViewModel canvas, string name, StateKind kind = StateKind.User)
    {
        var state = canvas.CreateState("", 10, 10, name, kind, 110, 65);
        canvas.AddState(state);
        return state;
    }

    [Fact]
    public void AddStateIncreasesCount()
    {
        var canvas = new CanvasViewModel();
        canvas.States.Count.Should().Be(0);

        MakeState(canvas, "State 1");
        MakeState(canvas, "State 2");

        canvas.States.Count.Should().Be(2);
    }

    [Fact]
    public void RemoveStateCascadesToTransactions()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");
        var b = MakeState(canvas, "B");
        canvas.AddTransaction(new TransactionViewModel("A", "B", "p", ""));
        canvas.Transactions.Count.Should().Be(1);

        canvas.RemoveState(a);

        canvas.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStateCascadesToLoopTransactions()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");
        canvas.AddLoopTransaction(new LoopTransactionViewModel("A", "p", "", ""));
        canvas.LoopTransactions.Count.Should().Be(1);

        canvas.RemoveState(a);

        canvas.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTransactionRemovesFromCollection()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");
        var b = MakeState(canvas, "B");
        var tx = new TransactionViewModel("A", "B", "p", "");
        canvas.AddTransaction(tx);
        canvas.Transactions.Count.Should().Be(1);

        canvas.RemoveTransaction(tx);

        canvas.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStateRemovesTransactionFromBothStartAndEnd()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");
        var b = MakeState(canvas, "B");
        var c = MakeState(canvas, "C");
        canvas.AddTransaction(new TransactionViewModel("A", "B", "", ""));
        canvas.AddTransaction(new TransactionViewModel("B", "C", "", ""));
        canvas.Transactions.Count.Should().Be(2);

        canvas.RemoveState(b);

        canvas.Transactions.Should().BeEmpty();
        canvas.States.Should().Contain(a);
        canvas.States.Should().Contain(c);
    }

    [Fact]
    public void ClearAllRemovesEverything()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");
        var b = MakeState(canvas, "B");
        canvas.AddTransaction(new TransactionViewModel("A", "B", "", ""));
        canvas.AddLoopTransaction(new LoopTransactionViewModel("A", "", "", ""));

        canvas.ClearAll();

        canvas.States.Should().BeEmpty();
        canvas.Transactions.Should().BeEmpty();
        canvas.LoopTransactions.Should().BeEmpty();
        canvas.CreateNextDefaultStateName().Should().Be("State 1");
    }

    [Fact]
    public void StateNameUniquenessIsEnforced()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");
        MakeState(canvas, "B");

        a.Name = "B";

        a.Name.Should().Be("A");
    }

    [Fact]
    public void StateCanKeepItsOwnName()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");

        a.Name = "A";

        a.Name.Should().Be("A");
    }

    [Fact]
    public void DefaultStateNamesAreSequential()
    {
        var canvas = new CanvasViewModel();
        MakeState(canvas, canvas.CreateNextDefaultStateName());
        MakeState(canvas, canvas.CreateNextDefaultStateName());

        canvas.CreateNextDefaultStateName().Should().Be("State 3");
    }

    [Fact]
    public void NameCounterRecoversFromRemovedHighNumberedState()
    {
        var canvas = new CanvasViewModel();
        var high = MakeState(canvas, "State 10");

        canvas.RemoveState(high);

        canvas.CreateNextDefaultStateName().Should().Be("State 11");
    }

    [Fact]
    public void StateByNameFindsRegisteredState()
    {
        var canvas = new CanvasViewModel();
        var a = MakeState(canvas, "A");

        canvas.StateByName("A").Should().BeSameAs(a);
        canvas.StateByName("Nope").Should().BeNull();
    }
}
