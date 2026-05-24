using Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels;

public class BlueprintEditorViewModelTests
{
    private BlueprintEditorViewModel CreateViewModel() => new(BlueprintModel.Empty);

    [Fact]
    public void AddState_AddsStateToCollection()
    {
        var viewModel = CreateViewModel();

        viewModel.States.Should().HaveCount(2);

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.AddStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(2);

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.AddStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(3);

        var newState = viewModel.States[2];
        newState.Name.Should().StartWith("State");
    }

    [Fact]
    public void AddState_MultipleAdds_IncrementsCount()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;

        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(3);

        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(4);
    }

    [Fact]
    public void RemoveState_RemovesSelectedState()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.AddStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(3);

        viewModel.CurrentMode = BlueprintEditorMode.RemoveState;
        viewModel.SelectedState = viewModel.States[2];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(2);
        viewModel.SelectedState.Should().BeNull();
    }

    [Fact]
    public void RemoveState_NoSelection_DoesNotRemove()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.AddStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(3);

        viewModel.CurrentMode = BlueprintEditorMode.RemoveState;
        viewModel.SelectedState = null;
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(3);
    }

    [Fact]
    public void AddTransition_AddsTransitionToCollection()
    {
        var viewModel = CreateViewModel();

        viewModel.Transactions.Should().BeEmpty();

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.SelectedState = viewModel.States[0];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);
        viewModel.CurrentMode.Should().Be(BlueprintEditorMode.Default);
    }

    [Fact]
    public void AddTransition_NoSelection_DoesNotAdd()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.SelectedState = null;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void AddTransition_WrongMode_DoesNotAdd()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.SelectedState = viewModel.States[0];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTransition_RemovesSelectedTransition()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.SelectedState = viewModel.States[0];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);

        viewModel.CurrentMode = BlueprintEditorMode.RemoveTransition;
        viewModel.SelectedTransaction = viewModel.Transactions[0];
        viewModel.RemoveTransitionCommand.Execute(null);

        viewModel.Transactions.Should().BeEmpty();
        viewModel.SelectedTransaction.Should().BeNull();
    }

    [Fact]
    public void RemoveTransition_NoSelection_DoesNotRemove()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.SelectedState = viewModel.States[0];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);

        viewModel.CurrentMode = BlueprintEditorMode.RemoveTransition;
        viewModel.SelectedTransaction = null;
        viewModel.RemoveTransitionCommand.Execute(null);

        viewModel.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void GetBlueprintModel_ReturnsModelWithStates()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.AddStateCommand.Execute(null);

        var model = viewModel.GetBlueprintModel();

        model.States.Should().HaveCount(1);
        model.Main.Name.Should().Be("main");
        model.Init.Name.Should().Be("init");
    }

    [Fact]
    public void SetBlueprintModel_LoadsModelIntoViewModel()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "loadedState", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var viewModel = CreateViewModel();
        viewModel.SetBlueprintModel(model);

        viewModel.States.Should().HaveCount(3);
    }

    [Fact]
    public void ResetEditorMode_SetsModeToDefault()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.ResetEditorModeCommand.Execute(null);

        viewModel.CurrentMode.Should().Be(BlueprintEditorMode.Default);
    }

    [Fact]
    public void Dispose_ClearsAllCollections()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        viewModel.SelectedState = viewModel.States[0];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Dispose();

        viewModel.States.Should().BeEmpty();
        viewModel.Transactions.Should().BeEmpty();
        viewModel.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithModel_InitializesFromModel()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "testState", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var viewModel = new BlueprintEditorViewModel(model);

        viewModel.States.Should().HaveCount(3);
        viewModel.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void AddState_DefaultMode_OnlyAddsUserState()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.Default;
        viewModel.AddStateCommand.Execute(null);

        var mainState = viewModel.States.First(s => s.IsMain);
        var initState = viewModel.States.First(s => s.IsInit);
        var userState = viewModel.States.First(s => !s.IsMain && !s.IsInit);

        mainState.Name.Should().Be("main");
        initState.Name.Should().Be("init");
        userState.Name.Should().StartWith("State");
    }

    [Fact]
    public void AddTransition_TransitionReferencesCorrectStates()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMode = BlueprintEditorMode.AddTransition;
        var targetState = viewModel.States[1];
        viewModel.SelectedState = targetState;
        viewModel.AddTransitionCommand.Execute(null);

        var tx = viewModel.Transactions[0];
        tx.StartState.Name.Should().Be(targetState.Name);
        tx.EndState.Name.Should().Be(targetState.Name);
    }
}
