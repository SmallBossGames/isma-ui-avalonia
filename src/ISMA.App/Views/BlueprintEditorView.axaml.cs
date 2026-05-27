using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ISMA.App.Controls;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class BlueprintEditorView : UserControl
{
    private EditArrowPopOverView? _popOverView;
    private EditArrowPopOverViewModel? _popOverViewModel;
    private BlueprintTransactionViewModel? _editingTransaction;
    private const double ArrowOffset = 10.0;
    private const double ArrowheadSize = 14.0;
    private const double LoopRadius = 40.0;
    private const double StateWidth = 120.0;
    private const double StateHeight = 60.0;
    private const double DoubleClickThreshold = 300;
    private const double SingleClickDelay = 200;

    private BlueprintStateViewModel? _draggingState;
    private Point _dragStartPoint;
    private Point _dragOffset;
    private bool _isDragging;
    private DispatcherTimer? _singleClickTimer;
    private Border? _editingBorder;
    private TextBox? _editingTextBox;
    private string? _previousName;
    private BlueprintStateViewModel? _editingState;
    private PointerPoint? _lastPressedPoint;

    public BlueprintEditorView()
    {
        InitializeComponent();
        InitializePopOver();
        InitializeSingleClickTimer();
    }

    private void InitializePopOver()
    {
        _popOverView = new EditArrowPopOverView();
        _popOverViewModel = new EditArrowPopOverViewModel();
        _popOverView.DataContext = _popOverViewModel;

        _popOverView.AliasChanged += OnPopOverAliasChanged;
        _popOverView.PredicateChanged += OnPopOverPredicateChanged;
        _popOverView.DismissRequested += OnPopOverDismissRequested;

        EditArrowPopup.Child = _popOverView;
    }

    private void InitializeSingleClickTimer()
    {
        _singleClickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SingleClickDelay) };
        _singleClickTimer.Tick += (s, e) =>
        {
            _singleClickTimer?.Stop();
            if (!_isDragging && _editingBorder != null && _editingState != null)
            {
                var point = _lastPressedPoint;
                if (point != null)
                {
                    OpenInlineNameEditor(_editingBorder, _editingState, point.Value);
                }
            }
            _editingBorder = null;
            _editingState = null;
            _lastPressedPoint = null;
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is BlueprintEditorViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
    }

    private void OnStatePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not BlueprintStateViewModel stateVm)
            return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        var position = e.GetPosition(Canvas);

        if (e.ClickCount > 1)
        {
            OpenStateTextEditorTab(stateVm);
            _singleClickTimer?.Stop();
            _editingBorder = null;
            _editingState = null;
            return;
        }

        _lastPressedPoint = e.GetCurrentPoint(Canvas);

        if (vm.CurrentMode == BlueprintEditorMode.AddTransition)
        {
            if (vm.SelectedState != null)
            {
                if (stateVm != vm.SelectedState)
                {
                    vm.SelectedState = stateVm;
                    vm.AddTransitionCommand.Execute(null);
                }
                else
                {
                    CreateLoopFromSelected(stateVm);
                }
            }
            else
            {
                vm.SelectedState = stateVm;
            }
            return;
        }

        if (vm.CurrentMode == BlueprintEditorMode.RemoveState)
        {
            vm.SelectedState = stateVm;
            vm.RemoveStateCommand.Execute(null);
            return;
        }

        if (vm.CurrentMode == BlueprintEditorMode.Default && !stateVm.IsMain && !stateVm.IsInit)
        {
            _draggingState = stateVm;
            _dragStartPoint = position;
            _dragOffset = new Point(position.X - stateVm.CanvasPositionX, position.Y - stateVm.CanvasPositionY);
            _isDragging = false;

            _editingBorder = border;
            _editingState = stateVm;
            _previousName = stateVm.Name;
            _singleClickTimer?.Stop();
            _singleClickTimer?.Start();
        }

        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingState == null || _isDragging == false) return;

        var position = e.GetPosition(Canvas);
        var dx = position.X - _dragStartPoint.X;
        var dy = position.Y - _dragStartPoint.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance > 3.0)
        {
            _isDragging = true;
            _singleClickTimer?.Stop();

            var newX = Math.Max(0.0, position.X - _dragOffset.X);
            var newY = Math.Max(0.0, position.Y - _dragOffset.Y);

            _draggingState.CanvasPositionX = newX;
            _draggingState.CanvasPositionY = newY;
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggingState != null)
        {
            if (!_isDragging)
            {
                var vm = DataContext as BlueprintEditorViewModel;
                if (vm != null)
                {
                    vm.SelectedState = _draggingState;
                }
            }

            _draggingState = null;
            _isDragging = false;
        }
    }

    private void OnCanvasPointerExited(object? sender, PointerEventArgs e)
    {
        if (_draggingState != null)
        {
            _draggingState = null;
            _isDragging = false;
            _singleClickTimer?.Stop();
        }
    }

    private void OnArrowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ArrowLine arrow) return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        var position = e.GetPosition(arrow);
        if (arrow.EndState == null) return;

        var end = GetCenter(arrow.EndState);
        var distance = Math.Sqrt(Math.Pow(position.X - end.X, 2) + Math.Pow(position.Y - end.Y, 2));

        if (distance < ArrowheadSize)
        {
            OnArrowHeadClicked(arrow, arrow);
        }
        else if (vm.CurrentMode == BlueprintEditorMode.RemoveTransition)
        {
            OnArrowClicked(arrow, arrow);
        }

        e.Handled = true;
    }

    private void OnLoopArrowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not LoopArrow arrow) return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        if (arrow.State == null) return;

        var position = e.GetPosition(arrow);
        var center = GetCenter(arrow.State);
        var r = LoopRadius;
        var circleCenter = new Point(center.X + r, center.Y - r);

        var dx = position.X - (circleCenter.X + r);
        var dy = position.Y - (circleCenter.Y - r);
        var dist = Math.Sqrt(dx * dx + dy * dy);

        var arrowheadAngle = Math.PI * 0.75;
        var arrowheadPos = new Point(
            circleCenter.X + r * Math.Cos(arrowheadAngle),
            circleCenter.Y + r * Math.Sin(arrowheadAngle));
        var headDist = Math.Sqrt(Math.Pow(position.X - arrowheadPos.X, 2) + Math.Pow(position.Y - arrowheadPos.Y, 2));

        if (headDist < ArrowheadSize)
        {
            OnLoopArrowHeadClicked(arrow, arrow);
        }
        else if (Math.Abs(dist - r) < 8 && vm.CurrentMode == BlueprintEditorMode.RemoveTransition)
        {
            OnLoopClicked(arrow, arrow);
        }

        e.Handled = true;
    }

    private Point GetCenter(BlueprintStateViewModel state)
    {
        return new Point(state.CanvasPositionX + StateWidth / 2, state.CanvasPositionY + StateHeight / 2);
    }

    private void OpenInlineNameEditor(Border border, BlueprintStateViewModel state, PointerPoint point)
    {
        if (state.IsMain || state.IsInit) return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm == null || (vm.CurrentMode != BlueprintEditorMode.Default)) return;

        StopInlineNameEditor();

        var textBox = new TextBox
        {
            Text = state.Name,
            FontSize = 11,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Width = 100,
            MaxWidth = 100
        };

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitInlineName(textBox, state);
            }
            else if (e.Key == Key.Escape)
            {
                CancelInlineName(state);
            }
        };

        textBox.LostFocus += (s, e) =>
        {
            CommitInlineName(textBox, state);
        };

        var grid = new Grid
        {
            Width = 120,
            Height = 60
        };
        grid.Children.Add(textBox);
        border.Child = grid;
        textBox.Focus();
        textBox.SelectAll();

        _editingTextBox = textBox;
        _editingState = state;
    }

    private void CommitInlineName(TextBox textBox, BlueprintStateViewModel state)
    {
        var newName = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            newName = _previousName;
        }

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm != null)
        {
            vm.UpdateStateName(state, newName ?? _previousName!);
        }

        StopInlineNameEditor();
    }

    private void CancelInlineName(BlueprintStateViewModel state)
    {
        if (_previousName != null)
        {
            var vm = DataContext as BlueprintEditorViewModel;
            if (vm != null)
            {
                vm.UpdateStateName(state, _previousName);
            }
        }
        StopInlineNameEditor();
    }

    private void StopInlineNameEditor()
    {
        _editingTextBox = null;
        _editingState = null;
        _previousName = null;
    }

    private void OpenStateTextEditorTab(BlueprintStateViewModel state)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm == null) return;

        var projectVm = vm as ISMA.ViewModels.ViewModels.IProjectViewModel;
        if (projectVm != null)
        {
            var textVm = projectVm as ISMA.ViewModels.ViewModels.LismaProjectViewModel;
            if (textVm != null)
            {
                var newTabName = state.Name;
                var newContent = state.Text;
                textVm.SetContent(newContent);
            }
        }
    }

    private void OnArrowClicked(object? sender, ArrowLine arrow)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        if (vm.CurrentMode == BlueprintEditorMode.RemoveTransition)
        {
            var tx = arrow.StartState != null && arrow.EndState != null
                ? vm.Transactions.FirstOrDefault(t => t.StartState == arrow.StartState && t.EndState == arrow.EndState)
                : null;

            if (tx != null)
            {
                vm.SelectedTransaction = tx;
                vm.RemoveTransitionCommand.Execute(null);
            }
        }
    }

    private void OnArrowHeadClicked(object? sender, ArrowLine arrow)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        var tx = arrow.StartState != null && arrow.EndState != null
            ? vm.Transactions.FirstOrDefault(t => t.StartState == arrow.StartState && t.EndState == arrow.EndState)
            : null;

        if (tx != null)
        {
            _editingTransaction = tx;
            _popOverViewModel!.Alias = tx.Alias;
            _popOverViewModel.Predicate = tx.Predicate;
            EditArrowPopup.IsOpen = true;
        }
    }

    private void OnLoopClicked(object? sender, LoopArrow arrow)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        if (vm.CurrentMode == BlueprintEditorMode.RemoveTransition)
        {
            var loop = arrow.State != null
                ? vm.LoopTransactions.FirstOrDefault(l => l.State == arrow.State)
                : null;

            if (loop != null)
            {
                vm.SelectedState = loop.State;
                vm.RemoveLoopCommand.Execute(null);
            }
        }
    }

    private void OnLoopArrowHeadClicked(object? sender, LoopArrow arrow)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null || arrow.State == null) return;

        var loop = vm.LoopTransactions.FirstOrDefault(l => l.State == arrow.State);
        if (loop != null)
        {
            _editingTransaction = null;
            _popOverViewModel!.Alias = loop.Alias;
            _popOverViewModel.Predicate = loop.Predicate;
            EditArrowPopup.IsOpen = true;
        }
    }

    private void OnPopOverAliasChanged()
    {
        if (_editingTransaction != null && _popOverViewModel != null)
        {
            _editingTransaction.Alias = _popOverViewModel.Alias ?? "";
        }
    }

    private void OnPopOverPredicateChanged()
    {
        if (_editingTransaction != null && _popOverViewModel != null)
        {
            _editingTransaction.Predicate = _popOverViewModel.Predicate ?? "";
        }
    }

    private void OnPopOverDismissRequested()
    {
        EditArrowPopup.IsOpen = false;
        _editingTransaction = null;
    }

    private void CreateLoopFromSelected(BlueprintStateViewModel state)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        foreach (var loop in vm.LoopTransactions)
        {
            if (loop.State == state) return;
        }

        var newLoop = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = state.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };

        vm.AddLoop(newLoop);
        vm.ResetEditorModeCommand.Execute(null);
    }
}
