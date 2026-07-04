global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

public class BlueprintCascadeTests
{
    private static BlueprintEditorViewModel CreateViewModel() => new(BlueprintModel.Empty);

    [Fact]
    public void RemoveState_RemovesAssociatedTransactions()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(3);

        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(4);

        viewModel.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        viewModel.SetTransitionSource(viewModel.States[2]);
        viewModel.SelectedState = viewModel.States[3];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[2];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(3);
        viewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveState_RemovesAssociatedLoops()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(3);

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateName = viewModel.States[2].Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);
        viewModel.LoopTransactions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[2];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(2);
        viewModel.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveState_WithMultipleTransactions_RemovesAll()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(5);

        viewModel.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        viewModel.SetTransitionSource(viewModel.States[2]);
        viewModel.SelectedState = viewModel.States[3];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        viewModel.SetTransitionSource(viewModel.States[2]);
        viewModel.SelectedState = viewModel.States[4];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(2);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[2];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(4);
        viewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMainState_IsNoOp()
    {
        var viewModel = CreateViewModel();
        int initialCount = viewModel.States.Count;
        viewModel.States.Should().Contain(s => s.IsMain);

        viewModel.Mode = new EditorMode.RemoveState();
        var mainState = viewModel.States.First(s => s.IsMain);
        viewModel.SelectedState = mainState;
        viewModel.RemoveStateCommand.Execute(null);

        var mainStateAfter = viewModel.States.FirstOrDefault(s => s.IsMain);
        mainStateAfter.Should().NotBeNull("Main state should not be removable");
        viewModel.States.Should().HaveCount(initialCount);
    }

    [Fact]
    public void RemoveInitState_IsNoOp()
    {
        var viewModel = CreateViewModel();
        int initialCount = viewModel.States.Count;
        viewModel.States.Should().Contain(s => s.IsInit);

        viewModel.Mode = new EditorMode.RemoveState();
        var initState = viewModel.States.First(s => s.IsInit);
        viewModel.SelectedState = initState;
        viewModel.RemoveStateCommand.Execute(null);

        var initStateAfter = viewModel.States.FirstOrDefault(s => s.IsInit);
        initStateAfter.Should().NotBeNull("Init state should not be removable");
        viewModel.States.Should().HaveCount(initialCount);
    }
}
