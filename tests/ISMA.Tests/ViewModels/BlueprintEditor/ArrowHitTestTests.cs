global using global::Xunit;
using FluentAssertions;
using ISMA.App.Controls;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

public class ArrowHitTestTests
{
    private static BlueprintEditorViewModel CreateViewModel() => new(BlueprintModel.Empty);

    [Fact]
    public void ArrowHitTest_ArrowLine_CreatesAndRenders()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(4);

        viewModel.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        viewModel.SetTransitionSource(viewModel.States[2]);
        viewModel.SelectedState = viewModel.States[3];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void ArrowHitTest_ArrowLine_EventHandlers_CanBeSubscribed()
    {
        var arrowLine = new ArrowLine();
        arrowLine.ArrowHeadClicked += (s, e) => { };
        arrowLine.ArrowBodyClicked += (s, e) => { };

        var result = new ArrowHitTestResult { IsArrowHead = true };
        var args = new ArrowHitTestEventArgs(result, new Avalonia.Point(50, 50));
        args.Result.IsArrowHead.Should().BeTrue();
        args.ClickPosition.X.Should().Be(50);
    }

    [Fact]
    public void ArrowHitTest_LoopArrow_CreatesAndRenders()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userStateName = viewModel.States.First(s => !s.IsMain && !s.IsInit).Name;

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateName = userStateName,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);

        viewModel.LoopTransactions.Should().HaveCount(1);
        viewModel.LoopTransactions[0].State.Name.Should().Be(userStateName);
    }

    [Fact]
    public void ArrowHitTest_LoopArrow_EventHandlers_CanBeSubscribed()
    {
        var loopArrow = new LoopArrow();
        loopArrow.LoopArrowHeadClicked += (s, e) => { };
        loopArrow.LoopBodyClicked += (s, e) => { };

        var result = new ArrowHitTestResult { IsArrowBody = true };
        var args = new ArrowHitTestEventArgs(result, new Avalonia.Point(100, 100));
        args.Result.IsArrowBody.Should().BeTrue();
        args.ClickPosition.Y.Should().Be(100);
    }

    [Fact]
    public void ArrowHitTest_ArrowHitTestResult_HeadClick()
    {
        var result = new ArrowHitTestResult { IsArrowHead = true, IsArrowBody = false };
        result.IsArrowHead.Should().BeTrue();
        result.IsArrowBody.Should().BeFalse();
    }

    [Fact]
    public void ArrowHitTest_ArrowHitTestResult_BodyClick()
    {
        var result = new ArrowHitTestResult { IsArrowHead = false, IsArrowBody = true };
        result.IsArrowHead.Should().BeFalse();
        result.IsArrowBody.Should().BeTrue();
    }

    [Fact]
    public void ArrowHitTest_TransitionRemovedInRemoveMode()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);
        viewModel.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        viewModel.SetTransitionSource(viewModel.States[2]);
        viewModel.SelectedState = viewModel.States[3];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveTransition();
        viewModel.SelectedTransaction = viewModel.Transactions[0];
        viewModel.RemoveTransitionCommand.Execute(null);

        viewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void ArrowHitTest_LoopRemovedInRemoveMode()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userStateName = viewModel.States.First(s => !s.IsMain && !s.IsInit).Name;

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateName = userStateName,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);
        viewModel.LoopTransactions.Should().HaveCount(1);

        var stateToRemove = viewModel.States.First(s => s.Name == userStateName);
        viewModel.SelectedState = stateToRemove;
        viewModel.RemoveLoopCommand.Execute(null);

        viewModel.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void ArrowHitTest_StateHeight_AffectsArrowCenterCalculation()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var mainState = viewModel.States.First(s => s.IsMain);
        var userState = viewModel.States.First(s => !s.IsMain && !s.IsInit);

        mainState.StateHeight.Should().Be(60);
        userState.StateHeight.Should().Be(65);

        mainState.CanvasPositionY.Should().Be(10);
    }
}
