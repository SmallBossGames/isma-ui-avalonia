using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.Models;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// The root view model of the blueprint editor: mode state machine, diagram
/// editing commands and persistence (to/from <see cref="BlueprintModel"/>).
/// </summary>
public partial class IsmaBlueprintViewModel : ObservableObject
{
    private readonly StateViewModel mainState;
    private readonly StateViewModel initState;

    public IsmaBlueprintViewModel()
    {
        mainState = CreateMainState();
        initState = CreateInitState();

        AddStateCommand = new RelayCommand(() => AddState());
        ToggleAddTransitionCommand = new RelayCommand(ToggleAddTransition);
        ToggleRemoveStateCommand = new RelayCommand(ToggleRemoveState);
        ToggleRemoveTransitionCommand = new RelayCommand(ToggleRemoveTransition);
    }

    /// <summary>The canvas document model.</summary>
    public CanvasViewModel CanvasViewModel { get; } = new();

    [ObservableProperty]
    private EditorMode editorMode = EditorMode.Idle;

    [ObservableProperty]
    private BlueprintEvent? evt;

    [ObservableProperty]
    private string addTransitionButtonText = "New transition";

    [ObservableProperty]
    private string removeStateButtonText = "Remove state";

    [ObservableProperty]
    private string removeTransitionButtonText = "Remove transition";

    public IRelayCommand AddStateCommand { get; } = null!;

    public IRelayCommand ToggleAddTransitionCommand { get; } = null!;

    public IRelayCommand ToggleRemoveStateCommand { get; } = null!;

    public IRelayCommand ToggleRemoveTransitionCommand { get; } = null!;

    /// <summary>Recomputes the toolbar button texts whenever the mode changes.</summary>
    partial void OnEditorModeChanged(EditorMode value)
    {
        AddTransitionButtonText = value is EditorMode.AddTransition ? "Stop adding transaction" : "New transition";
        RemoveStateButtonText = value is EditorMode.RemoveState ? "Stop remove state" : "Remove state";
        RemoveTransitionButtonText = value is EditorMode.RemoveTransition ? "Stop remove transition" : "Remove transition";
    }

    /// <summary>Resets the editor mode to idle.</summary>
    public void ResetMode()
    {
        EditorMode = EditorMode.Idle;
    }

    /// <summary>Toggles the add-transition mode (Idle ↔ AddTransition with a fresh selection).</summary>
    public void ToggleAddTransition()
    {
        EditorMode = EditorMode is EditorMode.AddTransition ? EditorMode.Idle : new EditorMode.AddTransition();
    }

    /// <summary>Toggles the remove-state mode.</summary>
    public void ToggleRemoveState()
    {
        EditorMode = EditorMode is EditorMode.RemoveState ? EditorMode.Idle : new EditorMode.RemoveState();
    }

    /// <summary>Toggles the remove-transition mode.</summary>
    public void ToggleRemoveTransition()
    {
        EditorMode = EditorMode is EditorMode.RemoveTransition ? EditorMode.Idle : new EditorMode.RemoveTransition();
    }

    /// <summary>Adds a new user state with the next default name and returns it.</summary>
    public StateViewModel AddState(double positionX = 10, double positionY = 200, string stateText = "")
    {
        ResetMode();
        string name = CanvasViewModel.CreateNextDefaultStateName();
        StateViewModel state = CanvasViewModel.CreateState(
            text: stateText,
            x: positionX,
            y: positionY,
            name: name,
            kind: StateKind.User,
            squareWidth: BlueprintEditorConstants.DefaultStateWidth,
            squareHeight: BlueprintEditorConstants.DefaultStateHeight);
        CanvasViewModel.AddState(state);
        return state;
    }

    /// <summary>Removes a state; no-op for non-user states.</summary>
    public void RemoveState(StateViewModel state)
    {
        if (state.Kind != StateKind.User)
        {
            return;
        }

        CanvasViewModel.RemoveState(state);
    }

    /// <summary>
    /// Records a state clicked while in add-transition mode. The second distinct
    /// state creates a transaction; the same state twice creates a loop.
    /// No-op unless the state is a user state and the mode is AddTransition.
    /// </summary>
    public void RecordTransitionSource(StateViewModel state)
    {
        if (state.Kind != StateKind.User)
        {
            return;
        }

        if (EditorMode is not EditorMode.AddTransition addTransitionMode)
        {
            return;
        }

        addTransitionMode.SelectedStates.Add(state);

        if (addTransitionMode.SelectedStates.Count < 2)
        {
            return;
        }

        StateViewModel state1 = addTransitionMode.SelectedStates[0];
        StateViewModel state2 = addTransitionMode.SelectedStates[1];

        if (ReferenceEquals(state1, state2))
        {
            AddLoopArrow(state1, "", "", "");
        }
        else
        {
            AddTransactionArrow(state1, state2, "", "");
        }

        EditorMode = EditorMode.Idle;
    }

    /// <summary>Adds a transaction arrow; no-op if the (start, end) pair already exists.</summary>
    public void AddTransactionArrow(StateViewModel start, StateViewModel end, string predicate, string alias)
    {
        if (CanvasViewModel.Transactions.Any(t => t.StartStateName == start.Name && t.EndStateName == end.Name))
        {
            return;
        }

        CanvasViewModel.AddTransaction(new TransactionViewModel(start.Name, end.Name, predicate, alias));
    }

    /// <summary>Adds a loop arrow; no-op if the state already has a loop.</summary>
    public void AddLoopArrow(StateViewModel state, string text, string predicate, string alias)
    {
        if (CanvasViewModel.LoopTransactions.Any(l => l.StateName == state.Name))
        {
            return;
        }

        CanvasViewModel.AddLoopTransaction(new LoopTransactionViewModel(state.Name, predicate, alias, text));
    }

    /// <summary>Removes a transaction.</summary>
    public void RemoveTransaction(TransactionViewModel tx)
    {
        CanvasViewModel.RemoveTransaction(tx);
    }

    /// <summary>Removes a loop.</summary>
    public void RemoveLoopArrow(LoopTransactionViewModel loop)
    {
        CanvasViewModel.RemoveLoopTransaction(loop);
    }

    /// <summary>
    /// Handles a single state click: add-transition mode records the source,
    /// remove-state mode removes it, idle mode starts name editing (user states only).
    /// </summary>
    public void HandleStateClick(StateViewModel state)
    {
        switch (EditorMode)
        {
            case EditorMode.AddTransition:
                RecordTransitionSource(state);
                break;
            case EditorMode.RemoveState:
                RemoveState(state);
                break;
            default:
                if (state.Kind == StateKind.User)
                {
                    state.StartEdit();
                }
                break;
        }
    }

    /// <summary>Fires the open-state-editor event.</summary>
    public void HandleStateDoubleClick(StateViewModel state)
    {
        FireEvent(new BlueprintEvent.OpenStateEditor(state));
    }

    /// <summary>Handles an arrow body click; removes the arrow in remove-transition mode.</summary>
    public void HandleArrowBodyClick(TransactionViewModel tx)
    {
        if (EditorMode is EditorMode.RemoveTransition)
        {
            RemoveTransaction(tx);
        }
    }

    /// <summary>
    /// Handles an arrowhead click: in remove-transition mode removes the arrow
    /// and returns false (no popover); otherwise returns true (show popover).
    /// </summary>
    public bool HandleArrowheadClick(TransactionViewModel tx)
    {
        if (EditorMode is EditorMode.RemoveTransition)
        {
            RemoveTransaction(tx);
            return false;
        }

        return true;
    }

    /// <summary>Handles a loop body click; removes the loop in remove-transition mode.</summary>
    public void HandleLoopArrowBodyClick(LoopTransactionViewModel loop)
    {
        if (EditorMode is EditorMode.RemoveTransition)
        {
            RemoveLoopArrow(loop);
        }
    }

    /// <summary>Fires the open-loop-editor event.</summary>
    public void HandleLoopArrowheadDoubleClick(LoopTransactionViewModel loop, StateViewModel state)
    {
        FireEvent(new BlueprintEvent.OpenLoopEditor(loop, state));
    }

    /// <summary>Commits a name edit (uniqueness-gated) and leaves edit mode.</summary>
    public void CommitNameEdit(StateViewModel state, string newName)
    {
        state.Name = newName;
        state.CommitEdit();
    }

    /// <summary>Fires an event toward the editor view.</summary>
    public void FireEvent(BlueprintEvent evt)
    {
        Evt = evt;
    }

    /// <summary>
    /// Serializes the diagram: Main/init from the private references,
    /// user states filtered from the canvas collection.
    /// </summary>
    public BlueprintModel ToBlueprintModel()
    {
        var main = new BlueprintStateModel(mainState.X, mainState.Y, mainState.Name, mainState.Text);
        var init = new BlueprintStateModel(initState.X, initState.Y, initState.Name, initState.Text);
        var states = CanvasViewModel.States
            .Where(s => s.Kind == StateKind.User)
            .Select(s => new BlueprintStateModel(s.X, s.Y, s.Name, s.Text))
            .ToImmutableArray();
        var transactions = CanvasViewModel.Transactions
            .Select(t => new BlueprintTransactionModel(t.StartStateName, t.EndStateName, t.Predicate, t.Alias))
            .ToImmutableArray();
        var loopTransactions = CanvasViewModel.LoopTransactions
            .Select(l => new BlueprintLoopTransactionModel(l.StateName, l.Predicate, l.Alias, l.Text))
            .ToImmutableArray();

        return new BlueprintModel(main, init, states, transactions, loopTransactions);
    }

    /// <summary>
    /// Restores the diagram from a model: clears the canvas, restores Main/init,
    /// rebuilds user states (skipping names Main/init), relinks transactions/loops
    /// by name, skipping dangling references.
    /// </summary>
    public void FromBlueprintModel(BlueprintModel model)
    {
        CanvasViewModel.ClearAll();

        mainState.X = model.Main.CanvasPositionX;
        mainState.Y = model.Main.CanvasPositionY;
        mainState.Name = model.Main.Name;
        mainState.Text = model.Main.Text;
        CanvasViewModel.AddState(mainState);

        initState.X = model.Init.CanvasPositionX;
        initState.Y = model.Init.CanvasPositionY;
        initState.Name = model.Init.Name;
        initState.Text = model.Init.Text;
        CanvasViewModel.AddState(initState);

        var stateMap = new Dictionary<string, StateViewModel>
        {
            [mainState.Name] = mainState,
            [initState.Name] = initState,
        };

        foreach (var blueprintState in model.States.Where(s => s.Name != StateNames.Main && s.Name != StateNames.Init))
        {
            var state = CanvasViewModel.CreateState(
                name: blueprintState.Name,
                text: blueprintState.Text,
                x: blueprintState.CanvasPositionX,
                y: blueprintState.CanvasPositionY,
                kind: StateKind.User,
                squareWidth: BlueprintEditorConstants.DefaultStateWidth,
                squareHeight: BlueprintEditorConstants.DefaultStateHeight);
            CanvasViewModel.AddState(state);
            stateMap[blueprintState.Name] = state;
        }

        foreach (var blueprintTx in model.Transactions)
        {
            if (stateMap.TryGetValue(blueprintTx.StartStateName, out var startState) &&
                stateMap.TryGetValue(blueprintTx.EndStateName, out var endState))
            {
                AddTransactionArrow(startState, endState, blueprintTx.Predicate, blueprintTx.Alias);
            }
        }

        foreach (var loopTx in model.LoopTransactions)
        {
            if (stateMap.TryGetValue(loopTx.StateName, out var state))
            {
                AddLoopArrow(state, loopTx.Text, loopTx.Predicate, loopTx.Alias);
            }
        }
    }

    private StateViewModel CreateMainState()
    {
        var state = new StateViewModel(
            name: StateNames.Main,
            text: "",
            x: BlueprintEditorConstants.StateInset,
            y: 0,
            squareWidth: BlueprintEditorConstants.DefaultStateWidth,
            squareHeight: BlueprintEditorConstants.FixedStateHeight,
            kind: StateKind.Main,
            isNameUnique: _ => true);
        CanvasViewModel.AddState(state);
        return state;
    }

    private StateViewModel CreateInitState()
    {
        var state = new StateViewModel(
            name: StateNames.Init,
            text: "",
            x: BlueprintEditorConstants.StateInset,
            y: 100,
            squareWidth: BlueprintEditorConstants.DefaultStateWidth,
            squareHeight: BlueprintEditorConstants.FixedStateHeight,
            kind: StateKind.Init,
            isNameUnique: _ => true);
        CanvasViewModel.AddState(state);
        return state;
    }
}
