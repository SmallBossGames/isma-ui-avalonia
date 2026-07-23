using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class BlueprintEditorView : UserControl
{
    private EditArrowPopOverView? _popOverView;
    private BlueprintEditorViewModel? _vm;
    // PositionResolver is set via XAML bindings on ArrowLine/LoopArrow controls

    public BlueprintEditorView()
    {
        InitializeComponent();
        _popOverView = new EditArrowPopOverView();
        _popOverView.DismissRequested += OnPopOverDismissRequested;
        _popOverView.AliasChanged += OnPopOverAliasChanged;
        _popOverView.PredicateChanged += OnPopOverPredicateChanged;
        EditArrowPopup.Child = _popOverView;
        Loaded += OnLoaded;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is BlueprintEditorViewModel oldVm)
        {
            Unsubscribe(oldVm);
        }

        if (DataContext is BlueprintEditorViewModel newVm)
        {
            Subscribe(newVm);
        }
    }

    private void Subscribe(BlueprintEditorViewModel vm)
    {
        _vm = vm;
        vm.EditArrowRequested += OnEditArrowRequested;
        vm.StateTextEditorRequested += OnStateTextEditorRequested;
        vm.LoopTextEditorRequested += OnLoopTextEditorRequested;
        vm.SaveProjectRequested += OnSaveProjectRequested;
    }

    private void Unsubscribe(BlueprintEditorViewModel vm)
    {
        vm.EditArrowRequested -= OnEditArrowRequested;
        vm.StateTextEditorRequested -= OnStateTextEditorRequested;
        vm.LoopTextEditorRequested -= OnLoopTextEditorRequested;
        vm.SaveProjectRequested -= OnSaveProjectRequested;
        _vm = null;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // ZIndex is declared in XAML via Panel.ZIndex
    }

    private void OnEditArrowRequested(BlueprintTransitionViewModel tx, double x, double y)
    {
        _vm?.OpenPopOver(tx, x, y);
        Dispatcher.UIThread.InvokeAsync(() => PositionPopOver(x, y), DispatcherPriority.Normal);
    }

    private void PositionPopOver(double x, double y)
    {
        if (_popOverView == null) return;

        if (EditArrowPopup.Child is Canvas parentCanvas)
        {
            Canvas.SetLeft(_popOverView, x);
            Canvas.SetTop(_popOverView, y);
        }
        else
        {
            var canvas = new Canvas();
            canvas.Children.Add(_popOverView);
            Canvas.SetLeft(_popOverView, x);
            Canvas.SetTop(_popOverView, y);
            EditArrowPopup.Child = canvas;
        }
    }

    private void OnCanvasKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (_vm == null) return;

        var ctrl = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control);

        if (e.Key == Avalonia.Input.Key.S && ctrl)
        {
            e.Handled = true;
            _vm.SaveProjectCommand.Execute(null);
        }
        else if (e.Key == Avalonia.Input.Key.Z && ctrl && !e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
        {
            e.Handled = true;
            _vm.UndoCommand.Execute(null);
        }
        else if (e.Key == Avalonia.Input.Key.Y && ctrl)
        {
            e.Handled = true;
            _vm.RedoCommand.Execute(null);
        }
        else if (e.Key == Avalonia.Input.Key.C && ctrl)
        {
            e.Handled = true;
            _vm.CopySelectedCommand.Execute(null);
        }
        else if (e.Key == Avalonia.Input.Key.V && ctrl)
        {
            e.Handled = true;
            _vm?.SetPasteOffset(50, 50);
            _vm?.PasteStatesCommand.Execute(null);
        }
        else if (e.Key == Avalonia.Input.Key.Delete)
        {
            e.Handled = true;
            _vm.DeleteSelectedCommand.Execute(null);
        }
    }

    private void OnPopOverDismissRequested() => _vm?.ClosePopOver();
    private void OnPopOverAliasChanged() => _vm?.OnPopOverAliasChanged();
    private void OnPopOverPredicateChanged() => _vm?.OnPopOverPredicateChanged();

    private void OnStateBoxStatePressed(object? sender, PointerEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        var position = e.GetPosition(Canvas);
        var isMultiSelect = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control);
        _vm?.OnStatePressed(stateVm, position.X, position.Y, isMultiSelect);
    }

    private void OnStateBoxStateReleased(object? sender, PointerEventArgs e)
    {
        _vm?.OnStateReleased();

        if (sender is Controls.StateBox stateBox && stateBox.DataContext is ISnapPosition snapPosition)
        {
            var gridSize = GetGridSize();
            snapPosition.SnapPositionX(gridSize);
            snapPosition.SnapPositionY(gridSize);
        }
    }

    private double GetGridSize()
    {
        // Try to get grid size from DI, fall back to hardcoded default
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.DataContext is MainWindowViewModel mainVm)
        {
            var projectService = mainVm.ProjectService;
            if (projectService.ActiveProject is BlueprintProjectViewModel bpProject)
            {
                // Use the default grid size since IGridSnappingService is not injected into views
                // This could be extended to read from preferences in the future
            }
        }
        return 20.0;
    }

    private void OnStateBoxStateClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStateClicked(stateVm);
    }

    private void OnStateBoxStateDoubleClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStateDoubleClicked(stateVm);
    }

    private void OnStateBoxNameCommitted(object? sender, string? newName)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStateNameCommitted(stateVm, newName);
    }

    private void OnArrowHeadClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.ArrowLine arrow) return;
        var tx = GetTransaction(arrow);
        if (tx != null) _vm?.OnArrowHeadClicked(tx, e.ClickPosition.X, e.ClickPosition.Y);
    }

    private void OnArrowBodyClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.ArrowLine arrow) return;
        var tx = GetTransaction(arrow);
        if (tx != null) _vm?.OnArrowBodyClicked(tx);
    }

    private void OnLoopArrowHeadClickedEvent(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null) _vm?.OnLoopArrowHeadClicked(loop);
    }

    private void OnLoopArrowEditRequested(BlueprintLoopTransactionViewModel loop)
    {
        // Open PopOver for editing loop arrow alias/predicate
        _vm?.OpenPopOverForLoop(loop);
        Dispatcher.UIThread.InvokeAsync(() => PositionPopOverForLoop(), DispatcherPriority.Normal);
    }

    private void PositionPopOverForLoop()
    {
        if (_popOverView == null) return;

        if (EditArrowPopup.Child is Canvas parentCanvas)
        {
            Canvas.SetLeft(_popOverView, 100);
            Canvas.SetTop(_popOverView, 100);
        }
    }

    private void OnLoopBodyClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null) _vm?.OnLoopBodyClicked(loop);
    }

    private BlueprintTransitionViewModel? GetTransaction(Controls.ArrowLine arrow)
    {
        if (arrow.StartStateId == Guid.Empty || arrow.EndStateId == Guid.Empty) return null;
        return _vm?.Transitions.FirstOrDefault(t => t.StartStateId == arrow.StartStateId && t.EndStateId == arrow.EndStateId);
    }

    private BlueprintLoopTransactionViewModel? GetLoop(Controls.LoopArrow arrow)
    {
        if (arrow.StateId == Guid.Empty) return null;
        return _vm?.LoopTransactions.FirstOrDefault(l => l.StateId == arrow.StateId);
    }

    private void OnStateTextEditorRequested(BlueprintStateViewModel state)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var window = topLevel as Avalonia.Controls.Window;
        if (window?.DataContext is not MainWindowViewModel mainVm) return;

        var stateName = state.Name;
        var title = $"State: {stateName}";
        mainVm.OpenStateTextEditorTab(state, title);
    }

    private void OnLoopTextEditorRequested(BlueprintLoopTransactionViewModel loop)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var window = topLevel as Avalonia.Controls.Window;
        if (window?.DataContext is not MainWindowViewModel mainVm) return;

        var loopStateName = loop.GetState(_vm?.States ?? [])?.Name ?? "unknown";
        var title = $"Loop: {loopStateName}";
        mainVm.OpenLoopTextEditorTab(loop, title);
    }

    private void OnSaveProjectRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var window = topLevel as Avalonia.Controls.Window;
        if (window?.DataContext is not MainWindowViewModel mainVm) return;

        var activeProject = mainVm.ProjectService.ActiveProject;
        if (activeProject is BlueprintProjectViewModel bpProject)
        {
            bpProject.SaveAsync();
        }
    }
}