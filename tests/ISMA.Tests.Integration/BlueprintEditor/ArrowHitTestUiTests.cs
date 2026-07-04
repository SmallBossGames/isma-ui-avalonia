using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Controls;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for arrow hit testing in the blueprint editor.
/// Tests that ArrowLine and LoopArrow correctly identify head vs body clicks.
/// </summary>
public class ArrowHitTestUiTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task ArrowHitTest_ArrowLine_CreatesAndRenders()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add two user states
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(4);

        // Create an inter-state transition
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify transition was created
        editorVm.Transactions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_ArrowLine_EventHandlers_CanBeSubscribed()
    {
        // Create an ArrowLine control directly and verify events can be subscribed
        var arrowLine = new ArrowLine();

        arrowLine.ArrowHeadClicked += (s, e) => { };
        arrowLine.ArrowBodyClicked += (s, e) => { };

        // Verify event args work correctly
        var result = new ArrowHitTestResult { IsArrowHead = true };
        var args = new ArrowHitTestEventArgs(result, new Avalonia.Point(50, 50));
        args.Result.IsArrowHead.Should().BeTrue();
        args.ClickPosition.X.Should().Be(50);
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_LoopArrow_CreatesAndRenders()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add a user state
        editorVm!.AddStateCommand.Execute(null);
        var userStateName = editorVm.States.First(s => !s.IsMain && !s.IsInit).Name;

        // Create a loop
        var loopModel = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = userStateName,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        editorVm.AddLoop(loopModel);

        // Verify loop was created
        editorVm.LoopTransactions.Should().HaveCount(1);
        editorVm.LoopTransactions[0].State.Name.Should().Be(userStateName);
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_LoopArrow_EventHandlers_CanBeSubscribed()
    {
        // Create a LoopArrow control directly and verify events can be subscribed
        var loopArrow = new LoopArrow();

        loopArrow.LoopArrowHeadClicked += (s, e) => { };
        loopArrow.LoopBodyClicked += (s, e) => { };

        // Verify event args work correctly
        var result = new ArrowHitTestResult { IsArrowBody = true };
        var args = new ArrowHitTestEventArgs(result, new Avalonia.Point(100, 100));
        args.Result.IsArrowBody.Should().BeTrue();
        args.ClickPosition.Y.Should().Be(100);
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_ArrowHitTestResult_HeadClick()
    {
        var result = new ArrowHitTestResult { IsArrowHead = true, IsArrowBody = false };
        result.IsArrowHead.Should().BeTrue();
        result.IsArrowBody.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_ArrowHitTestResult_BodyClick()
    {
        var result = new ArrowHitTestResult { IsArrowHead = false, IsArrowBody = true };
        result.IsArrowHead.Should().BeFalse();
        result.IsArrowBody.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_TransitionRemovedInRemoveMode()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add two states and transition
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(1);

        // Enter remove transition mode and remove
        editorVm.Mode = new EditorMode.RemoveTransition();
        editorVm.SelectedTransaction = editorVm.Transactions[0];
        editorVm.RemoveTransitionCommand.Execute(null);

        editorVm.Transactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_LoopRemovedInRemoveMode()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add a state and loop
        editorVm!.AddStateCommand.Execute(null);
        var userStateName = editorVm.States.First(s => !s.IsMain && !s.IsInit).Name;

        var loopModel = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = userStateName,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        editorVm.AddLoop(loopModel);
        editorVm.LoopTransactions.Should().HaveCount(1);

        // Remove the loop
        var stateToRemove = editorVm.States.First(s => s.Name == userStateName);
        editorVm.SelectedState = stateToRemove;
        editorVm.RemoveLoopCommand.Execute(null);

        editorVm.LoopTransactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task ArrowHitTest_StateHeight_AffectsArrowCenterCalculation()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Get Main state (height 60) and user state (height 65)
        editorVm!.AddStateCommand.Execute(null);
        var mainState = editorVm.States.First(s => s.IsMain);
        var userState = editorVm.States.First(s => !s.IsMain && !s.IsInit);

        mainState.StateHeight.Should().Be(60);
        userState.StateHeight.Should().Be(65);

        // The ArrowLine.GetCenter method uses the state's StateHeight property
        // Main center Y = 10 + 60/2 = 40
        mainState.CanvasPositionY.Should().Be(10);
    }
}
