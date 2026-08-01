using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintEditorViewModel : ObservableObject, IDisposable
{
    private readonly NameChangingMonitor _nameMonitor;
    private readonly IBlueprintValidationService? _validationService;
    private readonly IUndoRedoService? _undoRedoService;
    private readonly IBlueprintClipboardService? _clipboardService;

    public ObservableCollection<BlueprintStateViewModel> States { get; } = new();

    public ObservableCollection<BlueprintTransitionViewModel> Transitions { get; } = new();

    public ObservableCollection<BlueprintLoopTransactionViewModel> LoopTransactions { get; } = new();

    public BlueprintStateViewModel MainState { get; set; } = new();

    public BlueprintStateViewModel InitState { get; set; } = new();

    public ObservableCollection<BlueprintStateViewModel> SelectedStates { get; } = new();

    [ObservableProperty]
    private EditorMode _mode = new EditorMode.Default();

    [ObservableProperty]
    private BlueprintStateViewModel? _selectedState;

    [ObservableProperty]
    private BlueprintTransitionViewModel? _selectedTransition;

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
        EditorMode.RemoveState => "Stop removing state",
        _ => "Remove State"
    };

    public string RemoveTransitionButtonContent => Mode switch
    {
        EditorMode.RemoveTransition => "Stop removing transition",
        _ => "Remove Transition"
    };

    public event Action<BlueprintStateViewModel>? StateTextEditorRequested;
    public event Action<BlueprintLoopTransactionViewModel>? LoopTextEditorRequested;
    public event Action<BlueprintTransitionViewModel, double, double>? EditArrowRequested;
    public event Action? SaveProjectRequested;

    [ObservableProperty]
    private EditArrowPopOverViewModel? _popOverViewModel;

    [ObservableProperty]
    private bool _isPopOverOpen;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    /// <summary>
    /// Resolves a state's center position by its Guid. Used by <c>LoopArrow</c> for rendering.
    /// </summary>
    public Func<Guid, Point?>? PositionResolver => GetStatePosition;

    private Point? GetStatePosition(Guid stateId)
    {
        if (MainState.Id == stateId)
            return new Point(MainState.CanvasPositionX + 55, MainState.CanvasPositionY + MainState.StateHeight / 2);

        if (InitState.Id == stateId)
            return new Point(InitState.CanvasPositionX + 55, InitState.CanvasPositionY + InitState.StateHeight / 2);

        return States.FirstOrDefault(s => s.Id == stateId) is { } state
            ? new Point(state.CanvasPositionX + 55, state.CanvasPositionY + state.StateHeight / 2)
            : null;
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoRedoService?.CanUndo == true)
        {
            _undoRedoService.Undo();
            CanUndo = _undoRedoService.CanUndo;
            CanRedo = _undoRedoService.CanRedo;
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_undoRedoService?.CanRedo == true)
        {
            _undoRedoService.Redo();
            CanUndo = _undoRedoService.CanUndo;
            CanRedo = _undoRedoService.CanRedo;
        }
    }

    [RelayCommand]
    private void SaveProject()
    {
        SaveProjectRequested?.Invoke();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedStates.Count > 0)
        {
            var statesToDelete = SelectedStates.Where(s => !s.IsMain && !s.IsInit).ToList();
            foreach (var state in statesToDelete)
            {
                DeleteState(state, withUndo: true);
            }
            SelectedStates.Clear();
            SelectedState = null;
            UpdateStateEditability();
            UpdateCanvasSize();
        }
        else if (SelectedState != null && !SelectedState.IsMain && !SelectedState.IsInit)
        {
            DeleteState(SelectedState, withUndo: true);
            SelectedState = null;
            UpdateStateEditability();
            UpdateCanvasSize();
        }
        else if (SelectedTransition != null)
        {
            var tx = SelectedTransition;
            Transitions.Remove(tx);
            SelectedTransition = null;
            PushUndo($"Delete transition",
                () => Transitions.Add(tx),
                () => Transitions.Remove(tx));
        }
    }

    private void DeleteState(BlueprintStateViewModel state, bool withUndo)
    {
        var stateId = state.Id;
        var stateName = state.Name;

        var transitionsToRemove = Transitions.Where(tx => tx.StartStateId == stateId || tx.EndStateId == stateId).ToList();
        var loopsToRemove = LoopTransactions.Where(l => l.StateId == stateId).ToList();

        States.Remove(state);
        foreach (var tx in transitionsToRemove)
        {
            Transitions.Remove(tx);
        }
        foreach (var loop in loopsToRemove)
        {
            LoopTransactions.Remove(loop);
        }

        _nameMonitor.TryUnregister(stateName);

        if (withUndo && _undoRedoService != null)
        {
            var stateCopy = new BlueprintStateViewModel
            {
                Id = stateId,
                Name = stateName,
                CanvasPositionX = state.CanvasPositionX,
                CanvasPositionY = state.CanvasPositionY
            };
            PushUndo($"Delete state '{stateName}'",
                () =>
                {
                    _nameMonitor.TryRegister(stateName);
                    States.Add(stateCopy);
                    foreach (var tx in transitionsToRemove)
                    {
                        Transitions.Add(tx);
                    }
                    foreach (var loop in loopsToRemove)
                    {
                        LoopTransactions.Add(loop);
                    }
                    UpdateStateEditability();
                    UpdateCanvasSize();
                },
                () =>
                {
                    DeleteState(stateCopy, withUndo: false);
                });
        }
    }

    [RelayCommand]
    private void CopySelected()
    {
        if (SelectedStates.Count > 0)
        {
            _clipboardService?.CopyStates(SelectedStates);
        }
        else if (SelectedState != null && !SelectedState.IsMain && !SelectedState.IsInit)
        {
            _clipboardService?.CopyStates(new[] { SelectedState });
        }
    }

    private double _pasteOffsetX;
    private double _pasteOffsetY;

    public void SetPasteOffset(double offsetX, double offsetY)
    {
        _pasteOffsetX = offsetX;
        _pasteOffsetY = offsetY;
    }

    [RelayCommand]
    private void PasteStates()
    {
        if (_clipboardService == null) return;

        var newStates = _clipboardService.PasteStates(this, _pasteOffsetX, _pasteOffsetY).ToList();
        foreach (var newState in newStates)
        {
            if (!_nameMonitor.TryRegister(newState.Name))
                continue;

            States.Add(newState);
        }

        UpdateStateEditability();
        UpdateCanvasSize();
    }

    private BlueprintTransitionViewModel? _currentEditingTransition;

    private BlueprintLoopTransactionViewModel? _currentEditingLoop;

    public void OpenPopOver(BlueprintTransitionViewModel tx, double x, double y)
    {
        _currentEditingTransition = tx;
        _currentEditingLoop = null;
        PopOverViewModel = new EditArrowPopOverViewModel
        {
            Alias = tx.Alias ?? "",
            Predicate = tx.Predicate ?? ""
        };
        IsPopOverOpen = true;
        _popOverPositionX = x;
        _popOverPositionY = y;
    }

    public void OpenPopOverForLoop(BlueprintLoopTransactionViewModel loop)
    {
        _currentEditingLoop = loop;
        _currentEditingTransition = null;
        PopOverViewModel = new EditArrowPopOverViewModel
        {
            Alias = loop.Alias ?? "",
            Predicate = loop.Predicate ?? ""
        };
        IsPopOverOpen = true;
        _popOverPositionX = 100;
        _popOverPositionY = 100;
    }

    private double _popOverPositionX;
    private double _popOverPositionY;

    public double PopOverPositionX => _popOverPositionX;
    public double PopOverPositionY => _popOverPositionY;

    public void ClosePopOver()
    {
        if (_currentEditingTransition != null && PopOverViewModel != null)
        {
            _currentEditingTransition.Alias = PopOverViewModel.Alias ?? "";
            _currentEditingTransition.Predicate = PopOverViewModel.Predicate ?? "";
        }
        else if (_currentEditingLoop != null && PopOverViewModel != null)
        {
            _currentEditingLoop.Alias = PopOverViewModel.Alias ?? "";
            _currentEditingLoop.Predicate = PopOverViewModel.Predicate ?? "";
        }
        IsPopOverOpen = false;
        _currentEditingTransition = null;
        _currentEditingLoop = null;
    }

    public void OnPopOverAliasChanged()
    {
        if (PopOverViewModel == null) return;

        if (_currentEditingTransition != null)
        {
            _currentEditingTransition.Alias = PopOverViewModel.Alias ?? "";
        }
        else if (_currentEditingLoop != null)
        {
            _currentEditingLoop.Alias = PopOverViewModel.Alias ?? "";
        }
    }

    public void OnPopOverPredicateChanged()
    {
        if (PopOverViewModel == null) return;

        if (_currentEditingTransition != null)
        {
            _currentEditingTransition.Predicate = PopOverViewModel.Predicate ?? "";
        }
        else if (_currentEditingLoop != null)
        {
            _currentEditingLoop.Predicate = PopOverViewModel.Predicate ?? "";
        }
    }

    private BlueprintStateViewModel? _draggingState;

    [ObservableProperty]
    private Size _canvasSize = new(1200, 800);

    private void UpdateCanvasSize()
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var state in States)
        {
            minX = Math.Min(minX, state.CanvasPositionX);
            minY = Math.Min(minY, state.CanvasPositionY);
            maxX = Math.Max(maxX, state.CanvasPositionX + 110);
            maxY = Math.Max(maxY, state.CanvasPositionY + state.StateHeight);
        }
        if (minX == double.MaxValue)
        {
            CanvasSize = new Size(1200, 800);
        }
        else
        {
            CanvasSize = new Size(Math.Max(400, (maxX - minX) + 200), Math.Max(300, (maxY - minY) + 200));
        }
    }

    partial void OnModeChanged(EditorMode value)
    {
        if (value is not EditorMode.AddTransition)
        {
            SelectedStates.Clear();
        }
        NotifyModeProperties();
        UpdateStateEditability();
    }

    private void NotifyModeProperties()
    {
        OnPropertyChanged(nameof(IsDefaultMode));
        OnPropertyChanged(nameof(IsAddTransitionMode));
        OnPropertyChanged(nameof(IsRemoveStateMode));
        OnPropertyChanged(nameof(IsRemoveTransitionMode));
        OnPropertyChanged(nameof(AddTransitionButtonContent));
        OnPropertyChanged(nameof(RemoveStateButtonContent));
        OnPropertyChanged(nameof(RemoveTransitionButtonContent));
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
        : this(new NameChangingMonitor(), null, null, null)
    {
    }

    public BlueprintEditorViewModel(BlueprintModel model)
        : this(new NameChangingMonitor(), null, null, null)
    {
        LoadFromModel(model);
    }

    public BlueprintEditorViewModel(NameChangingMonitor nameMonitor)
        : this(nameMonitor, null, null, null)
    {
    }

    public BlueprintEditorViewModel(
        NameChangingMonitor nameMonitor,
        IBlueprintValidationService? validationService = null,
        IUndoRedoService? undoRedoService = null,
        IBlueprintClipboardService? clipboardService = null)
    {
        _nameMonitor = nameMonitor;
        _validationService = validationService;
        _undoRedoService = undoRedoService;
        _clipboardService = clipboardService;
        LoadFromModel(BlueprintModel.Empty);
    }

    private void PushUndo(string description, System.Action execute, System.Action undo)
    {
        _undoRedoService?.Push(description, execute, undo);
        CanUndo = _undoRedoService?.CanUndo ?? false;
        CanRedo = _undoRedoService?.CanRedo ?? false;
    }

    public void LoadFromModel(BlueprintModel model)
    {
        var main = model.Main;
        var init = model.Init;
        var states = model.States;
        var transactions = model.Transactions;
        var loopTransactions = model.LoopTransactions;

        // Build new state in temporary collections first
        var tempStates = new ObservableCollection<BlueprintStateViewModel>();
        var tempTransitions = new ObservableCollection<BlueprintTransitionViewModel>();
        var tempLoopTransactions = new ObservableCollection<BlueprintLoopTransactionViewModel>();
        var tempNameMonitor = new NameChangingMonitor();
        BlueprintStateViewModel? tempMainState = null;
        BlueprintStateViewModel? tempInitState = null;

        try
        {
            // Register main and init states
            if (main != null)
            {
                tempNameMonitor.TryRegister(main.Name);
                tempMainState = MapState(main, true, false);
            }

            if (init != null)
            {
                tempNameMonitor.TryRegister(init.Name);
                tempInitState = MapState(init, false, true);
            }

            // Load user states
            foreach (var state in states)
            {
                tempNameMonitor.TryRegister(state.Name);
                var vm = MapState(state, false, false);
                tempStates.Add(vm);
            }

            // Load transitions
            foreach (var tx in transactions)
            {
                if (tx.StartStateId == Guid.Empty || tx.EndStateId == Guid.Empty) continue;

                var startState = tempStates.FirstOrDefault(s => s.Id == tx.StartStateId);
                var endState = tempStates.FirstOrDefault(s => s.Id == tx.EndStateId);
                if (startState != null && endState != null)
                {
                    var txVm = new BlueprintTransitionViewModel(tx.Id)
                    {
                        StartStateId = tx.StartStateId,
                        EndStateId = tx.EndStateId,
                        Predicate = tx.Predicate,
                        Alias = tx.Alias
                    };
                    txVm._states = tempStates;
                    tempTransitions.Add(txVm);
                }
            }

            // Load loop transactions
            foreach (var loop in loopTransactions)
            {
                if (loop.StateId == Guid.Empty) continue;

                var state = tempStates.FirstOrDefault(s => s.Id == loop.StateId);
                if (state != null)
                {
                    var loopVm = new BlueprintLoopTransactionViewModel(loop.Id)
                    {
                        StateId = loop.StateId,
                        Predicate = loop.Predicate,
                        Alias = loop.Alias,
                        Text = loop.Text
                    };
                    loopVm._states = tempStates;
                    tempLoopTransactions.Add(loopVm);
                }
            }

            // All loaded successfully — now swap in the new state
            States.Clear();
            Transitions.Clear();
            LoopTransactions.Clear();
            _nameMonitor.Clear();
            SelectedState = null;
            SelectedTransition = null;

            MainState = tempMainState!;
            InitState = tempInitState!;

            foreach (var state in tempStates)
            {
                States.Add(state);
            }
            foreach (var tx in tempTransitions)
            {
                Transitions.Add(tx);
            }
            foreach (var loop in tempLoopTransactions)
            {
                LoopTransactions.Add(loop);
            }

            // Copy names from temp monitor to actual monitor
            foreach (var name in tempNameMonitor.ExposedNames)
            {
                _nameMonitor.TryRegister(name);
            }
        }
        catch
        {
            // If anything fails during loading, restore the original state
            // by clearing everything (caller's try-catch will handle fallback)
            States.Clear();
            Transitions.Clear();
            LoopTransactions.Clear();
            SelectedState = null;
            SelectedTransition = null;
            throw;
        }

        UpdateStateEditability();
        UpdateCanvasSize();

        if (_validationService != null)
        {
            var result = _validationService.Validate(this);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"[BlueprintValidation] {error}");
                }
            }
        }
    }

    public BlueprintModel GetBlueprintModel()
    {
        var statesArray = States
            .Where(s => !s.IsMain && !s.IsInit)
            .Select(s => new BlueprintStateModel
            {
                Id = s.Id,
                CanvasPositionX = s.CanvasPositionX,
                CanvasPositionY = s.CanvasPositionY,
                Name = s.Name,
                Text = s.Text
            })
            .ToImmutableArray();

        var transactionsArray = Transitions
            .Select(tx => new BlueprintTransactionModel
            {
                Id = tx.Id,
                StartStateId = tx.StartStateId,
                EndStateId = tx.EndStateId,
                Predicate = tx.Predicate,
                Alias = tx.Alias
            })
            .ToImmutableArray();

        var loopTransactionsArray = LoopTransactions
            .Select(loop => new BlueprintLoopTransactionModel
            {
                Id = loop.Id,
                StateId = loop.StateId,
                Predicate = loop.Predicate,
                Alias = loop.Alias,
                Text = loop.Text
            })
            .ToImmutableArray();

        return new BlueprintModel
        {
            Main = MapState(MainState, true, false),
            Init = MapState(InitState, false, true),
            States = statesArray,
            Transactions = transactionsArray,
            LoopTransactions = loopTransactionsArray
        };
    }

    private BlueprintStateModel MapState(BlueprintStateViewModel vm, bool isMain, bool isInit)
    {
        return new BlueprintStateModel
        {
            Id = vm.Id,
            CanvasPositionX = vm.CanvasPositionX,
            CanvasPositionY = vm.CanvasPositionY,
            Name = vm.Name,
            Text = vm.Text
        };
    }

    public void OnStatePressed(BlueprintStateViewModel state, double positionX, double positionY, bool isMultiSelect = false)
    {
        if (Mode is EditorMode.AddTransition)
        {
            if (SelectedStates.Count > 0)
            {
                if (state.Name != SelectedStates[0].Name)
                {
                    SelectedState = state;
                    AddTransitionCommand.Execute(null);
                }
                else
                {
                    CreateLoop(state);
                }
            }
            else
            {
                SelectedStates.Add(state);
            }
            return;
        }

        if (Mode is EditorMode.RemoveState)
        {
            SelectedState = state;
            RemoveStateCommand.Execute(null);
            return;
        }

        if (isMultiSelect)
        {
            ToggleStateSelection(state);
        }
        else
        {
            SelectedStates.Clear();
            SelectedState = state;
        }

        _draggingState = state;
    }

    public void ToggleStateSelection(BlueprintStateViewModel state)
    {
        if (state.IsMain || state.IsInit) return;

        if (SelectedStates.Contains(state))
        {
            SelectedStates.Remove(state);
        }
        else
        {
            SelectedStates.Add(state);
        }

        if (SelectedStates.Count == 1)
        {
            SelectedState = SelectedStates[0];
        }
        else if (SelectedStates.Count > 1)
        {
            SelectedState = null;
        }
        else
        {
            SelectedState = null;
        }
    }

    public int SelectedStateCount
    {
        get
        {
            if (SelectedState == null)
                return SelectedStates.Count;

            // Use Id-based comparison since SelectedState may be set via XAML binding
            // with a different object instance representing the same state
            return SelectedStates.Any(s => s.Id == SelectedState.Id) ? SelectedStates.Count : SelectedStates.Count + 1;
        }
    }

    public void OnStateReleased()
    {
        _draggingState = null;
    }

    public void OnStateClicked(BlueprintStateViewModel state)
    {
        SelectedState = state;
    }

    public void OnStateDoubleClicked(BlueprintStateViewModel state)
    {
        OpenStateTextEditor(state);
    }

    public void OnStateNameCommitted(BlueprintStateViewModel state, string? newName)
    {
        var finalName = string.IsNullOrEmpty(newName) ? state.Name : newName;
        UpdateStateName(state, finalName);
    }

    public void OnArrowHeadClicked(BlueprintTransitionViewModel transition, double clickX, double clickY)
    {
        EditArrowRequested?.Invoke(transition, clickX, clickY);
    }

    public void OnArrowBodyClicked(BlueprintTransitionViewModel transition)
    {
        if (Mode is EditorMode.RemoveTransition)
        {
            SelectedTransition = transition;
            RemoveTransitionCommand.Execute(null);
        }
    }

    public void OnLoopArrowHeadClicked(BlueprintLoopTransactionViewModel loop)
    {
        // Single-click on loop arrowhead: open PopOver for editing alias/predicate
        // Double-click on loop arrowbody: open text editor for loop content
        // For now, invoke a PopOver-like event for loop arrows
        OnLoopArrowEditRequested?.Invoke(loop);
    }

    public event Action<BlueprintLoopTransactionViewModel>? OnLoopArrowEditRequested;

    public void OnLoopBodyClicked(BlueprintLoopTransactionViewModel loop)
    {
        if (Mode is EditorMode.RemoveTransition)
        {
            SelectedState = loop.GetState(States);
            RemoveLoopCommand.Execute(null);
        }
    }

    private void CreateLoop(BlueprintStateViewModel state)
    {
        var existingLoop = LoopTransactions.FirstOrDefault(l => l.StateId == state.Id);
        if (existingLoop != null) return;
        var newLoop = new BlueprintLoopTransactionModel
        {
            StateId = state.Id,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        AddLoop(newLoop);
        ResetEditorModeCommand.Execute(null);
    }

    [RelayCommand]
    private void AddState()
    {
        if (Mode is not EditorMode.Default)
            return;

        var newStateName = _nameMonitor.CreateNextDefaultName();
        var newState = new BlueprintStateViewModel
        {
            CanvasPositionX = 10,
            CanvasPositionY = 200,
            Name = newStateName,
            Text = "",
            IsEditable = true,
            IsMain = false,
            IsInit = false,
            FillColor = new SolidColorBrush(Avalonia.Media.Color.Parse("#F08080")),
            StateHeight = 65.0
        };

        _nameMonitor.TryRegister(newStateName);
        States.Add(newState);
        UpdateStateEditability();
        UpdateCanvasSize();
        PushUndo($"Add state '{newStateName}'", () => { }, () =>
        {
            _nameMonitor.TryUnregister(newStateName);
            States.Remove(newState);
            UpdateStateEditability();
            UpdateCanvasSize();
        });
    }

    public void AddStateWithName(string name, double x, double y)
    {
        if (Mode is not EditorMode.Default)
            return;

        if (!_nameMonitor.TryRegister(name))
            return;

        var newState = new BlueprintStateViewModel
        {
            CanvasPositionX = x,
            CanvasPositionY = y,
            Name = name,
            Text = "",
            IsEditable = true,
            IsMain = false,
            IsInit = false,
            FillColor = new SolidColorBrush(Avalonia.Media.Color.Parse("#F08080")),
            StateHeight = 65.0
        };

        States.Add(newState);
        UpdateStateEditability();
        UpdateCanvasSize();
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
        state.Name = newName;
    }

    private bool HasDuplicateTransition(Guid startStateId, Guid endStateId)
    {
        foreach (var tx in Transitions)
        {
            if (tx.StartStateId == Guid.Empty || tx.EndStateId == Guid.Empty) continue;
            if (tx.StartStateId == startStateId && tx.EndStateId == endStateId)
            {
                return true;
            }
        }
        return false;
    }

    [RelayCommand]
    private void AddTransition()
    {
        if (Mode is not EditorMode.AddTransition)
            return;

        if (SelectedState == null)
            return;

        if (SelectedStates.Count == 0)
        {
            SelectedState = null;
            Mode = new EditorMode.Default();
            return;
        }

        var firstState = SelectedStates[0];

        if (firstState.Name == SelectedState.Name)
        {
            var existingLoop = LoopTransactions.FirstOrDefault(l => l.StateId == firstState.Id);
            if (existingLoop == null)
            {
                var newLoop = new BlueprintLoopTransactionModel
                {
                    StateId = firstState.Id,
                    Predicate = "",
                    Alias = "",
                    Text = ""
                };
                AddLoop(newLoop);
            }
        }
        else
        {
            var newTx = new BlueprintTransitionViewModel
            {
                StartStateId = firstState.Id,
                EndStateId = SelectedState.Id,
                Predicate = "",
                Alias = ""
            };

            if (!HasDuplicateTransition(newTx.StartStateId, newTx.EndStateId))
            {
                Transitions.Add(newTx);
            }
        }

        SelectedState = null;
        Mode = new EditorMode.Default();
    }

    [RelayCommand]
    private void RemoveState()
    {
        if (Mode is not EditorMode.RemoveState)
            return;

        if (SelectedState == null)
            return;

        if (SelectedState.IsMain || SelectedState.IsInit)
            return;

        var stateId = SelectedState.Id;
        var stateName = SelectedState.Name;
        var stateCopy = new BlueprintStateViewModel
        {
            Id = stateId,
            Name = stateName,
            CanvasPositionX = SelectedState.CanvasPositionX,
            CanvasPositionY = SelectedState.CanvasPositionY
        };

        var transitionsToRemove = Transitions.Where(tx => tx.StartStateId == stateId || tx.EndStateId == stateId).ToList();
        var loopsToRemove = LoopTransactions.Where(l => l.StateId == stateId).ToList();

        // Perform the removal immediately
        States.Remove(SelectedState!);
        foreach (var tx in transitionsToRemove)
        {
            Transitions.Remove(tx);
        }
        foreach (var loop in loopsToRemove)
        {
            LoopTransactions.Remove(loop);
        }
        _nameMonitor.TryUnregister(stateName);
        SelectedState = null;
        Mode = new EditorMode.Default();
        UpdateCanvasSize();

        // Register undo/redo if service is available
        PushUndo($"Remove state '{stateName}'",
            () =>
            {
                // Redo: re-remove the state (after undo re-added it)
                if (States.Any(s => s.Id == stateId))
                {
                    States.Remove(States.First(s => s.Id == stateId));
                    foreach (var tx in transitionsToRemove)
                    {
                        if (Transitions.All(t => t.StartStateId != tx.StartStateId || t.EndStateId != tx.EndStateId))
                        {
                            Transitions.Remove(tx);
                        }
                    }
                    foreach (var loop in loopsToRemove)
                    {
                        LoopTransactions.Remove(loop);
                    }
                    _nameMonitor.TryUnregister(stateName);
                    UpdateCanvasSize();
                }
            },
            () =>
            {
                // Undo: re-add
                _nameMonitor.TryRegister(stateName);
                States.Add(stateCopy);
                foreach (var tx in transitionsToRemove)
                {
                    Transitions.Add(tx);
                }
                foreach (var loop in loopsToRemove)
                {
                    LoopTransactions.Add(loop);
                }
                UpdateStateEditability();
                UpdateCanvasSize();
            });
    }

    [RelayCommand]
    private void RemoveTransition()
    {
        if (Mode is not EditorMode.RemoveTransition)
            return;

        if (SelectedTransition == null)
            return;

        var startStateId = SelectedTransition.StartStateId;
        var endStateId = SelectedTransition.EndStateId;
        var predicate = SelectedTransition.Predicate ?? "";
        var alias = SelectedTransition.Alias ?? "";

        var transitionToRemove = Transitions.FirstOrDefault(tx =>
            tx.StartStateId == startStateId &&
            tx.EndStateId == endStateId &&
            tx.Predicate == predicate);

        if (transitionToRemove != null)
        {
            Transitions.Remove(transitionToRemove);
        }

        SelectedTransition = null;
        Mode = new EditorMode.Default();

        PushUndo($"Remove transition",
            () =>
            {
                if (Transitions.All(t => t.StartStateId != startStateId || t.EndStateId != endStateId || t.Predicate != predicate))
                {
                    Transitions.Add(new BlueprintTransitionViewModel
                    {
                        StartStateId = startStateId,
                        EndStateId = endStateId,
                        Predicate = predicate,
                        Alias = alias
                    });
                }
            },
            () =>
            {
                var transitionToRemove = Transitions.FirstOrDefault(t =>
                    t.StartStateId == startStateId &&
                    t.EndStateId == endStateId &&
                    t.Predicate == predicate);
                if (transitionToRemove != null)
                {
                    Transitions.Remove(transitionToRemove);
                }
            });
    }

    public void SetTransitionSource(BlueprintStateViewModel state)
    {
        if (Mode is EditorMode.AddTransition)
        {
            SelectedStates.Add(state);
        }
    }

    public BlueprintStateViewModel? GetTransitionSource()
    {
        if (Mode is EditorMode.AddTransition && SelectedStates.Count > 0)
        {
            return SelectedStates[0];
        }
        return null;
    }

    public void AddLoop(BlueprintLoopTransactionModel loopModel)
    {
        var existing = LoopTransactions.FirstOrDefault(l => l.StateId == loopModel.StateId);
        if (existing != null)
        {
            return;
        }

        var newLoop = new BlueprintLoopTransactionViewModel(loopModel.Id)
        {
            StateId = loopModel.StateId,
            Predicate = loopModel.Predicate,
            Alias = loopModel.Alias,
            Text = loopModel.Text
        };
        newLoop._states = States;
        LoopTransactions.Add(newLoop);
        UpdateCanvasSize();
    }

    [RelayCommand]
    private void RemoveLoop()
    {
        if (SelectedState == null)
            return;

        var stateId = SelectedState.Id;

        var loopToRemove = LoopTransactions.FirstOrDefault(l => l.StateId == stateId);
        if (loopToRemove != null)
        {
            var alias = loopToRemove.Alias ?? "";
            var predicate = loopToRemove.Predicate ?? "";

            LoopTransactions.Remove(loopToRemove);

            PushUndo($"Remove loop",
                () =>
                {
                    // Redo: remove again (no-op since already removed synchronously)
                },
                () =>
                {
                    // Undo: re-add the loop
                    if (LoopTransactions.All(l => l.StateId != stateId))
                    {
                        LoopTransactions.Add(new BlueprintLoopTransactionViewModel
                        {
                            StateId = stateId,
                            Predicate = predicate,
                            Alias = alias
                        });
                    }
                });
        }

        SelectedState = null;
        Mode = new EditorMode.Default();
    }

    public void SetBlueprintModel(BlueprintModel model)
    {
        LoadFromModel(model);
    }

    public void OpenStateTextEditor(BlueprintStateViewModel state)
    {
        StateTextEditorRequested?.Invoke(state);
    }

    public void OpenLoopTextEditor(BlueprintLoopTransactionViewModel loop)
    {
        LoopTextEditorRequested?.Invoke(loop);
    }

    public void UpdateStateText(BlueprintStateViewModel state, string newText)
    {
        state.Text = newText;
    }

    public void UpdateLoopText(BlueprintLoopTransactionViewModel loop, string newText)
    {
        loop.Text = newText;
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
        {
            SelectedStates.Clear();
            Mode = new EditorMode.Default();
        }
        else
        {
            SelectedStates.Clear();
            Mode = new EditorMode.AddTransition();
        }
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

    private BlueprintStateViewModel MapState(BlueprintStateModel model, bool isMain, bool isInit)
    {
        var fillColor = new SolidColorBrush(Avalonia.Media.Color.Parse("#F08080"));
        var stateHeight = 65.0;

        if (isMain)
        {
            fillColor = new SolidColorBrush(Avalonia.Media.Color.Parse("#90EE90"));
            stateHeight = 60.0;
        }
        else if (isInit)
        {
            fillColor = new SolidColorBrush(Avalonia.Media.Color.Parse("#ADD8E6"));
            stateHeight = 60.0;
        }

        return new BlueprintStateViewModel
        {
            Id = model.Id,
            CanvasPositionX = model.CanvasPositionX,
            CanvasPositionY = model.CanvasPositionY,
            Name = model.Name,
            Text = model.Text,
            IsEditable = true,
            IsMain = isMain,
            IsInit = isInit,
            FillColor = fillColor,
            StateHeight = stateHeight,
            IsEnabled = !(isMain || isInit)
        };
    }

    public void Dispose()
    {
        IsPopOverOpen = false;
        PopOverViewModel = null;
        States.Clear();
        Transitions.Clear();
        LoopTransactions.Clear();
        _nameMonitor.Clear();
    }

    public Point? ResolveStatePosition(Guid stateId)
    {
        var state = States.FirstOrDefault(s => s.Id == stateId);
        if (state == null) return null;

        var width = 110.0;
        var height = state.StateHeight > 0 ? state.StateHeight : width;
        return new Point(state.CanvasPositionX + width / 2, state.CanvasPositionY + height / 2);
    }
}
