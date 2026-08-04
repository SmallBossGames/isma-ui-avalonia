using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.Models;
using ISMA.BlueprintEditor.Services;

namespace ISMA.BlueprintEditor.ViewModels;

public partial class IsmaBlueprintViewModel : ObservableObject
{
    private StateViewModel _mainState = null!;
    private StateViewModel _initState = null!;
    private readonly CanvasViewModel _canvasViewModel;
    private readonly ITextEditorFactory _textEditorFactory;

    public Action<StateViewModel>? OnStateDoubleClick { get; set; }

    public IsmaBlueprintViewModel(ITextEditorFactory? textEditorFactory = null)
    {
        _textEditorFactory = textEditorFactory ?? new DefaultTextEditorFactory();
        _canvasViewModel = new CanvasViewModel();

        _mainState = _canvasViewModel.CreateState(
            StateNames.Main,
            "LightGreen",
            200, 100,
            BlueprintEditorConstants.DefaultStateWidth,
            BlueprintEditorConstants.FixedStateHeight,
            editable: true);

        _initState = _canvasViewModel.CreateState(
            StateNames.Init,
            "LightBlue",
            200, 300,
            BlueprintEditorConstants.DefaultStateWidth,
            BlueprintEditorConstants.FixedStateHeight,
            editable: false);
    }

    public CanvasViewModel CanvasViewModel => _canvasViewModel;

    public StateViewModel MainState => _mainState;

    public StateViewModel InitState => _initState;

    [ObservableProperty]
    private EditorMode _editorMode = new IdleMode();

    [ObservableProperty]
    private string _addTransitionButtonText = "New transition";

    [ObservableProperty]
    private string _removeStateButtonText = "Remove state";

    [ObservableProperty]
    private string _removeTransitionButtonText = "Remove transition";

    public void UpdateButtonTexts()
    {
        switch (EditorMode)
        {
            case IdleMode:
                AddTransitionButtonText = "New transition";
                RemoveStateButtonText = "Remove state";
                RemoveTransitionButtonText = "Remove transition";
                break;
            case AddTransitionMode:
                AddTransitionButtonText = "Stop adding transaction";
                break;
            case RemoveStateMode:
                RemoveStateButtonText = "Stop remove state";
                break;
            case RemoveTransitionMode:
                RemoveTransitionButtonText = "Stop remove transition";
                break;
        }
    }

    public void ResetMode()
    {
        EditorMode = new IdleMode();
    }

    public void ToggleAddTransition()
    {
        EditorMode = EditorMode is IdleMode ? new AddTransitionMode() : new IdleMode();
    }

    public void ToggleRemoveState()
    {
        EditorMode = EditorMode is RemoveStateMode ? new IdleMode() : new RemoveStateMode();
    }

    public void ToggleRemoveTransition()
    {
        EditorMode = EditorMode is RemoveTransitionMode ? new IdleMode() : new RemoveTransitionMode();
    }

    public void AddState()
    {
        var defaultName = _canvasViewModel.CreateNextDefaultStateName();
        _canvasViewModel.CreateState(
            defaultName,
            "Coral",
            150 + _canvasViewModel.States.Count * 30,
            150 + _canvasViewModel.States.Count * 30,
            BlueprintEditorConstants.DefaultStateWidth,
            BlueprintEditorConstants.DefaultStateHeight,
            editable: true);
    }

    public void RemoveState(StateViewModel state)
    {
        if (state.Name == StateNames.Main || state.Name == StateNames.Init)
        {
            return;
        }

        _canvasViewModel.RemoveState(state);
    }

    public void RecordTransitionSource(StateViewModel state)
    {
        if (EditorMode is AddTransitionMode addTransitionMode)
        {
            if (addTransitionMode.SelectedStates.Count == 1)
            {
                var startState = addTransitionMode.SelectedStates[0];
                addTransitionMode.SelectedStates.Clear();

                if (ReferenceEquals(startState, state))
                {
                    AddLoopArrow(state, "", "", state.Text);
                }
                else
                {
                    AddTransactionArrow(startState, state, "", "");
                }

                EditorMode = new IdleMode();
            }
            else
            {
                addTransitionMode.SelectedStates.Add(state);
            }
        }
    }

    public void AddTransactionArrow(StateViewModel start, StateViewModel end, string predicate, string alias)
    {
        var tx = new TransactionViewModel(start.Name, end.Name, predicate, alias);
        _canvasViewModel.AddTransaction(tx);
    }

    public void AddLoopArrow(StateViewModel state, string predicate, string alias, string text)
    {
        var loop = new LoopTransactionViewModel(state.Name, predicate, alias, text);
        _canvasViewModel.AddLoopTransaction(loop);
    }

    public void RemoveTransaction(TransactionViewModel transaction)
    {
        _canvasViewModel.RemoveTransaction(transaction);
    }

    public void RemoveLoopArrow(LoopTransactionViewModel loopTransaction)
    {
        _canvasViewModel.RemoveLoopTransaction(loopTransaction);
    }

    public BlueprintModel ToBlueprintModel()
    {
        var bpStates = new List<BlueprintStateModel>();

        bpStates.Add(new BlueprintStateModel(
            MainState.X, MainState.Y, MainState.Name, MainState.Text));
        bpStates.Add(new BlueprintStateModel(
            InitState.X, InitState.Y, InitState.Name, InitState.Text));

        foreach (var state in _canvasViewModel.States)
        {
            if (state.Name != StateNames.Main && state.Name != StateNames.Init)
            {
                bpStates.Add(new BlueprintStateModel(
                    state.X, state.Y, state.Name, state.Text));
            }
        }

        var bpTransactions = _canvasViewModel.Transactions.Select(tx =>
            new BlueprintTransactionModel(tx.StartStateName, tx.EndStateName, tx.Predicate, tx.Alias)).ToList();

        var bpLoopTransactions = _canvasViewModel.LoopTransactions.Select(loop =>
            new BlueprintLoopTransactionModel(loop.StateName, loop.Predicate, loop.Alias, loop.Text)).ToList();

        return new BlueprintModel(
            bpStates[0], bpStates[1],
            bpStates.Skip(2),
            bpTransactions,
            bpLoopTransactions);
    }

    public void FromBlueprintModel(BlueprintModel model)
    {
        _canvasViewModel.ClearAll();

        var mainState = _canvasViewModel.CreateState(
            model.Main.Name,
            "LightGreen",
            model.Main.CanvasPositionX,
            model.Main.CanvasPositionY,
            BlueprintEditorConstants.DefaultStateWidth,
            BlueprintEditorConstants.FixedStateHeight,
            editable: true);
        mainState.Text = model.Main.Text;
        _mainState = mainState;

        var initState = _canvasViewModel.CreateState(
            model.Init.Name,
            "LightBlue",
            model.Init.CanvasPositionX,
            model.Init.CanvasPositionY,
            BlueprintEditorConstants.DefaultStateWidth,
            BlueprintEditorConstants.FixedStateHeight,
            editable: false);
        initState.Text = model.Init.Text;
        _initState = initState;

        var stateMap = new Dictionary<string, StateViewModel>();
        stateMap[MainState.Name] = MainState;
        stateMap[InitState.Name] = InitState;

        foreach (var stateModel in model.States)
        {
            var state = _canvasViewModel.CreateState(
                stateModel.Name,
                "Coral",
                stateModel.CanvasPositionX,
                stateModel.CanvasPositionY,
                BlueprintEditorConstants.DefaultStateWidth,
                BlueprintEditorConstants.DefaultStateHeight,
                editable: true);
            state.Text = stateModel.Text;
            stateMap[stateModel.Name] = state;
        }

        foreach (var txModel in model.Transactions)
        {
            if (stateMap.TryGetValue(txModel.StartStateName, out var startState) &&
                stateMap.TryGetValue(txModel.EndStateName, out var endState))
            {
                AddTransactionArrow(startState, endState, txModel.Predicate, txModel.Alias);
            }
        }

        foreach (var loopModel in model.LoopTransactions)
        {
            if (stateMap.TryGetValue(loopModel.StateName, out var state))
            {
                AddLoopArrow(state, loopModel.Predicate, loopModel.Alias, loopModel.Text);
            }
        }
    }

    public string ComputeArrowDisplayText(string predicate, string alias)
    {
        return string.IsNullOrWhiteSpace(alias) ? predicate : alias;
    }

    public void OpenStateTextEditor(StateViewModel state)
    {
        OnStateDoubleClick?.Invoke(state);
    }

    public void OpenLoopTextEditor(LoopTransactionViewModel loop)
    {
        var state = _canvasViewModel.States.FirstOrDefault(s => s.Name == loop.StateName);
        if (state != null)
        {
            OnStateDoubleClick?.Invoke(state);
        }
    }

    private sealed class DefaultTextEditorFactory : ITextEditorFactory
    {
        public Control CreateTextEditor(string text, Action<string> onTextChanged)
        {
            return new TextBlock { Text = text };
        }

        public void DisposeInstance(Control node)
        {
        }
    }
}
