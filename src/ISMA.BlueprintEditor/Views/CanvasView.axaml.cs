using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using ISMA.BlueprintEditor.Controls;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Views;

public partial class CanvasView : Panel
{
    private readonly Dictionary<StateViewModel, StateBox> _stateBoxMap = new();
    private readonly Dictionary<TransactionViewModel, TransactionArrow> _transactionArrowMap = new();
    private readonly Dictionary<LoopTransactionViewModel, LoopTransactionArrow> _loopTransactionArrowMap = new();

    private StateViewModel? _dragState;
    private Point _dragStart;
    private Point _clickOffset;

    private bool _isPointerPressed;
    private Point _pointerPressedPosition;

    public CanvasView()
    {
        AddHandler(PointerPressedEvent, HandlePointerPressed, handledEventsToo: true);
        AddHandler(PointerMovedEvent, HandlePointerMoved, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, HandlePointerReleased, handledEventsToo: true);
    }

    public static readonly StyledProperty<CanvasViewModel?> CanvasViewModelProperty =
        AvaloniaProperty.Register<CanvasView, CanvasViewModel?>(nameof(CanvasViewModel));

    public CanvasViewModel? CanvasViewModel
    {
        get => GetValue(CanvasViewModelProperty);
        set
        {
            if (ReferenceEquals(GetValue(CanvasViewModelProperty), value))
                return;

            UnsubscribeFromCollections();
            SetValue(CanvasViewModelProperty, value);
            SubscribeToCollections();

            SyncAll();
        }
    }

    public static readonly StyledProperty<Action<StateViewModel>?> OnStateDoubleClickProperty =
        AvaloniaProperty.Register<CanvasView, Action<StateViewModel>?>(nameof(OnStateDoubleClick));

    public Action<StateViewModel>? OnStateDoubleClick
    {
        get => GetValue(OnStateDoubleClickProperty);
        set => SetValue(OnStateDoubleClickProperty, value);
    }

    public static readonly StyledProperty<Action<LoopTransactionViewModel, StateViewModel>?> OnLoopStateDoubleClickProperty =
        AvaloniaProperty.Register<CanvasView, Action<LoopTransactionViewModel, StateViewModel>?>(nameof(OnLoopStateDoubleClick));

    public Action<LoopTransactionViewModel, StateViewModel>? OnLoopStateDoubleClick
    {
        get => GetValue(OnLoopStateDoubleClickProperty);
        set => SetValue(OnLoopStateDoubleClickProperty, value);
    }

    private void SubscribeToCollections()
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        vm.States.CollectionChanged += OnStatesChanged;
        vm.Transactions.CollectionChanged += OnTransactionsChanged;
        vm.LoopTransactions.CollectionChanged += OnLoopTransactionsChanged;
    }

    private void UnsubscribeFromCollections()
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        vm.States.CollectionChanged -= OnStatesChanged;
        vm.Transactions.CollectionChanged -= OnTransactionsChanged;
        vm.LoopTransactions.CollectionChanged -= OnLoopTransactionsChanged;
    }

    private void SyncAll()
    {
        SyncStates();
        SyncTransactions();
        SyncLoopTransactions();
    }

    private void OnStatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncStates();
    }

    private void OnTransactionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncTransactions();
    }

    private void OnLoopTransactionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncLoopTransactions();
    }

    private void SyncStates()
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        foreach (var state in vm.States)
        {
            if (!_stateBoxMap.ContainsKey(state))
            {
                CreateStateBox(state);
            }
        }

        var toRemove = _stateBoxMap.Keys.Except(vm.States).ToList();
        foreach (var state in toRemove)
        {
            RemoveStateBox(state);
        }
    }

    private void SyncTransactions()
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        foreach (var tx in vm.Transactions)
        {
            if (!_transactionArrowMap.ContainsKey(tx))
            {
                CreateTransactionArrow(tx);
            }
        }

        var toRemove = _transactionArrowMap.Keys.Except(vm.Transactions).ToList();
        foreach (var tx in toRemove)
        {
            RemoveTransactionArrow(tx);
        }
    }

    private void SyncLoopTransactions()
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        foreach (var loop in vm.LoopTransactions)
        {
            if (!_loopTransactionArrowMap.ContainsKey(loop))
            {
                CreateLoopTransactionArrow(loop);
            }
        }

        var toRemove = _loopTransactionArrowMap.Keys.Except(vm.LoopTransactions).ToList();
        foreach (var loop in toRemove)
        {
            RemoveLoopTransactionArrow(loop);
        }
    }

    private void CreateStateBox(StateViewModel state)
    {
        var box = new StateBox { ViewModel = state };

        box.AddHandler(PointerPressedEvent, StateBoxPointerPressedHandler, handledEventsToo: true);
        box.AddHandler(PointerMovedEvent, StateBoxPointerMovedHandler, handledEventsToo: true);
        box.AddHandler(PointerReleasedEvent, StateBoxPointerReleasedHandler, handledEventsToo: true);

        box.OnClick = _ =>
        {
            var vm = CanvasViewModel;
            if (vm == null)
                return;

            if (vm.EditorMode is AddTransitionMode addMode)
            {
                if (addMode.SelectedStates.Count == 1)
                {
                    var startState = addMode.SelectedStates[0];
                    addMode.SelectedStates.Clear();

                    if (ReferenceEquals(startState, state))
                    {
                        vm.AddLoopArrow(state, "", "", state.Text);
                    }
                    else
                    {
                        vm.AddTransactionArrow(startState, state, "", "");
                    }

                    vm.EditorMode = new IdleMode();
                }
                else
                {
                    addMode.SelectedStates.Add(state);
                }
            }
        };

        box.OnDoubleClick = state =>
        {
            var handler = OnStateDoubleClick;
            handler?.Invoke(state);
        };

        box.PropertyChanged += OnStateBoxPropertyChanged;
        UpdateStateBoxPosition(box, state);

        _stateBoxMap[state] = box;
        Canvas.Children.Add(box);
    }

    private void RemoveStateBox(StateViewModel state)
    {
        if (!_stateBoxMap.TryGetValue(state, out var box))
            return;

        box.PropertyChanged -= OnStateBoxPropertyChanged;
        box.RemoveHandler(PointerPressedEvent, StateBoxPointerPressedHandler);
        box.RemoveHandler(PointerMovedEvent, StateBoxPointerMovedHandler);
        box.RemoveHandler(PointerReleasedEvent, StateBoxPointerReleasedHandler);
        box.Cleanup();

        if (Canvas.Children.Contains(box))
            Canvas.Children.Remove(box);

        _stateBoxMap.Remove(state);
    }

    private void OnStateBoxPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not StateViewModel state)
            return;

        if (e.PropertyName is not (nameof(StateViewModel.X) or nameof(StateViewModel.Y)))
            return;

        if (_stateBoxMap.TryGetValue(state, out var box))
        {
            Canvas.SetLeft(box, state.X);
            Canvas.SetTop(box, state.Y);
        }
    }

    private void UpdateStateBoxPosition(StateBox box, StateViewModel state)
    {
        Canvas.SetLeft(box, state.X);
        Canvas.SetTop(box, state.Y);

        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(StateViewModel.X) or nameof(StateViewModel.Y))
            {
                if (_stateBoxMap.TryGetValue(state, out var b))
                {
                    Canvas.SetLeft(b, state.X);
                    Canvas.SetTop(b, state.Y);
                }
            }
        };
    }

    private void CreateTransactionArrow(TransactionViewModel tx)
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        var startState = vm.States.FirstOrDefault(s => s.Name == tx.StartStateName);
        var endState = vm.States.FirstOrDefault(s => s.Name == tx.EndStateName);
        if (startState == null || endState == null)
            return;

        var arrow = new TransactionArrow { ViewModel = tx };

        arrow.OnClick = _ =>
        {
            var popPosition = default(Point);
            var popover = new EditArrowPopOver(tx, this, popPosition);
            Canvas.Children.Add(popover);

            popover.Closed += (_, _) =>
            {
                if (Canvas.Children.Contains(popover))
                    Canvas.Children.Remove(popover);
            };
        };

        arrow.UpdateArrowPositions(startState, endState);

        _transactionArrowMap[tx] = arrow;
        Canvas.Children.Add(arrow);
    }

    private void RemoveTransactionArrow(TransactionViewModel tx)
    {
        if (!_transactionArrowMap.TryGetValue(tx, out var arrow))
            return;

        arrow.Cleanup();

        if (Canvas.Children.Contains(arrow))
            Canvas.Children.Remove(arrow);

        _transactionArrowMap.Remove(tx);
    }

    private void CreateLoopTransactionArrow(LoopTransactionViewModel loop)
    {
        var vm = CanvasViewModel;
        if (vm == null)
            return;

        var state = vm.States.FirstOrDefault(s => s.Name == loop.StateName);
        if (state == null)
            return;

        var arrow = new LoopTransactionArrow { ViewModel = loop };

        arrow.OnClick = _ =>
        {
            var popPosition = default(Point);
            var txVm = new TransactionViewModel(loop.StateName, loop.StateName, loop.Predicate, loop.Alias);
            var popover = new EditArrowPopOver(txVm, this, popPosition);
            Canvas.Children.Add(popover);

            popover.Closed += (_, _) =>
            {
                if (Canvas.Children.Contains(popover))
                    Canvas.Children.Remove(popover);
            };
        };

        arrow.UpdateLoopPosition(state);

        _loopTransactionArrowMap[loop] = arrow;
        Canvas.Children.Add(arrow);
    }

    private void RemoveLoopTransactionArrow(LoopTransactionViewModel loop)
    {
        if (!_loopTransactionArrowMap.TryGetValue(loop, out var arrow))
            return;

        arrow.Cleanup();

        if (Canvas.Children.Contains(arrow))
            Canvas.Children.Remove(arrow);

        _loopTransactionArrowMap.Remove(loop);
    }

    private void StateBoxPointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not StateBox box || box.ViewModel == null)
            return;

        _isPointerPressed = true;
        _pointerPressedPosition = e.GetPosition(this);
    }

    private void StateBoxPointerMovedHandler(object? sender, PointerMovedEventArgs e)
    {
        if (sender is not StateBox box || box.ViewModel == null || !_isPointerPressed)
            return;

        var position = e.GetPosition(this);
        var dx = Math.Abs(position.X - _pointerPressedPosition.X);
        var dy = Math.Abs(position.Y - _pointerPressedPosition.Y);

        if (dx > 3 || dy > 3)
        {
            _dragState = box.ViewModel;
            _dragStart = position;
            _clickOffset = new Point(position.X - box.ViewModel.X, position.Y - box.ViewModel.Y);
        }
    }

    private void StateBoxPointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
    {
        _isPointerPressed = false;
        _dragState = null;
    }

    private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_dragState == null || sender is not StateBox)
            return;

        var position = e.GetPosition(this);
        _clickOffset = new Point(position.X - _dragState.X, position.Y - _dragState.Y);
    }

    private void HandlePointerMoved(object? sender, PointerMovedEventArgs e)
    {
        if (_dragState == null)
            return;

        var position = e.GetPosition(this);
        var newX = Math.Max(0, position.X - _clickOffset.X);
        var newY = Math.Max(0, position.Y - _clickOffset.Y);
        _dragState.X = newX;
        _dragState.Y = newY;
    }

    private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragState = null;
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentRoot root)
    {
        base.OnAttachedToLogicalTree(root);
        SyncAll();
    }
}
