using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.BlueprintEditor.Models;
using ISMA.BlueprintEditor.Services;
using ISMA.BlueprintEditor.Utilities;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// Core blueprint editor view model. Manages the visual state machine / finite state machine (FSM) blueprint canvas.
/// Handles CRUD operations, editor modes, serialization, undo/redo, and clipboard.
/// </summary>
public partial class BlueprintEditorViewModel : ObservableObject, IDisposable
{
    private readonly NameChangingMonitor _nameMonitor;
    private readonly BlueprintValidationService? _validationService;
    private readonly IUndoRedoService? _undoRedoService;
    private readonly BlueprintClipboardService? _clipboardService;
    private Guid _blueprintId;
    private double _pasteOffsetX;
    private double _pasteOffsetY;
    private BlueprintTransitionViewModel? _currentEditingTransition;
    private BlueprintLoopTransactionViewModel? _currentEditingLoop;
    private double _popOverPositionX;
    private double _popOverPositionY;

    /// <summary>
    /// User-defined states on the canvas.
    /// </summary>
    public ObservableCollection<BlueprintStateViewModel> States { get; } = new();

    /// <summary>
    /// Transitions between states.
    /// </summary>
    public ObservableCollection<BlueprintTransitionViewModel> Transitions { get; } = new();

    /// <summary>
    /// Loopback transitions on single states.
    /// </summary>
    public ObservableCollection<BlueprintLoopTransactionViewModel> LoopTransactions { get; } = new();

    /// <summary>
    /// The permanent main state node.
    /// </summary>
    [ObservableProperty]
    private BlueprintStateViewModel _mainState = new();

    /// <summary>
    /// The permanent initialization state node.
    /// </summary>
    [ObservableProperty]
    private BlueprintStateViewModel _initState = new();

    /// <summary>
    /// Currently selected states (for multi-selection).
    /// </summary>
    public ObservableCollection<BlueprintStateViewModel> SelectedStates { get; } = new();

    /// <summary>
    /// Current editor interaction mode.
    /// </summary>
    [ObservableProperty]
    private EditorMode _mode = new EditorMode.Default();

    /// <summary>
    /// Currently selected state.
    /// </summary>
    [ObservableProperty]
    private BlueprintStateViewModel? _selectedState;

    /// <summary>
    /// Currently selected transition.
    /// </summary>
    [ObservableProperty]
    private BlueprintTransitionViewModel? _selectedTransition;

    /// <summary>
    /// Gets whether the editor is in default mode.
    /// </summary>
    public bool IsDefaultMode => Mode is EditorMode.Default;

    /// <summary>
    /// Gets whether the editor is in add transition mode.
    /// </summary>
    public bool IsAddTransitionMode => Mode is EditorMode.AddTransition;

    /// <summary>
    /// Gets whether the editor is in remove state mode.
    /// </summary>
    public bool IsRemoveStateMode => Mode is EditorMode.RemoveState;

    /// <summary>
    /// Gets whether the editor is in remove transition mode.
    /// </summary>
    public bool IsRemoveTransitionMode => Mode is EditorMode.RemoveTransition;

    /// <summary>
    /// Button content for the add transition toggle.
    /// </summary>
    public string AddTransitionButtonContent => Mode switch
    {
        EditorMode.AddTransition => "Stop adding transaction",
        _ => "Add Transition"
    };

    /// <summary>
    /// Button content for the remove state toggle.
    /// </summary>
    public string RemoveStateButtonContent => Mode switch
    {
        EditorMode.RemoveState => "Stop removing state",
        _ => "Remove State"
    };

    /// <summary>
    /// Button content for the remove transition toggle.
    /// </summary>
    public string RemoveTransitionButtonContent => Mode switch
    {
        EditorMode.RemoveTransition => "Stop removing transition",
        _ => "Remove Transition"
    };

    /// <summary>
    /// Popover view model for editing arrow properties.
    /// </summary>
    [ObservableProperty]
    private EditArrowPopOverViewModel? _popOverViewModel;

    /// <summary>
    /// Gets whether the popover is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isPopOverOpen;

    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    [ObservableProperty]
    private bool _canUndo;

    /// <summary>
    /// Gets whether redo is available.
    /// </summary>
    [ObservableProperty]
    private bool _canRedo;

    /// <summary>
    /// Dynamic canvas size based on bounding box of all states.
    /// </summary>
    [ObservableProperty]
    private CanvasSizeDto _canvasSize = new(1200, 800);

    /// <summary>
    /// Resolves a state's center position by its Guid. Used by LoopArrow for rendering.
    /// </summary>
    public Func<Guid, (double X, double Y)?>? PositionResolver => GetStatePosition;

    private (double X, double Y)? GetStatePosition(Guid stateId)
    {
        if (MainState.Id == stateId)
            return (MainState.CanvasPositionX + 55, MainState.CanvasPositionY + MainState.StateHeight / 2);

        if (InitState.Id == stateId)
            return (InitState.CanvasPositionX + 55, InitState.CanvasPositionY + InitState.StateHeight / 2);

        return States.FirstOrDefault(s => s.Id == stateId) is { } state
            ? (state.CanvasPositionX + 55, state.CanvasPositionY + state.StateHeight / 2)
            : null;
    }

    // Events

    /// <summary>
    /// Requested to open a text editor for a state's text content.
    /// </summary>
    public event Action<BlueprintStateViewModel>? StateTextEditorRequested;

    /// <summary>
    /// Requested to open a text editor for a loop's text content.
    /// </summary>
    public event Action<BlueprintLoopTransactionViewModel>? LoopTextEditorRequested;

    /// <summary>
    /// Requested to edit an arrow's properties (alias/predicate).
    /// </summary>
    public event Action<BlueprintTransitionViewModel, double, double>? EditArrowRequested;

    /// <summary>
    /// Requested to save the current project.
    /// </summary>
    public event Action? SaveProjectRequested;

    /// <summary>
    /// Requested to edit a loop arrow (open popover).
    /// </summary>
    public event Action<BlueprintLoopTransactionViewModel>? OnLoopArrowEditRequested;

    // Constructor overloads

    /// <summary>
    /// Creates a new blueprint editor with default settings.
    /// </summary>
    public BlueprintEditorViewModel()
        : this(new NameChangingMonitor(), null, null, null)
    {
    }

    /// <summary>
    /// Creates a new blueprint editor and loads the specified model.
    /// </summary>
    /// <param name="model">The blueprint model to load.</param>
    public BlueprintEditorViewModel(BlueprintModel model)
        : this(new NameChangingMonitor(), null, null, null)
    {
        LoadFromModel(model);
    }

    /// <summary>
    /// Creates a new blueprint editor with a custom name monitor.
    /// </summary>
    /// <param name="nameMonitor">The name changing monitor to use.</param>
    public BlueprintEditorViewModel(NameChangingMonitor nameMonitor)
        : this(nameMonitor, null, null, null)
    {
    }

    /// <summary>
    /// Creates a new blueprint editor with optional dependency injection.
    /// </summary>
    /// <param name="nameMonitor">Name uniqueness tracker.</param>
    /// <param name="validationService">Optional validation service.</param>
    /// <param name="undoRedoService">Optional undo/redo service.</param>
    /// <param name="clipboardService">Optional clipboard service.</param>
    public BlueprintEditorViewModel(
        NameChangingMonitor nameMonitor,
        BlueprintValidationService? validationService = null,
        IUndoRedoService? undoRedoService = null,
        BlueprintClipboardService? clipboardService = null)
    {
        _nameMonitor = nameMonitor;
        _validationService = validationService;
        _undoRedoService = undoRedoService;
        _clipboardService = clipboardService;
        _blueprintId = Guid.NewGuid();
        LoadFromModel(BlueprintModel.Empty);
    }

    // Commands

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
            PushUndo("Delete transition",
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

        foreach (var tx in transitionsToRemove)
        {
            tx.UnsubscribeFromState();
        }
        foreach (var loop in loopsToRemove)
        {
            loop.UnsubscribeFromState();
        }

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

    /// <summary>
    /// Sets the paste offset for clipboard paste operations.
    /// </summary>
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

    // Popover

    /// <summary>
    /// Opens the arrow editing popover for a transition.
    /// </summary>
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

    /// <summary>
    /// Opens the arrow editing popover for a loop.
    /// </summary>
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
        _popOverPositionX = 0;
        _popOverPositionY = 0;
    }

    /// <summary>
    /// Gets the popover X position.
    /// </summary>
    public double PopOverPositionX => _popOverPositionX;

    /// <summary>
    /// Gets the popover Y position.
    /// </summary>
    public double PopOverPositionY => _popOverPositionY;

    /// <summary>
    /// Closes the popover and applies changes.
    /// </summary>
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

    /// <summary>
    /// Called when the popover alias changes.
    /// </summary>
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

    /// <summary>
    /// Called when the popover predicate changes.
    /// </summary>
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

    // Canvas size

    private void UpdateCanvasSize()
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        if (MainState != null)
        {
            minX = Math.Min(minX, MainState.CanvasPositionX);
            minY = Math.Min(minY, MainState.CanvasPositionY);
            maxX = Math.Max(maxX, MainState.CanvasPositionX + 110);
            maxY = Math.Max(maxY, MainState.CanvasPositionY + MainState.StateHeight);
        }

        if (InitState != null)
        {
            minX = Math.Min(minX, InitState.CanvasPositionX);
            minY = Math.Min(minY, InitState.CanvasPositionY);
            maxX = Math.Max(maxX, InitState.CanvasPositionX + 110);
            maxY = Math.Max(maxY, InitState.CanvasPositionY + InitState.StateHeight);
        }

        foreach (var state in States)
        {
            minX = Math.Min(minX, state.CanvasPositionX);
            minY = Math.Min(minY, state.CanvasPositionY);
            maxX = Math.Max(maxX, state.CanvasPositionX + 110);
            maxY = Math.Max(maxY, state.CanvasPositionY + state.StateHeight);
        }

        if (minX == double.MaxValue)
        {
            CanvasSize = new CanvasSizeDto(1200, 800);
        }
        else
        {
            CanvasSize = new CanvasSizeDto(Math.Max(400, (maxX - minX) + 200), Math.Max(300, (maxY - minY) + 200));
        }
    }

    // Mode changes

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
            state.IsEnabled = Mode is not EditorMode.AddTransition and not EditorMode.RemoveState;
            state.IsEditable = Mode is not EditorMode.AddTransition and not EditorMode.RemoveState;
        }
    }

    // Serialization

    /// <summary>
    /// Loads the editor state from a blueprint model.
    /// Uses atomic load with rollback on failure.
    /// </summary>
    public void LoadFromModel(BlueprintModel model)
    {
        _blueprintId = model.Id;
        var main = model.Main;
        var init = model.Init;
        var states = model.States;
        var transactions = model.Transactions;
        var loopTransactions = model.LoopTransactions;

        var tempStates = new ObservableCollection<BlueprintStateViewModel>();
        var tempTransitions = new ObservableCollection<BlueprintTransitionViewModel>();
        var tempLoopTransactions = new ObservableCollection<BlueprintLoopTransactionViewModel>();
        var tempNameMonitor = new NameChangingMonitor();
        BlueprintStateViewModel? tempMainState = null;
        BlueprintStateViewModel? tempInitState = null;

        try
        {
            if (main != null)
            {
                tempNameMonitor.TryRegister(main.Name);
                tempMainState = CreateStateViewModel(main, true, false);
            }

            if (init != null)
            {
                tempNameMonitor.TryRegister(init.Name);
                tempInitState = CreateStateViewModel(init, false, true);
            }

            foreach (var state in states)
            {
                tempNameMonitor.TryRegister(state.Name);
                var vm = CreateStateViewModel(state, false, false);
                tempStates.Add(vm);
            }

            var allStatesForLoad = new List<BlueprintStateViewModel>(tempStates);
            if (tempMainState != null) allStatesForLoad.Add(tempMainState);
            if (tempInitState != null) allStatesForLoad.Add(tempInitState);
            foreach (var tx in transactions)
            {
                if (tx.StartStateId == Guid.Empty || tx.EndStateId == Guid.Empty) continue;

                var startState = allStatesForLoad.FirstOrDefault(s => s.Id == tx.StartStateId);
                var endState = allStatesForLoad.FirstOrDefault(s => s.Id == tx.EndStateId);
                if (startState != null && endState != null)
                {
                    var txVm = new BlueprintTransitionViewModel(tx.Id, allStatesForLoad)
                    {
                        StartStateId = tx.StartStateId,
                        EndStateId = tx.EndStateId,
                        Predicate = tx.Predicate,
                        Alias = tx.Alias
                    };

                    tempTransitions.Add(txVm);
                }
            }

            var allStatesForLoopLoad = new List<BlueprintStateViewModel>(tempStates);
            if (tempMainState != null) allStatesForLoopLoad.Add(tempMainState);
            if (tempInitState != null) allStatesForLoopLoad.Add(tempInitState);
            foreach (var loop in loopTransactions)
            {
                if (loop.StateId == Guid.Empty) continue;

                var state = tempStates.FirstOrDefault(s => s.Id == loop.StateId);
                if (state != null)
                {
                    var loopVm = new BlueprintLoopTransactionViewModel(loop.Id, allStatesForLoopLoad)
                    {
                        StateId = loop.StateId,
                        Predicate = loop.Predicate,
                        Alias = loop.Alias,
                        Text = loop.Text
                    };
                    tempLoopTransactions.Add(loopVm);
                }
            }

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

            foreach (var name in tempNameMonitor.RegisteredNames)
            {
                _nameMonitor.TryRegister(name);
            }
        }
        catch
        {
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

    /// <summary>
    /// Exports the current editor state to a blueprint model.
    /// </summary>
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
            Id = _blueprintId,
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

    // Interaction handlers

    /// <summary>
    /// Called when a state box is pressed (pointer down).
    /// </summary>
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
    }

    /// <summary>
    /// Toggles the selection of a state.
    /// </summary>
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

    /// <summary>
    /// Called when a state box is released (pointer up).
    /// </summary>
    public void OnStateReleased()
    {
    }

    /// <summary>
    /// Called when a state box is clicked (not dragged).
    /// </summary>
    public void OnStateClicked(BlueprintStateViewModel state)
    {
        SelectedState = state;
    }

    /// <summary>
    /// Called when a state box is double-clicked.
    /// </summary>
    public void OnStateDoubleClicked(BlueprintStateViewModel state)
    {
        OpenStateTextEditor(state);
    }

    /// <summary>
    /// Called when inline name editing commits a new name.
    /// </summary>
    /// <returns>True if the name was accepted.</returns>
    public bool OnStateNameCommitted(BlueprintStateViewModel state, string? newName)
    {
        var finalName = string.IsNullOrEmpty(newName) ? state.Name : newName;
        return UpdateStateName(state, finalName);
    }

    /// <summary>
    /// Called when an arrow head is clicked.
    /// </summary>
    public void OnArrowHeadClicked(BlueprintTransitionViewModel transition, double clickX, double clickY)
    {
        EditArrowRequested?.Invoke(transition, clickX, clickY);
    }

    /// <summary>
    /// Called when an arrow body is clicked.
    /// </summary>
    public void OnArrowBodyClicked(BlueprintTransitionViewModel transition)
    {
        if (Mode is EditorMode.RemoveTransition)
        {
            SelectedTransition = transition;
            RemoveTransitionCommand.Execute(null);
        }
    }

    /// <summary>
    /// Called when a loop arrow head is clicked.
    /// </summary>
    public void OnLoopArrowHeadClicked(BlueprintLoopTransactionViewModel loop)
    {
        OnLoopArrowEditRequested?.Invoke(loop);
    }

    /// <summary>
    /// Called when a loop arrow body is clicked.
    /// </summary>
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

    // CRUD Commands

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
            FillColor = "#F08080",
            StateHeight = 65.0
        };

        _nameMonitor.TryRegister(newStateName);
        States.Add(newState);
        UpdateStateEditability();
        UpdateCanvasSize();
        PushUndo($"Add state '{newStateName}'",
            () =>
            {
                _nameMonitor.TryUnregister(newStateName);
                States.Remove(newState);
                UpdateStateEditability();
                UpdateCanvasSize();
            },
            () =>
            {
                _nameMonitor.TryRegister(newStateName);
                States.Add(newState);
                UpdateStateEditability();
                UpdateCanvasSize();
            });
    }

    /// <summary>
    /// Adds a state with a specific name at a specific position.
    /// </summary>
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
            FillColor = "#F08080",
            StateHeight = 65.0
        };

        States.Add(newState);
        UpdateStateEditability();
        UpdateCanvasSize();
    }

    /// <summary>
    /// Updates the name of a state, ensuring uniqueness.
    /// </summary>
    /// <returns>True if the name was updated successfully.</returns>
    public bool UpdateStateName(BlueprintStateViewModel state, string newName)
    {
        if (state.IsMain || state.IsInit)
            return false;

        var oldName = state.Name;
        if (oldName == newName)
            return false;

        if (!_nameMonitor.TryRegister(newName))
            return false;

        _nameMonitor.TryUnregister(oldName);
        state.Name = newName;
        return true;
    }

    private bool HasDuplicateTransition(Guid startStateId, Guid endStateId, string? predicate = null)
    {
        foreach (var tx in Transitions)
        {
            if (tx.StartStateId == Guid.Empty || tx.EndStateId == Guid.Empty) continue;
            if (tx.StartStateId == startStateId && tx.EndStateId == endStateId)
            {
                var existingPredicate = tx.Predicate ?? "";
                var newPredicate = predicate ?? "";
                if (!string.Equals(newPredicate, existingPredicate, StringComparison.Ordinal))
                    continue;
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
            var allStatesForTx = new List<BlueprintStateViewModel>(States);
            if (MainState != null) allStatesForTx.Add(MainState);
            if (InitState != null) allStatesForTx.Add(InitState);
            var newTx = new BlueprintTransitionViewModel(Guid.NewGuid(), allStatesForTx)
            {
                StartStateId = firstState.Id,
                EndStateId = SelectedState.Id,
                Predicate = "",
                Alias = ""
            };

            if (!HasDuplicateTransition(newTx.StartStateId, newTx.EndStateId, newTx.Predicate))
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

        foreach (var tx in transitionsToRemove)
        {
            tx.UnsubscribeFromState();
        }
        foreach (var loop in loopsToRemove)
        {
            loop.UnsubscribeFromState();
        }

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

        PushUndo($"Remove state '{stateName}'",
            () =>
            {
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

        PushUndo("Remove transition",
            () =>
            {
                if (Transitions.All(t => t.StartStateId != startStateId || t.EndStateId != endStateId || t.Predicate != predicate))
                {
                    var allStatesForUndoTx = new List<BlueprintStateViewModel>(States);
                    if (MainState != null) allStatesForUndoTx.Add(MainState);
                    if (InitState != null) allStatesForUndoTx.Add(InitState);
                    Transitions.Add(new BlueprintTransitionViewModel(Guid.NewGuid(), allStatesForUndoTx)
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

    /// <summary>
    /// Sets the transition source state in add transition mode.
    /// </summary>
    public void SetTransitionSource(BlueprintStateViewModel state)
    {
        if (Mode is EditorMode.AddTransition)
        {
            SelectedStates.Add(state);
        }
    }

    /// <summary>
    /// Gets the transition source state if in add transition mode.
    /// </summary>
    public BlueprintStateViewModel? GetTransitionSource()
    {
        if (Mode is EditorMode.AddTransition && SelectedStates.Count > 0)
        {
            return SelectedStates[0];
        }
        return null;
    }

    /// <summary>
    /// Adds a loop transaction.
    /// </summary>
    public void AddLoop(BlueprintLoopTransactionModel loopModel)
    {
        var existing = LoopTransactions.FirstOrDefault(l => l.StateId == loopModel.StateId);
        if (existing != null)
        {
            return;
        }

        var allStatesForLoop = new List<BlueprintStateViewModel>(States);
        if (MainState != null) allStatesForLoop.Add(MainState);
        if (InitState != null) allStatesForLoop.Add(InitState);
        var newLoop = new BlueprintLoopTransactionViewModel(loopModel.Id, allStatesForLoop)
        {
            StateId = loopModel.StateId,
            Predicate = loopModel.Predicate,
            Alias = loopModel.Alias,
            Text = loopModel.Text
        };
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

            PushUndo("Remove loop",
                () => { },
                () =>
                {
                    if (LoopTransactions.All(l => l.StateId != stateId))
                    {
                        var allStatesForLoopUndo = new List<BlueprintStateViewModel>(States);
                        if (MainState != null) allStatesForLoopUndo.Add(MainState);
                        if (InitState != null) allStatesForLoopUndo.Add(InitState);
                        LoopTransactions.Add(new BlueprintLoopTransactionViewModel(Guid.NewGuid(), allStatesForLoopUndo)
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

    /// <summary>
    /// Sets the blueprint model (alias for LoadFromModel).
    /// </summary>
    public void SetBlueprintModel(BlueprintModel model)
    {
        LoadFromModel(model);
    }

    // Text editor integration

    /// <summary>
    /// Opens a text editor for a state's text content.
    /// </summary>
    public void OpenStateTextEditor(BlueprintStateViewModel state)
    {
        StateTextEditorRequested?.Invoke(state);
    }

    /// <summary>
    /// Opens a text editor for a loop's text content.
    /// </summary>
    public void OpenLoopTextEditor(BlueprintLoopTransactionViewModel loop)
    {
        LoopTextEditorRequested?.Invoke(loop);
    }

    /// <summary>
    /// Updates the text of a state.
    /// </summary>
    public void UpdateStateText(BlueprintStateViewModel state, string newText)
    {
        state.Text = newText;
    }

    /// <summary>
    /// Updates the text of a loop.
    /// </summary>
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

    private BlueprintStateViewModel CreateStateViewModel(BlueprintStateModel model, bool isMain, bool isInit)
    {
        var fillColor = "#F08080";

        if (isMain)
        {
            fillColor = "#90EE90";
        }
        else if (isInit)
        {
            fillColor = "#ADD8E6";
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
            StateHeight = 65.0,
            IsEnabled = !(isMain || isInit)
        };
    }

    // Undo helper

    private void PushUndo(string description, System.Action execute, System.Action undo)
    {
        _undoRedoService?.Push(description, execute, undo);
        CanUndo = _undoRedoService?.CanUndo ?? false;
        CanRedo = _undoRedoService?.CanRedo ?? false;
    }

    /// <summary>
    /// Resolves a state's center position by its Guid.
    /// </summary>
    public (double X, double Y)? ResolveStatePosition(Guid stateId)
    {
        var state = States.FirstOrDefault(s => s.Id == stateId);
        if (state == null) return null;

        var width = 110.0;
        var height = state.StateHeight > 0 ? state.StateHeight : width;
        return (state.CanvasPositionX + width / 2, state.CanvasPositionY + height / 2);
    }

    /// <summary>
    /// Gets the count of selected states.
    /// </summary>
    public int SelectedStateCount => SelectedStates.Count;

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        IsPopOverOpen = false;
        PopOverViewModel = null;
        States.Clear();
        Transitions.Clear();
        LoopTransactions.Clear();
        _nameMonitor.Clear();
    }
}
