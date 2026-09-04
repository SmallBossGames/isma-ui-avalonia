global using global::Xunit;
using FluentAssertions;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

/// <summary>
/// Ported from the original ISMA Kotlin/JavaFX <c>IsmaBlueprintViewModelTest</c>.
/// </summary>
public class IsmaBlueprintViewModelTests
{
    private readonly IsmaBlueprintViewModel _vm = new();

    private StateViewModel UserState() => _vm.AddState();

    [Fact]
    public void MainAndInitStatesExistOnCreation()
    {
        _vm.CanvasViewModel.StateByName("Main").Should().NotBeNull();
        _vm.CanvasViewModel.StateByName("init").Should().NotBeNull();
        _vm.CanvasViewModel.StateByName("Main")!.Kind.Should().Be(StateKind.Main);
        _vm.CanvasViewModel.StateByName("init")!.Kind.Should().Be(StateKind.Init);
    }

    [Fact]
    public void ToggleAddTransitionIsIdempotent()
    {
        _vm.ToggleAddTransition();
        _vm.EditorMode.Should().BeOfType<EditorMode.AddTransition>();

        _vm.ToggleAddTransition();
        _vm.EditorMode.Should().BeSameAs(EditorMode.Idle);
    }

    [Fact]
    public void ToggleRemoveStateIsIdempotent()
    {
        _vm.ToggleRemoveState();
        _vm.EditorMode.Should().BeOfType<EditorMode.RemoveState>();

        _vm.ToggleRemoveState();
        _vm.EditorMode.Should().BeSameAs(EditorMode.Idle);
    }

    [Fact]
    public void ToggleRemoveTransitionIsIdempotent()
    {
        _vm.ToggleRemoveTransition();
        _vm.EditorMode.Should().BeOfType<EditorMode.RemoveTransition>();

        _vm.ToggleRemoveTransition();
        _vm.EditorMode.Should().BeSameAs(EditorMode.Idle);
    }

    [Fact]
    public void AddStateResetsModeAndCreatesUserStateWithDefaultName()
    {
        _vm.ToggleAddTransition();

        var state = UserState();

        _vm.EditorMode.Should().BeSameAs(EditorMode.Idle);
        state.Name.Should().Be("State 1");
        state.Kind.Should().Be(StateKind.User);
    }

    [Fact]
    public void HandleStateClickInIdleModeStartsEditForUserStateOnly()
    {
        var user = UserState();
        var main = _vm.CanvasViewModel.StateByName("Main")!;

        _vm.HandleStateClick(user);
        _vm.HandleStateClick(main);

        user.EditMode.Should().BeTrue();
        main.EditMode.Should().BeFalse();
    }

    [Fact]
    public void HandleStateClickInAddTransitionModeRecordsTransitionSource()
    {
        var a = UserState();
        var b = UserState();
        _vm.ToggleAddTransition();

        _vm.HandleStateClick(a);
        _vm.CanvasViewModel.Transactions.Should().BeEmpty();

        _vm.HandleStateClick(b);

        _vm.CanvasViewModel.Transactions.Count.Should().Be(1);
        _vm.CanvasViewModel.Transactions[0].StartStateName.Should().Be(a.Name);
        _vm.CanvasViewModel.Transactions[0].EndStateName.Should().Be(b.Name);
        _vm.EditorMode.Should().BeSameAs(EditorMode.Idle);
    }

    [Fact]
    public void HandleStateClickInAddTransitionModeWithSameStateCreatesLoopArrow()
    {
        var a = UserState();
        _vm.ToggleAddTransition();

        _vm.HandleStateClick(a);
        _vm.HandleStateClick(a);

        _vm.CanvasViewModel.LoopTransactions.Count.Should().Be(1);
        _vm.CanvasViewModel.LoopTransactions[0].StateName.Should().Be(a.Name);
        _vm.CanvasViewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void HandleStateClickInRemoveStateModeRemovesUserState()
    {
        var a = UserState();
        _vm.ToggleRemoveState();

        _vm.HandleStateClick(a);

        _vm.CanvasViewModel.States.Should().NotContain(a);
    }

    [Fact]
    public void RemoveStateProtectsMainAndInit()
    {
        var main = _vm.CanvasViewModel.StateByName("Main")!;
        var init = _vm.CanvasViewModel.StateByName("init")!;

        _vm.RemoveState(main);
        _vm.RemoveState(init);

        _vm.CanvasViewModel.StateByName("Main").Should().NotBeNull();
        _vm.CanvasViewModel.StateByName("init").Should().NotBeNull();
    }

    [Fact]
    public void DuplicateTransactionBetweenSameStatesIsNotAdded()
    {
        var a = UserState();
        var b = UserState();

        _vm.AddTransactionArrow(a, b, "", "");
        _vm.AddTransactionArrow(a, b, "", "");

        _vm.CanvasViewModel.Transactions.Count.Should().Be(1);
    }

    [Fact]
    public void DuplicateLoopArrowForSameStateIsNotAdded()
    {
        var a = UserState();

        _vm.AddLoopArrow(a, "", "", "");
        _vm.AddLoopArrow(a, "", "", "");

        _vm.CanvasViewModel.LoopTransactions.Count.Should().Be(1);
    }

    [Fact]
    public void HandleArrowBodyClickRemovesOnlyInRemoveTransitionMode()
    {
        var a = UserState();
        var b = UserState();
        _vm.AddTransactionArrow(a, b, "", "");

        _vm.HandleArrowBodyClick(_vm.CanvasViewModel.Transactions[0]);
        _vm.CanvasViewModel.Transactions.Count.Should().Be(1);

        _vm.ToggleRemoveTransition();
        _vm.HandleArrowBodyClick(_vm.CanvasViewModel.Transactions[0]);
        _vm.CanvasViewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void HandleArrowheadClickReturnsTrueInNormalMode()
    {
        var a = UserState();
        var b = UserState();
        _vm.AddTransactionArrow(a, b, "", "");

        _vm.HandleArrowheadClick(_vm.CanvasViewModel.Transactions[0]).Should().BeTrue();
        _vm.CanvasViewModel.Transactions.Count.Should().Be(1);
    }

    [Fact]
    public void HandleArrowheadClickRemovesAndReturnsFalseInRemoveTransitionMode()
    {
        var a = UserState();
        var b = UserState();
        _vm.AddTransactionArrow(a, b, "", "");
        _vm.ToggleRemoveTransition();

        _vm.HandleArrowheadClick(_vm.CanvasViewModel.Transactions[0]).Should().BeFalse();
        _vm.CanvasViewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void HandleLoopArrowBodyClickRemovesOnlyInRemoveTransitionMode()
    {
        var a = UserState();
        _vm.AddLoopArrow(a, "", "", "");

        _vm.HandleLoopArrowBodyClick(_vm.CanvasViewModel.LoopTransactions[0]);
        _vm.CanvasViewModel.LoopTransactions.Count.Should().Be(1);

        _vm.ToggleRemoveTransition();
        _vm.HandleLoopArrowBodyClick(_vm.CanvasViewModel.LoopTransactions[0]);
        _vm.CanvasViewModel.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void HandleStateDoubleClickFiresOpenStateEditorEvent()
    {
        var a = UserState();

        _vm.HandleStateDoubleClick(a);

        _vm.Evt.Should().BeOfType<BlueprintEvent.OpenStateEditor>()
            .Which.State.Should().BeSameAs(a);
    }

    [Fact]
    public void HandleLoopArrowheadDoubleClickFiresOpenLoopEditorEvent()
    {
        var a = UserState();
        _vm.AddLoopArrow(a, "", "", "");
        var loop = _vm.CanvasViewModel.LoopTransactions[0];

        _vm.HandleLoopArrowheadDoubleClick(loop, a);

        _vm.Evt.Should().BeOfType<BlueprintEvent.OpenLoopEditor>()
            .Which.Loop.Should().BeSameAs(loop);
    }

    [Fact]
    public void CommitNameEditUpdatesNameAndExitsEditMode()
    {
        var a = UserState();
        a.StartEdit();

        _vm.CommitNameEdit(a, "Renamed");

        a.Name.Should().Be("Renamed");
        a.EditMode.Should().BeFalse();
    }

    [Fact]
    public void CommitNameEditIgnoresDuplicateName()
    {
        var a = UserState();
        var b = UserState();
        a.StartEdit();

        _vm.CommitNameEdit(a, b.Name);

        a.Name.Should().NotBe(b.Name);
    }

    [Fact]
    public void ButtonTextsFollowEditorMode()
    {
        _vm.AddTransitionButtonText.Should().Be("New transition");
        _vm.RemoveStateButtonText.Should().Be("Remove state");
        _vm.RemoveTransitionButtonText.Should().Be("Remove transition");

        _vm.ToggleAddTransition();
        _vm.AddTransitionButtonText.Should().Be("Stop adding transaction");

        _vm.ResetMode();
        _vm.AddTransitionButtonText.Should().Be("New transition");
    }

    [Fact]
    public void BlueprintModelRoundTripPreservesStatesAndTransactions()
    {
        var a = UserState();
        var b = UserState();
        _vm.AddTransactionArrow(a, b, "p", "alias");
        _vm.AddLoopArrow(a, "loop body", "lp", "la");
        a.Text = "state body";

        var model = _vm.ToBlueprintModel();

        var roundTrip = new IsmaBlueprintViewModel();
        roundTrip.FromBlueprintModel(model);

        roundTrip.CanvasViewModel.States.Count(s => s.Kind == StateKind.User).Should().Be(2);
        roundTrip.CanvasViewModel.StateByName(a.Name)!.Text.Should().Be("state body");
        roundTrip.CanvasViewModel.Transactions.Count.Should().Be(1);
        roundTrip.CanvasViewModel.Transactions[0].Predicate.Should().Be("p");
        roundTrip.CanvasViewModel.Transactions[0].Alias.Should().Be("alias");
        roundTrip.CanvasViewModel.LoopTransactions.Count.Should().Be(1);
        roundTrip.CanvasViewModel.LoopTransactions[0].Text.Should().Be("loop body");
    }

    [Fact]
    public void CommitNameEditPropagatesToTransactionsAndLoops()
    {
        var a = UserState();
        var b = UserState();
        _vm.AddTransactionArrow(a, b, "p", "");
        _vm.AddLoopArrow(a, "loop body", "lp", "la");

        _vm.CommitNameEdit(a, "Renamed");

        a.Name.Should().Be("Renamed");
        _vm.CanvasViewModel.Transactions[0].StartStateName.Should().Be("Renamed");
        _vm.CanvasViewModel.Transactions[0].EndStateName.Should().Be(b.Name);
        _vm.CanvasViewModel.LoopTransactions[0].StateName.Should().Be("Renamed");
    }

    [Fact]
    public void RemoveStateAfterRenameStillCascadesToLoop()
    {
        var a = UserState();
        _vm.AddLoopArrow(a, "loop body", "lp", "");
        _vm.CommitNameEdit(a, "Renamed");

        _vm.RemoveState(a);

        _vm.CanvasViewModel.LoopTransactions.Should().BeEmpty();
        _vm.CanvasViewModel.States.Should().NotContain(a);
    }

    [Fact]
    public void CommitNameEditKeepsOldNameWhenDuplicate()
    {
        var a = UserState();
        var b = UserState();
        _vm.AddLoopArrow(a, "loop body", "lp", "");

        _vm.CommitNameEdit(a, b.Name);

        a.Name.Should().NotBe(b.Name);
        _vm.CanvasViewModel.LoopTransactions[0].StateName.Should().Be(a.Name);
    }
}
