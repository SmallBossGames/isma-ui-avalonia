using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintEditorViewModel : ObservableObject
{
    private BlueprintModel _model = BlueprintModel.Empty;
    private BlueprintEditorMode _currentMode = BlueprintEditorMode.Default;

    private ObservableCollection<BlueprintStateViewModel> _states = new();
    private ObservableCollection<BlueprintTransactionViewModel> _transactions = new();
    private ObservableCollection<BlueprintLoopTransactionViewModel> _loopTransactions = new();

    public ObservableCollection<BlueprintStateViewModel> States
    {
        get => _states;
        set => SetProperty(ref _states, value);
    }

    public ObservableCollection<BlueprintTransactionViewModel> Transactions
    {
        get => _transactions;
        set => SetProperty(ref _transactions, value);
    }

    public ObservableCollection<BlueprintLoopTransactionViewModel> LoopTransactions
    {
        get => _loopTransactions;
        set => SetProperty(ref _loopTransactions, value);
    }

    public BlueprintEditorMode CurrentMode
    {
        get => _currentMode;
        set => SetProperty(ref _currentMode, value);
    }

    [ObservableProperty]
    private BlueprintStateViewModel? _selectedState;

    [ObservableProperty]
    private BlueprintTransactionViewModel? _selectedTransaction;

    public BlueprintEditorViewModel()
    {
    }

    public BlueprintEditorViewModel(BlueprintModel model)
    {
        SetBlueprintModel(model);
    }

    [RelayCommand]
    private void AddState()
    {
        if (CurrentMode != BlueprintEditorMode.Default)
            return;

        var statesList = _model.States.AddRange(_model.States).ToImmutableList().ToImmutableArray();
        var newState = new BlueprintStateModel
        {
            CanvasPositionX = 100 + (_model.States.Length * 30),
            CanvasPositionY = 100 + (_model.States.Length * 30),
            Name = $"state{_model.States.Length}",
            Text = ""
        };

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = _model.States.Add(newState),
            Transactions = _model.Transactions,
            LoopTransactions = _model.LoopTransactions
        };

        ReloadViews();
    }

    [RelayCommand]
    private void AddTransition()
    {
        if (CurrentMode != BlueprintEditorMode.AddTransition)
            return;

        if (SelectedState == null)
            return;

        var newTx = new BlueprintTransactionModel
        {
            StartStateName = SelectedState.Name,
            EndStateName = SelectedState.Name,
            Predicate = "condition",
            Alias = ""
        };

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = _model.States,
            Transactions = _model.Transactions.Add(newTx),
            LoopTransactions = _model.LoopTransactions
        };

        CurrentMode = BlueprintEditorMode.Default;
        ReloadViews();
    }

    [RelayCommand]
    private void RemoveState()
    {
        if (CurrentMode != BlueprintEditorMode.RemoveState)
            return;

        if (SelectedState == null)
            return;

        var newStateList = _model.States.RemoveAll(s => s.Name == SelectedState.Name);

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = newStateList,
            Transactions = _model.Transactions,
            LoopTransactions = _model.LoopTransactions
        };

        CurrentMode = BlueprintEditorMode.Default;
        SelectedState = null;
        ReloadViews();
    }

    [RelayCommand]
    private void RemoveTransition()
    {
        if (CurrentMode != BlueprintEditorMode.RemoveTransition)
            return;

        if (SelectedTransaction == null)
            return;

        var newTxList = _model.Transactions.RemoveAll(tx =>
            tx.StartStateName == SelectedTransaction.StartState.Name &&
            tx.EndStateName == SelectedTransaction.EndState.Name &&
            tx.Predicate == SelectedTransaction.Predicate);

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = _model.States,
            Transactions = newTxList,
            LoopTransactions = _model.LoopTransactions
        };

        CurrentMode = BlueprintEditorMode.Default;
        SelectedTransaction = null;
        ReloadViews();
    }

    [RelayCommand]
    private void RemoveLoop()
    {
        if (SelectedState == null)
            return;

        var newLoopList = _model.LoopTransactions.RemoveAll(loop => loop.StateName == SelectedState.Name);

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = _model.States,
            Transactions = _model.Transactions,
            LoopTransactions = newLoopList
        };

        SelectedState = null;
        ReloadViews();
    }

    public BlueprintModel GetBlueprintModel() => _model;

    public void SetBlueprintModel(BlueprintModel model)
    {
        _model = model;
        ReloadViews();
    }

    [RelayCommand]
    private void ResetEditorMode()
    {
        CurrentMode = BlueprintEditorMode.Default;
    }

    private void ReloadViews()
    {
        States.Clear();
        Transactions.Clear();
        LoopTransactions.Clear();

        if (_model.Main != null)
            States.Add(MapState(_model.Main, true, false));

        if (_model.Init != null)
            States.Add(MapState(_model.Init, false, true));

        foreach (var state in _model.States)
            States.Add(MapState(state, false, false));

        foreach (var tx in _model.Transactions)
        {
            var startState = States.FirstOrDefault(s => s.Name == tx.StartStateName);
            var endState = States.FirstOrDefault(s => s.Name == tx.EndStateName);
            if (startState != null && endState != null)
            {
                Transactions.Add(new BlueprintTransactionViewModel
                {
                    StartState = startState,
                    EndState = endState,
                    Predicate = tx.Predicate,
                    Alias = tx.Alias
                });
            }
        }

        foreach (var loop in _model.LoopTransactions)
        {
            var state = States.FirstOrDefault(s => s.Name == loop.StateName);
            if (state != null)
            {
                LoopTransactions.Add(new BlueprintLoopTransactionViewModel
                {
                    State = state,
                    Predicate = loop.Predicate,
                    Alias = loop.Alias,
                    Text = loop.Text
                });
            }
        }
    }

    private BlueprintStateViewModel MapState(BlueprintStateModel model, bool isMain, bool isInit)
    {
        var fillColorHex = "#F08080";
        if (isMain)
            fillColorHex = "#90EE90";
        else if (isInit)
            fillColorHex = "#ADD8E6";

        return new BlueprintStateViewModel
        {
            CanvasPositionX = model.CanvasPositionX,
            CanvasPositionY = model.CanvasPositionY,
            Name = model.Name,
            Text = model.Text,
            IsEditable = true,
            IsMain = isMain,
            IsInit = isInit,
            FillColorHex = fillColorHex
        };
    }

    public void Dispose()
    {
        States.Clear();
        Transactions.Clear();
        LoopTransactions.Clear();
    }
}
