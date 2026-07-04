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
    private EditArrowPopOverViewModel? _popOverViewModel;
    private BlueprintEditorViewModel? _vm;

    public BlueprintEditorView()
    {
        InitializeComponent();
        InitializePopOver();
        Loaded += OnLoaded;
    }

    private void InitializePopOver()
    {
        _popOverView = new EditArrowPopOverView();
        _popOverViewModel = new EditArrowPopOverViewModel();
        _popOverView.DataContext = _popOverViewModel;
        _popOverView.DismissRequested += OnPopOverDismissRequested;
        EditArrowPopup.Child = _popOverView;
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
        vm.States.CollectionChanged += (_, _) => Dispatcher.UIThread.InvokeAsync(RecalculateCanvasSize);
        vm.Transactions.CollectionChanged += (_, _) => Dispatcher.UIThread.InvokeAsync(RecalculateCanvasSize);
        vm.LoopTransactions.CollectionChanged += (_, _) => Dispatcher.UIThread.InvokeAsync(RecalculateCanvasSize);
    }

    private void Unsubscribe(BlueprintEditorViewModel vm)
    {
        vm.EditArrowRequested -= OnEditArrowRequested;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(RecalculateCanvasSize);
    }

    private void RecalculateCanvasSize()
    {
        if (_vm == null) return;
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var state in _vm.States)
        {
            minX = Math.Min(minX, state.CanvasPositionX);
            minY = Math.Min(minY, state.CanvasPositionY);
            maxX = Math.Max(maxX, state.CanvasPositionX + 110);
            maxY = Math.Max(maxY, state.CanvasPositionY + state.StateHeight);
        }
        if (minX == double.MaxValue) { Canvas.Width = 1200; Canvas.Height = 800; return; }
        Canvas.Width = Math.Max(400, (maxX - minX) + 200);
        Canvas.Height = Math.Max(300, (maxY - minY) + 200);
    }

    private void OnEditArrowRequested(BlueprintTransactionViewModel tx, double x, double y)
    {
        if (_popOverViewModel == null) return;
        _popOverViewModel.Alias = tx.Alias ?? "";
        _popOverViewModel.Predicate = tx.Predicate ?? "";
        EditArrowPopup.PlacementTarget = Canvas;
        EditArrowPopup.Placement = PlacementMode.Pointer;
        EditArrowPopup.IsOpen = true;
    }

    private void OnPopOverDismissRequested() => EditArrowPopup.IsOpen = false;

    private void OnStateBoxStatePressed(object? sender, PointerEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        var position = e.GetPosition(Canvas);
        _vm?.OnStatePressed(stateVm, position.X, position.Y);
    }

    private void OnStateBoxStateReleased(object? sender, PointerEventArgs e) => _vm?.OnStateReleased();

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

    private void OnLoopBodyClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null) _vm?.OnLoopBodyClicked(loop);
    }

    private BlueprintTransactionViewModel? GetTransaction(Controls.ArrowLine arrow)
    {
        if (arrow.StartState == null || arrow.EndState == null) return null;
        return _vm?.Transactions.FirstOrDefault(t => t.StartState == arrow.StartState && t.EndState == arrow.EndState);
    }

    private BlueprintLoopTransactionViewModel? GetLoop(Controls.LoopArrow arrow)
    {
        if (arrow.State == null) return null;
        return _vm?.LoopTransactions.FirstOrDefault(l => l.State == arrow.State);
    }
}
