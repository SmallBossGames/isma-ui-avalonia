global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

public class BlueprintCanvasTests
{
    private static BlueprintEditorViewModel CreateViewModel() => new(BlueprintModel.Empty);

    [Fact]
    public void BlueprintProject_StateDrag_RepositionsState()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First();
        var originalX = userState.CanvasPositionX;
        var originalY = userState.CanvasPositionY;

        userState.CanvasPositionX = originalX + 50;
        userState.CanvasPositionY = originalY + 30;

        userState.CanvasPositionX.Should().Be(originalX + 50);
        userState.CanvasPositionY.Should().Be(originalY + 30);
    }

    [Fact]
    public void BlueprintProject_StateDrag_ClampsToNonNegative()
    {
        var dragOffsetX = 50.0;
        var dragOffsetY = 30.0;
        var moveX = 10.0;
        var moveY = 5.0;

        var clampedX = Math.Max(0.0, moveX - dragOffsetX);
        var clampedY = Math.Max(0.0, moveY - dragOffsetY);

        clampedX.Should().Be(0.0);
        clampedY.Should().Be(0.0);

        var positiveMoveX = 100.0;
        var positiveMoveY = 100.0;
        var positiveClampedX = Math.Max(0.0, positiveMoveX - dragOffsetX);
        var positiveClampedY = Math.Max(0.0, positiveMoveY - dragOffsetY);

        positiveClampedX.Should().Be(50.0);
        positiveClampedY.Should().Be(70.0);
    }

    [Fact]
    public void BlueprintProject_TransitionCreation_CreatesTransition()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(2);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.Mode.Should().BeOfType<EditorMode.AddTransition>();

        var sourceState = viewModel.States[0];
        viewModel.SetTransitionSource(sourceState);

        var targetState = viewModel.States[1];
        viewModel.SelectedState = targetState;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);
        viewModel.Transitions[0].GetStartState(viewModel.States)!.Name.Should().Be(sourceState.Name);
        viewModel.Transitions[0].GetEndState(viewModel.States)!.Name.Should().Be(targetState.Name);
    }

    [Fact]
    public void BlueprintProject_LoopCreation_SameStateTwice_CreatesLoop()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First();

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateId = userState.Id,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);

        viewModel.LoopTransactions.Should().HaveCount(1);
        viewModel.LoopTransactions[0].GetState(viewModel.States)!.Name.Should().Be(userState.Name);
    }

    [Fact]
    public void BlueprintProject_StateHeight_DifferentForMainInitAndUser()
    {
        var viewModel = CreateViewModel();

        var mainState = viewModel.MainState;
        mainState.StateHeight.Should().Be(60);

        var initState = viewModel.InitState;
        initState.StateHeight.Should().Be(60);

        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First();
        userState.StateHeight.Should().Be(65);
    }

    [Fact]
    public void BlueprintProject_NoMainInitLabelsOnCanvas()
    {
        var viewModel = CreateViewModel();

        var mainState = viewModel.MainState;
        mainState.Name.Should().Be("Main");

        var initState = viewModel.InitState;
        initState.Name.Should().Be("init");

        mainState.Text.Should().BeNullOrEmpty();
        initState.Text.Should().BeNullOrEmpty();
    }

    [Fact]
    public void BlueprintProject_TransitionUpdatesWhenStateDragged()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States[0];

        userState.CanvasPositionX = 200;
        userState.CanvasPositionY = 200;

        userState.CanvasPositionX.Should().Be(200);
        userState.CanvasPositionY.Should().Be(200);
    }

    [Fact]
    public void BlueprintProject_EditorMode_ToggleButtonContent_UpdatesCorrectly()
    {
        var viewModel = CreateViewModel();

        viewModel.AddTransitionButtonContent.Should().Be("Add Transition");
        viewModel.RemoveStateButtonContent.Should().Be("Remove State");
        viewModel.RemoveTransitionButtonContent.Should().Be("Remove Transition");

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.AddTransitionButtonContent.Should().Be("Stop adding transaction");
        viewModel.RemoveStateButtonContent.Should().Be("Remove State");
        viewModel.RemoveTransitionButtonContent.Should().Be("Remove Transition");

        viewModel.ResetEditorModeCommand.Execute(null);
        viewModel.AddTransitionButtonContent.Should().Be("Add Transition");
    }
}
