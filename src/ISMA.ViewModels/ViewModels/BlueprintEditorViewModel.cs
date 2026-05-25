using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintEditorViewModel : ObservableObject, IDisposable
{
    private BlueprintModel _model = BlueprintModel.Empty;
    private BlueprintEditorMode _currentMode = BlueprintEditorMode.Default;

    private ObservableCollection<BlueprintStateViewModel> _states = new();
    private ObservableCollection<BlueprintTransactionViewModel> _transactions = new();
    private ObservableCollection<BlueprintLoopTransactionViewModel> _loopTransactions = new();
    private readonly NameChangingMonitor _nameMonitor = new();

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

    [ObservableProperty]
    private bool _isAddTransitionMode;

    [ObservableProperty]
    private bool _isRemoveStateMode;

    [ObservableProperty]
    private bool _isRemoveTransitionMode;

    public BlueprintEditorViewModel()
    {
        IsAddTransitionMode = false;
        IsRemoveStateMode = false;
        IsRemoveTransitionMode = false;
    }

    public BlueprintEditorViewModel(BlueprintModel model)
    {
        SetBlueprintModel(model);
    }

    public BlueprintEditorViewModel(NameChangingMonitor nameMonitor)
    {
        _nameMonitor = nameMonitor;
        IsAddTransitionMode = false;
        IsRemoveStateMode = false;
        IsRemoveTransitionMode = false;
    }

    [RelayCommand]
    private void AddState()
    {
        if (CurrentMode != BlueprintEditorMode.Default)
            return;

        var newStateName = _nameMonitor.CreateNextDefaultName();
        var newState = new BlueprintStateModel
        {
            CanvasPositionX = 100 + (States.Count * 30),
            CanvasPositionY = 100 + (States.Count * 30),
            Name = newStateName,
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

    public void AddStateWithName(string name, double x, double y)
    {
        if (CurrentMode != BlueprintEditorMode.Default)
            return;

        if (!_nameMonitor.TryRegister(name))
            return;

        var newState = new BlueprintStateModel
        {
            CanvasPositionX = x,
            CanvasPositionY = y,
            Name = name,
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

    public void UpdateStateName(BlueprintStateViewModel state, string newName)
    {
        if (state.IsMain || state.IsInit)
            return;

        var oldName = state.Name;
        if (oldName == newName)
            return;

        if (!_nameMonitor.TryRegister(newName))
            return;

        _nameMonitor.TryUnregister(oldName);

        var statesArray = _model.States.ToArray();
        for (int i = 0; i < statesArray.Length; i++)
        {
            if (statesArray[i].Name == oldName)
            {
                statesArray[i] = new BlueprintStateModel
                {
                    CanvasPositionX = statesArray[i].CanvasPositionX,
                    CanvasPositionY = statesArray[i].CanvasPositionY,
                    Name = newName,
                    Text = statesArray[i].Text
                };
                break;
            }
        }

        var transactionsArray = _model.Transactions.ToArray();
        for (int i = 0; i < transactionsArray.Length; i++)
        {
            if (transactionsArray[i].StartStateName == oldName)
            {
                transactionsArray[i] = new BlueprintTransactionModel
                {
                    StartStateName = newName,
                    EndStateName = transactionsArray[i].EndStateName,
                    Predicate = transactionsArray[i].Predicate,
                    Alias = transactionsArray[i].Alias
                };
            }
            if (transactionsArray[i].EndStateName == oldName)
            {
                transactionsArray[i] = new BlueprintTransactionModel
                {
                    StartStateName = transactionsArray[i].StartStateName,
                    EndStateName = newName,
                    Predicate = transactionsArray[i].Predicate,
                    Alias = transactionsArray[i].Alias
                };
            }
        }

        var loopTransactionsArray = _model.LoopTransactions.ToArray();
        for (int i = 0; i < loopTransactionsArray.Length; i++)
        {
            if (loopTransactionsArray[i].StateName == oldName)
            {
                loopTransactionsArray[i] = new BlueprintLoopTransactionModel
                {
                    StateName = newName,
                    Predicate = loopTransactionsArray[i].Predicate,
                    Alias = loopTransactionsArray[i].Alias,
                    Text = loopTransactionsArray[i].Text
                };
            }
        }

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = statesArray.ToImmutableArray(),
            Transactions = transactionsArray.ToImmutableArray(),
            LoopTransactions = loopTransactionsArray.ToImmutableArray()
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
        IsAddTransitionMode = false;
        ReloadViews();
    }

    [RelayCommand]
    private void RemoveState()
    {
        if (CurrentMode != BlueprintEditorMode.RemoveState)
            return;

        if (SelectedState == null)
            return;

        var stateName = SelectedState.Name;
        var newStateList = _model.States.RemoveAll(s => s.Name == stateName);

        _nameMonitor.TryUnregister(stateName);

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = newStateList,
            Transactions = _model.Transactions,
            LoopTransactions = _model.LoopTransactions
        };

        CurrentMode = BlueprintEditorMode.Default;
        IsRemoveStateMode = false;
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
        IsRemoveTransitionMode = false;
        SelectedTransaction = null;
        ReloadViews();
    }

    public void AddLoop(BlueprintLoopTransactionModel loopModel)
    {
        // Check for existing loop on this state
        foreach (var loop in _model.LoopTransactions)
        {
            if (loop.StateName == loopModel.StateName)
            {
                return; // Loop already exists
            }
        }

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = _model.States,
            Transactions = _model.Transactions,
            LoopTransactions = _model.LoopTransactions.Add(loopModel)
        };

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

    [RelayCommand]
    private void RemoveSelected()
    {
        if (CurrentMode == BlueprintEditorMode.RemoveState && SelectedState != null)
        {
            var newStateList = _model.States.RemoveAll(s => s.Name == SelectedState.Name);
            _model = new BlueprintModel
            {
                Main = _model.Main,
                Init = _model.Init,
                States = newStateList,
                Transactions = _model.Transactions,
                LoopTransactions = _model.LoopTransactions
            };
            SelectedState = null;
            ReloadViews();
        }
        else if (CurrentMode == BlueprintEditorMode.RemoveTransition && SelectedTransaction != null)
        {
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
            SelectedTransaction = null;
            ReloadViews();
        }
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
        IsAddTransitionMode = false;
        IsRemoveStateMode = false;
        IsRemoveTransitionMode = false;
    }

    partial void OnIsAddTransitionModeChanged(bool value)
    {
        if (value)
        {
            CurrentMode = BlueprintEditorMode.AddTransition;
            IsRemoveStateMode = false;
            IsRemoveTransitionMode = false;
        }
        else if (CurrentMode == BlueprintEditorMode.AddTransition)
        {
            CurrentMode = BlueprintEditorMode.Default;
        }
    }

    partial void OnIsRemoveStateModeChanged(bool value)
    {
        if (value)
        {
            CurrentMode = BlueprintEditorMode.RemoveState;
            IsAddTransitionMode = false;
            IsRemoveTransitionMode = false;
        }
        else if (CurrentMode == BlueprintEditorMode.RemoveState)
        {
            CurrentMode = BlueprintEditorMode.Default;
        }
    }

    partial void OnIsRemoveTransitionModeChanged(bool value)
    {
        if (value)
        {
            CurrentMode = BlueprintEditorMode.RemoveTransition;
            IsAddTransitionMode = false;
            IsRemoveStateMode = false;
        }
        else if (CurrentMode == BlueprintEditorMode.RemoveTransition)
        {
            CurrentMode = BlueprintEditorMode.Default;
        }
    }

    private void ReloadViews()
    {
        States.Clear();
        Transactions.Clear();
        LoopTransactions.Clear();

        _nameMonitor.Clear();

        if (_model.Main != null)
        {
            _nameMonitor.TryRegister(_model.Main.Name);
            States.Add(MapState(_model.Main, true, false));
        }

        if (_model.Init != null)
        {
            _nameMonitor.TryRegister(_model.Init.Name);
            States.Add(MapState(_model.Init, false, true));
        }

        foreach (var state in _model.States)
        {
            _nameMonitor.TryRegister(state.Name);
            States.Add(MapState(state, false, false));
        }

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
        _nameMonitor.Clear();
    }
}
