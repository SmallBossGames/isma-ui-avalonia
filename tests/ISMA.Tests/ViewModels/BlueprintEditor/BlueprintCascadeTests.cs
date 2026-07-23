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
        viewModel.States.Should().HaveCount(1);

        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(2);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[1];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[0];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(1);
        viewModel.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveState_RemovesAssociatedLoops()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(1);

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateId = viewModel.States[0].Id,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);
        viewModel.LoopTransactions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[0];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(0);
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
        viewModel.States.Should().HaveCount(3);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[1];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[2];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(2);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[0];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(2);
        viewModel.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMainState_IsNoOp()
    {
        var viewModel = CreateViewModel();
        var initialMain = viewModel.MainState;
        initialMain.Should().NotBeNull("Main state should exist");

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = initialMain;
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.MainState.Should().Be(initialMain, "Main state should not be removable");
    }

    [Fact]
    public void RemoveInitState_IsNoOp()
    {
        var viewModel = CreateViewModel();
        var initialInit = viewModel.InitState;
        initialInit.Should().NotBeNull("Init state should exist");

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = initialInit;
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.InitState.Should().Be(initialInit, "Init state should not be removable");
    }
}
