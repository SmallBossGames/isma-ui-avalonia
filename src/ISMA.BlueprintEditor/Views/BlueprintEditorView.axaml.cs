using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ISMA.BlueprintEditor.Controls;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Views;

/// <summary>
/// Main blueprint editor view. Renders the canvas with states, transitions, and loops.
/// Handles drag-and-drop, context menu, and popover positioning.
/// </summary>
public partial class BlueprintEditorView : UserControl
{
    private EditArrowPopOverControl? _popOverControl;
    private BlueprintEditorViewModel? _vm;
    private BlueprintStateViewModel? _draggingState;
    private Point _dragStartPointerPosition;
    private Point _dragStartCanvasPosition;
    private bool _wasDragging;

    public BlueprintEditorView()
    {
        InitializeComponent();
        _popOverControl = new EditArrowPopOverControl();
        _popOverControl.DismissRequested += OnPopOverDismissRequested;
        _popOverControl.AliasChanged += OnPopOverAliasChanged;
        _popOverControl.PredicateChanged += OnPopOverPredicateChanged;
        EditArrowPopup.Child = _popOverControl;
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
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void Unsubscribe(BlueprintEditorViewModel vm)
    {
        vm.EditArrowRequested -= OnEditArrowRequested;
        vm.StateTextEditorRequested -= OnStateTextEditorRequested;
        vm.LoopTextEditorRequested -= OnLoopTextEditorRequested;
        vm.SaveProjectRequested -= OnSaveProjectRequested;
        vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = null;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlueprintEditorViewModel.PopOverViewModel) && _popOverControl != null)
        {
            _popOverControl.PopOverViewModel = _vm?.PopOverViewModel;
        }
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
        if (_popOverControl == null) return;

        var scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
        var offset = scrollViewer?.Offset ?? default;

        var popOverWidth = _popOverControl.DesiredSize.Width;
        double left = x - popOverWidth / 2 - offset.X;
        Canvas.SetLeft(_popOverControl, left);
        Canvas.SetTop(_popOverControl, y - offset.Y);

        if (popOverWidth == 0)
        {
            EventHandler<SizeChangedEventArgs>? handler = null;
            handler = (s, ev) =>
            {
                if (_popOverControl != null)
                {
                    _popOverControl.SizeChanged -= handler;
                    var newWidth = _popOverControl.DesiredSize.Width;
                    if (newWidth > 0)
                    {
                        Canvas.SetLeft(_popOverControl, x - newWidth / 2 - offset.X);
                    }
                }
            };
            _popOverControl.SizeChanged += handler;
        }

        if (EditArrowPopup.Child is not Avalonia.Controls.Canvas)
        {
            var canvas = new Avalonia.Controls.Canvas();
            canvas.Children.Add(_popOverControl);
            EditArrowPopup.Child = canvas;
        }

        _popOverControl.PopOverViewModel = _vm?.PopOverViewModel;
    }

    private void OnCanvasKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (e.Key == Key.S && ctrl)
        {
            e.Handled = true;
            _vm.SaveProjectCommand.Execute(null);
        }
        else if (e.Key == Key.Z && ctrl && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            _vm.UndoCommand.Execute(null);
        }
        else if (e.Key == Key.Y && ctrl)
        {
            e.Handled = true;
            _vm.RedoCommand.Execute(null);
        }
        else if (e.Key == Key.C && ctrl)
        {
            e.Handled = true;
            _vm.CopySelectedCommand.Execute(null);
        }
        else if (e.Key == Key.V && ctrl)
        {
            e.Handled = true;
            _vm?.SetPasteOffset(50, 50);
            _vm?.PasteStatesCommand.Execute(null);
        }
        else if (e.Key == Key.Delete)
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
        if (sender is not StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        var position = e.GetPosition(Canvas);
        var isMultiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _vm?.OnStatePressed(stateVm, position.X, position.Y, isMultiSelect);
        _draggingState = stateVm;
        _dragStartPointerPosition = e.GetPosition(Canvas);
        _dragStartCanvasPosition = new Point(stateVm.CanvasPositionX, stateVm.CanvasPositionY);
        _wasDragging = false;
        e.Pointer.Capture(Canvas);
    }

    private void OnStateBoxStateReleased(object? sender, PointerEventArgs e)
    {
        _vm?.OnStateReleased();
        e.Pointer.Capture(null);
        _draggingState = null;

        if (sender is StateBox stateBox && stateBox.DataContext is ISnapPosition snapPosition)
        {
            var gridSize = 20.0;
            snapPosition.SnapPositionX(gridSize);
            snapPosition.SnapPositionY(gridSize);
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingState == null) return;
        var position = e.GetPosition(Canvas);
        var dx = position.X - _dragStartPointerPosition.X;
        var dy = position.Y - _dragStartPointerPosition.Y;
        if (_wasDragging)
        {
            _draggingState.CanvasPositionX = Math.Max(0.0, _dragStartCanvasPosition.X + dx);
            _draggingState.CanvasPositionY = Math.Max(0.0, _dragStartCanvasPosition.Y + dy);
        }
    }

    private void OnStateBoxStateClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStateClicked(stateVm);
    }

    private void OnStateBoxStateDoubleClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStateDoubleClicked(stateVm);
    }

    private void OnStateBoxNameCommitted(object? sender, StateNameCommittedEventArgs e)
    {
        if (sender is not StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        var result = _vm?.OnStateNameCommitted(stateVm, e.NewName) ?? false;
        e.Handled = !result;
    }

    private void OnArrowHeadClicked(object? sender, ArrowHitTestEventArgs e)
    {
        if (sender is not ArrowLine arrow) return;
        var tx = GetTransaction(arrow);
        if (tx != null) _vm?.OnArrowHeadClicked(tx, e.ClickPosition.X, e.ClickPosition.Y);
    }

    private void OnArrowBodyClicked(object? sender, ArrowHitTestEventArgs e)
    {
        if (sender is not ArrowLine arrow) return;
        var tx = GetTransaction(arrow);
        if (tx != null) _vm?.OnArrowBodyClicked(tx);
    }

    private Point? _loopArrowClickPosition;

    private void OnLoopArrowHeadClickedEvent(object? sender, ArrowHitTestEventArgs e)
    {
        if (sender is not LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null)
        {
            _loopArrowClickPosition = e.ClickPosition;
            _vm?.OnLoopArrowHeadClicked(loop);
        }
    }

    private void OnLoopArrowDoubleClick(object? sender, ArrowHitTestEventArgs e)
    {
        if (sender is not LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null)
        {
            _vm?.OpenLoopTextEditor(loop);
        }
    }

    private void OnLoopBodyClicked(object? sender, ArrowHitTestEventArgs e)
    {
        if (sender is not LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null) _vm?.OnLoopBodyClicked(loop);
    }

    private BlueprintTransitionViewModel? GetTransaction(ArrowLine arrow)
    {
        if (arrow.Id == null) return null;
        return _vm?.Transitions.FirstOrDefault(t => t.Id == arrow.Id);
    }

    private BlueprintLoopTransactionViewModel? GetLoop(LoopArrow arrow)
    {
        if (arrow.StateId == Guid.Empty) return null;
        return _vm?.LoopTransactions.FirstOrDefault(l => l.StateId == arrow.StateId);
    }

    private void OnStateTextEditorRequested(BlueprintStateViewModel state)
    {
        // This event is handled by the consuming application
        // The BlueprintEditorViewModel exposes StateTextEditorRequested event
        // which the consumer can subscribe to
    }

    private void OnLoopTextEditorRequested(BlueprintLoopTransactionViewModel loop)
    {
        // This event is handled by the consuming application
    }

    private void OnSaveProjectRequested()
    {
        // This event is handled by the consuming application
    }
}


