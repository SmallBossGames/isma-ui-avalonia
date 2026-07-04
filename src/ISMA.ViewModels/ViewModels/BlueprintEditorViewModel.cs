using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintEditorViewModel : ObservableObject, IDisposable
{
    private BlueprintModel _model = BlueprintModel.Empty;

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

    [ObservableProperty]
    private EditorMode _mode = new EditorMode.Default();

    [ObservableProperty]
    private BlueprintStateViewModel? _selectedState;

    [ObservableProperty]
    private BlueprintTransactionViewModel? _selectedTransaction;

    public bool IsDefaultMode => Mode is EditorMode.Default;
    public bool IsAddTransitionMode => Mode is EditorMode.AddTransition;
    public bool IsRemoveStateMode => Mode is EditorMode.RemoveState;
    public bool IsRemoveTransitionMode => Mode is EditorMode.RemoveTransition;

    public string AddTransitionButtonContent => Mode switch
    {
        EditorMode.AddTransition => "Stop adding transaction",
        _ => "Add Transition"
    };

    public string RemoveStateButtonContent => Mode switch
    {
        EditorMode.RemoveState => "Stop remove state",
        _ => "Remove State"
    };

    public string RemoveTransitionButtonContent => Mode switch
    {
        EditorMode.RemoveTransition => "Stop remove transition",
        _ => "Remove Transition"
    };

    partial void OnModeChanged(EditorMode value)
    {
        OnPropertyChanged(nameof(IsDefaultMode));
        OnPropertyChanged(nameof(IsAddTransitionMode));
        OnPropertyChanged(nameof(IsRemoveStateMode));
        OnPropertyChanged(nameof(IsRemoveTransitionMode));
        OnPropertyChanged(nameof(AddTransitionButtonContent));
        OnPropertyChanged(nameof(RemoveStateButtonContent));
        OnPropertyChanged(nameof(RemoveTransitionButtonContent));
        UpdateStateEditability();
    }

    private void UpdateStateEditability()
    {
        foreach (var state in States)
        {
            if (state.IsMain || state.IsInit)
            {
                state.IsEnabled = false;
            }
            else
            {
                state.IsEnabled = Mode is not EditorMode.AddTransition and not EditorMode.RemoveState;
            }
        }
    }

    public BlueprintEditorViewModel()
    {
        SetBlueprintModel(BlueprintModel.Empty);
    }

    public BlueprintEditorViewModel(BlueprintModel model)
    {
        SetBlueprintModel(model);
    }

    public BlueprintEditorViewModel(NameChangingMonitor nameMonitor)
    {
        _nameMonitor = nameMonitor;
    }

    [RelayCommand]
    private void AddState()
    {
        if (Mode is not EditorMode.Default)
            return;

        var newStateName = _nameMonitor.CreateNextDefaultName();
        var newState = new BlueprintStateModel
        {
            CanvasPositionX = 10,
            CanvasPositionY = 200,
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
        if (Mode is not EditorMode.Default)
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

    private bool HasDuplicateTransaction(string startState, string endState, string predicate)
    {
        foreach (var tx in _model.Transactions)
        {
            if (tx.StartStateName == startState && tx.EndStateName == endState && tx.Predicate == predicate)
            {
                return true;
            }
        }
        return false;
    }

    [RelayCommand]
    private void AddTransition()
    {
        if (Mode is not EditorMode.AddTransition addMode)
            return;

        if (SelectedState == null)
            return;

        var selectedStates = addMode.SelectedStates;

        if (selectedStates.Count == 0)
        {
            SelectedState = null;
            Mode = new EditorMode.Default();
            return;
        }

        var firstState = selectedStates[0];

        if (firstState.Name == SelectedState.Name)
        {
            var existingLoop = _model.LoopTransactions.FirstOrDefault(l => l.StateName == firstState.Name);
            if (existingLoop == null)
            {
                var newLoop = new BlueprintLoopTransactionModel
                {
                    StateName = firstState.Name,
                    Predicate = "1 > 0",
                    Alias = "",
                    Text = ""
                };
                AddLoop(newLoop);
            }
        }
        else
        {
            var newTx = new BlueprintTransactionModel
            {
                StartStateName = firstState.Name,
                EndStateName = SelectedState.Name,
                Predicate = "condition",
                Alias = ""
            };

            if (!HasDuplicateTransaction(newTx.StartStateName, newTx.EndStateName, newTx.Predicate))
            {
                _model = new BlueprintModel
                {
                    Main = _model.Main,
                    Init = _model.Init,
                    States = _model.States,
                    Transactions = _model.Transactions.Add(newTx),
                    LoopTransactions = _model.LoopTransactions
                };
            }
        }

        SelectedState = null;
        Mode = new EditorMode.Default();
        ReloadViews();
    }

    [RelayCommand]
    private void RemoveState()
    {
        if (Mode is not EditorMode.RemoveState)
            return;

        if (SelectedState == null)
            return;

        var stateName = SelectedState.Name;
        var newStateList = _model.States.RemoveAll(s => s.Name == stateName);

        var newTxList = _model.Transactions.RemoveAll(tx =>
            tx.StartStateName == stateName || tx.EndStateName == stateName);

        var newLoopList = _model.LoopTransactions.RemoveAll(loop => loop.StateName == stateName);

        _nameMonitor.TryUnregister(stateName);

        _model = new BlueprintModel
        {
            Main = _model.Main,
            Init = _model.Init,
            States = newStateList,
            Transactions = newTxList,
            LoopTransactions = newLoopList
        };

        Mode = new EditorMode.Default();
        SelectedState = null;
        ReloadViews();
    }

    [RelayCommand]
    private void RemoveTransition()
    {
        if (Mode is not EditorMode.RemoveTransition)
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

        Mode = new EditorMode.Default();
        SelectedTransaction = null;
        ReloadViews();
    }

    public void SetTransitionSource(BlueprintStateViewModel state)
    {
        if (Mode is EditorMode.AddTransition addMode)
        {
            addMode.SelectedStates.Add(state);
        }
    }

    public BlueprintStateViewModel? GetTransitionSource()
    {
        if (Mode is EditorMode.AddTransition addMode && addMode.SelectedStates.Count > 0)
        {
            return addMode.SelectedStates[0];
        }
        return null;
    }

    public void AddLoop(BlueprintLoopTransactionModel loopModel)
    {
        foreach (var loop in _model.LoopTransactions)
        {
            if (loop.StateName == loopModel.StateName)
            {
                return;
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
        if (Mode is EditorMode.RemoveState && SelectedState != null)
        {
            var newStateList = _model.States.RemoveAll(s => s.Name == SelectedState.Name);
            var newTxList = _model.Transactions.RemoveAll(tx =>
                tx.StartStateName == SelectedState.Name || tx.EndStateName == SelectedState.Name);
            var newLoopList = _model.LoopTransactions.RemoveAll(loop => loop.StateName == SelectedState.Name);
            _model = new BlueprintModel
            {
                Main = _model.Main,
                Init = _model.Init,
                States = newStateList,
                Transactions = newTxList,
                LoopTransactions = newLoopList
            };
            SelectedState = null;
            ReloadViews();
        }
        else if (Mode is EditorMode.RemoveTransition && SelectedTransaction != null)
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
        Mode = new EditorMode.Default();
    }

    [RelayCommand]
    private void SetAddTransitionMode()
    {
        if (Mode is EditorMode.AddTransition)
            Mode = new EditorMode.Default();
        else
            Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
    }

    [RelayCommand]
    private void SetRemoveStateMode()
    {
        if (Mode is EditorMode.RemoveState)
            Mode = new EditorMode.Default();
        else
            Mode = new EditorMode.RemoveState();
    }

    [RelayCommand]
    private void SetRemoveTransitionMode()
    {
        if (Mode is EditorMode.RemoveTransition)
            Mode = new EditorMode.Default();
        else
            Mode = new EditorMode.RemoveTransition();
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

        UpdateStateEditability();
    }

    private BlueprintStateViewModel MapState(BlueprintStateModel model, bool isMain, bool isInit)
    {
        var fillColorHex = "#F08080";
        var stateHeight = 65.0;
        var canvasX = model.CanvasPositionX;
        var canvasY = model.CanvasPositionY;

        if (isMain)
        {
            fillColorHex = "#90EE90";
            stateHeight = 60.0;
        }
        else if (isInit)
        {
            fillColorHex = "#ADD8E6";
            stateHeight = 60.0;
        }

        return new BlueprintStateViewModel
        {
            CanvasPositionX = canvasX,
            CanvasPositionY = canvasY,
            Name = model.Name,
            Text = model.Text,
            IsEditable = true,
            IsMain = isMain,
            IsInit = isInit,
            FillColorHex = fillColorHex,
            StateHeight = stateHeight,
            IsEnabled = !(isMain || isInit)
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
