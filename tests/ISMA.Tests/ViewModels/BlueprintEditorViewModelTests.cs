global using global::Xunit;
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

        viewModel.States.Should().HaveCount(0);

        // Original addState resets the editor mode before adding
        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.AddStateCommand.Execute(null);

        viewModel.Mode.Should().BeOfType<EditorMode.Default>();
        viewModel.States.Should().HaveCount(1);

        var newState = viewModel.States[0];
        newState.Name.Should().StartWith("State");
    }

    [Fact]
    public void AddState_MultipleAdds_IncrementsCount()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();

        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(1);

        viewModel.AddStateCommand.Execute(null);
        viewModel.States.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveState_RemovesSelectedState()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = viewModel.States[0];
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(0);
        viewModel.SelectedState.Should().BeNull();
    }

    [Fact]
    public void RemoveState_NoSelection_DoesNotRemove()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveState();
        viewModel.SelectedState = null;
        viewModel.RemoveStateCommand.Execute(null);

        viewModel.States.Should().HaveCount(1);
    }

    [Fact]
    public void AddTransition_AddsTransitionToCollection()
    {
        var viewModel = CreateViewModel();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        viewModel.Transitions.Should().BeEmpty();

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[1];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);
        viewModel.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [Fact]
    public void AddTransition_MainAndInitCannotBeSource()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.MainState);
        viewModel.SetTransitionSource(viewModel.InitState);
        viewModel.SelectedState = viewModel.MainState;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().BeEmpty();
        viewModel.LoopTransactions.Should().BeEmpty();
    }

    [Fact]
    public void AddTransition_NoSelection_DoesNotAdd()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SelectedState = null;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void AddTransition_WrongMode_DoesNotAdd()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();
        viewModel.SelectedState = viewModel.MainState;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTransition_RemovesSelectedTransition()
    {
        var viewModel = CreateViewModel();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[1];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveTransition();
        viewModel.SelectedTransition = viewModel.Transitions[0];
        viewModel.RemoveTransitionCommand.Execute(null);

        viewModel.Transitions.Should().BeEmpty();
        viewModel.SelectedTransition.Should().BeNull();
    }

    [Fact]
    public void RemoveTransition_NoSelection_DoesNotRemove()
    {
        var viewModel = CreateViewModel();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[1];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);

        viewModel.Mode = new EditorMode.RemoveTransition();
        viewModel.SelectedTransition = null;
        viewModel.RemoveTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);
    }

    [Fact]
    public void GetBlueprintModel_ReturnsModelWithStates()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);

        var model = viewModel.GetBlueprintModel();

        model.States.Should().HaveCount(1);
        model.Main.Name.Should().Be("Main");
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

        viewModel.States.Should().HaveCount(1);
    }

    [Fact]
    public void ResetEditorMode_SetsModeToDefault()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.ResetEditorModeCommand.Execute(null);

        viewModel.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [Fact]
    public void Dispose_ClearsAllCollections()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        viewModel.Mode = new EditorMode.AddTransition();
        viewModel.SetTransitionSource(viewModel.States[0]);
        viewModel.SelectedState = viewModel.States[1];
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Dispose();

        viewModel.States.Should().BeEmpty();
        viewModel.Transitions.Should().BeEmpty();
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

        viewModel.States.Should().HaveCount(1);
        viewModel.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void AddState_DefaultMode_OnlyAddsUserState()
    {
        var viewModel = CreateViewModel();

        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);

        var mainState = viewModel.MainState;
        var initState = viewModel.InitState;
        var userState = viewModel.States.First();

        mainState.Name.Should().Be("Main");
        initState.Name.Should().Be("init");
        userState.Name.Should().StartWith("State");
    }

    [Fact]
    public void AddTransition_SameStateTwice_CreatesLoop()
    {
        var viewModel = CreateViewModel();
        viewModel.AddStateCommand.Execute(null);

        viewModel.Mode = new EditorMode.AddTransition();
        var targetState = viewModel.States[0];
        viewModel.SetTransitionSource(targetState);
        viewModel.SelectedState = targetState;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.LoopTransactions.Should().HaveCount(1);
        viewModel.Transitions.Should().BeEmpty();
        viewModel.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [Fact]
    public void AddTransition_DifferentStates_CreatesInterStateTransition()
    {
        var viewModel = CreateViewModel();
        viewModel.AddStateCommand.Execute(null); // State 1
        viewModel.AddStateCommand.Execute(null); // State 2

        viewModel.Mode = new EditorMode.AddTransition();
        var sourceState = viewModel.States[0];
        var targetState = viewModel.States[1];
        viewModel.SetTransitionSource(sourceState);
        viewModel.SelectedState = targetState;
        viewModel.AddTransitionCommand.Execute(null);

        viewModel.Transitions.Should().HaveCount(1);
        viewModel.LoopTransactions.Should().BeEmpty();
        var tx = viewModel.Transitions[0];
        tx.GetStartState(viewModel.States)!.Name.Should().Be(sourceState.Name);
        tx.GetEndState(viewModel.States)!.Name.Should().Be(targetState.Name);
        viewModel.Mode.Should().BeOfType<EditorMode.Default>();
    }
}
