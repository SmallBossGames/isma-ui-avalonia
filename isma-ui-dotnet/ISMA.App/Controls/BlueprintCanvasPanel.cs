using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Controls;

public class BlueprintCanvasPanel : Control
{
    private DispatcherTimer? _doubleClickTimer;
    private DateTime _lastClickTime;
    private Point _lastClickPosition;

    private Point? _dragStartPoint;
    private BlueprintStateViewModel? _dragState;
    private Vector _dragStartOffset;

    private Point? _arrowStartPoint;
    private BlueprintTransactionViewModel? _dragTransaction;

    private Point? _loopStartPoint;
    private BlueprintLoopTransactionViewModel? _dragLoop;

    public static readonly StyledProperty<BlueprintEditorViewModel?> EditorViewModelProperty =
        AvaloniaProperty.Register<BlueprintCanvasPanel, BlueprintEditorViewModel?>(nameof(EditorViewModel));

    public BlueprintEditorViewModel? EditorViewModel
    {
        get => GetValue(EditorViewModelProperty);
        set => SetValue(EditorViewModelProperty, value);
    }

    public event Action<BlueprintStateViewModel?>? StateSelected;
    public event Action<BlueprintStateViewModel?>? StateDoubleClicked;
    public event Action<BlueprintTransactionViewModel?>? ArrowClicked;

    static BlueprintCanvasPanel()
    {
        AffectsRender<BlueprintCanvasPanel>(EditorViewModelProperty);
    }

    public BlueprintCanvasPanel()
    {
        this.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        this.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
        this.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble);
        this.AddHandler(PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Bubble);

        _doubleClickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SystemInformation.DoubleClickTime)
        };
        _doubleClickTimer.Tick += OnDoubleClickTick;
        _doubleClickTimer.Start();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var maxRight = Bounds.Width;
        var maxBottom = Bounds.Height;

        if (EditorViewModel != null)
        {
            foreach (var state in EditorViewModel.States)
            {
                var x = Math.Max(state.CanvasPositionX, 0.0);
                var right = x + 120;
                var bottom = state.CanvasPositionY + 60;
                if (right > maxRight) maxRight = right;
                if (bottom > maxBottom) maxBottom = bottom;
            }

            foreach (var tx in EditorViewModel.Transactions)
            {
                var sx = Math.Max(tx.StartState.CanvasPositionX, 0.0);
                var sy = tx.StartState.CanvasPositionY;
                var ex = Math.Max(tx.EndState.CanvasPositionX, 0.0);
                var ey = tx.EndState.CanvasPositionY;
                var maxX = Math.Max(Math.Max(sx, ex) + 120, Math.Abs(ex - sx) + 120);
                var maxY = Math.Max(Math.Max(sy, ey) + 100, Math.Abs(ey - sy) + 100);
                if (maxX > maxRight) maxRight = maxX;
                if (maxY > maxBottom) maxBottom = maxY;
            }

            foreach (var loop in EditorViewModel.LoopTransactions)
            {
                var sx = Math.Max(loop.State.CanvasPositionX, 0.0);
                var sy = loop.State.CanvasPositionY;
                if (sx + 150 > maxRight) maxRight = sx + 150;
                if (sy + 100 > maxBottom) maxBottom = sy + 100;
            }
        }

        return new Size(Math.Max(availableSize.Width, maxRight), Math.Max(availableSize.Height, maxBottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (EditorViewModel == null)
            return;

        // Draw transactions (arrows)
        foreach (var tx in EditorViewModel.Transactions)
        {
            DrawArrow(context, tx.StartState, tx.EndState, tx.IsSelected);
        }

        // Draw loop transactions
        foreach (var loop in EditorViewModel.LoopTransactions)
        {
            DrawLoopArrow(context, loop.State, loop.IsSelected);
        }

        // Draw drag preview line for AddTransition mode
        if (EditorViewModel.CurrentMode == BlueprintEditorMode.AddTransition &&
            _arrowStartPoint != null && _dragTransaction != null)
        {
            var startX = Math.Max(_dragTransaction.StartState.CanvasPositionX, 0.0) + 60;
            var startY = _dragTransaction.StartState.CanvasPositionY + 30;
            context.DrawLine(
                new Pen(Brushes.Blue, 2) { DashStyle = DashStyle.Dash },
                new Point(startX, startY),
                new Point(startX + 50, startY + 50));
        }
    }

    private void DrawArrow(DrawingContext context, BlueprintStateViewModel start, BlueprintStateViewModel end, bool isSelected)
    {
        var startX = Math.Max(start.CanvasPositionX, 0.0) + 60;
        var startY = start.CanvasPositionY + 30;
        var endX = Math.Max(end.CanvasPositionX, 0.0) + 60;
        var endY = end.CanvasPositionY + 30;

        var dx = endX - startX;
        var dy = endY - startY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1.0) return;

        var nx = dx / dist;
        var ny = dy / dist;

        var adjustedStartX = startX + nx * 30;
        var adjustedStartY = startY + ny * 30;
        var adjustedEndX = endX - nx * 30;
        var adjustedEndY = endY - ny * 30;

        var midX = (adjustedStartX + adjustedEndX) / 2.0;
        var midY = (adjustedStartY + adjustedEndY) / 2.0;
        var perpX = -ny * 20;
        var perpY = nx * 20;

        var cp1x = adjustedStartX + (midX - adjustedStartX) * 0.5 + perpX;
        var cp1y = adjustedStartY + (midY - adjustedStartY) * 0.5 + perpY;
        var cp2x = adjustedEndX - (adjustedEndX - midX) * 0.5 + perpX;
        var cp2y = adjustedEndY - (adjustedEndY - midY) * 0.5 + perpY;

        var pen = new Pen(isSelected ? Brushes.Blue : Brushes.Black, 1.5f);

        // Draw curved line
        var figure = new PathFigure
        {
            StartPoint = new Point(adjustedStartX, adjustedStartY),
            IsClosed = false
        };
        figure.Segments.Add(new BezierSegment(
            new Point(cp1x, cp1y),
            new Point(cp2x, cp2y),
            new Point(adjustedEndX, adjustedEndY),
            true));

        var geometry = new PathGeometry(new[] { figure });
        context.DrawGeometry(null, pen, geometry);

        // Arrowhead
        var arrowAngle = Math.Atan2(adjustedEndY - cp2y, adjustedEndX - cp2x);
        var arrowSize = 10.0;
        var arrowX = adjustedEndX;
        var arrowY = adjustedEndY;
        var p1x = arrowX - arrowSize * Math.Cos(arrowAngle - Math.PI / 6);
        var p1y = arrowY - arrowSize * Math.Sin(arrowAngle - Math.PI / 6);
        var p2x = arrowX - arrowSize * Math.Cos(arrowAngle + Math.PI / 6);
        var p2y = arrowY - arrowSize * Math.Sin(arrowAngle + Math.PI / 6);

        var arrowGeometry = new GeometryGroup();
        var polygonGeometry = new StreamGeometry();
        using (var ctx = polygonGeometry.Open())
        {
            ctx.BeginFigure(new Point(arrowX, arrowY), true, false);
            ctx.LineTo(new Point(p1x, p1y), true, false);
            ctx.LineTo(new Point(p2x, p2y), true, false);
            ctx.LineTo(new Point(arrowX, arrowY), true, false);
        }
        arrowGeometry.Children.Add(polygonGeometry);
        context.DrawGeometry(Brushes.White, pen, arrowGeometry);

        // Hit testing area (wider invisible line)
        var hitPen = new Pen(Brushes.Transparent, 12f);
        context.DrawLine(hitPen,
            new Point(adjustedStartX, adjustedStartY),
            new Point(adjustedEndX, adjustedEndY));

        // Alias/Predicate label
        var tx = GetTransactionForStates(start, end);
        if (tx != null && !string.IsNullOrEmpty(tx.Alias))
        {
            var labelX = (adjustedStartX + adjustedEndX) / 2.0;
            var labelY = (adjustedStartY + adjustedEndY) / 2.0;
            var formatted = new FormattedText(
                tx.Alias,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                Typeface.Default,
                9,
                Brushes.DarkGray);
            context.DrawText(formatted, new Point(labelX - formatted.Width / 2, labelY - formatted.Height / 2));
        }
    }

    private void DrawLoopArrow(DrawingContext context, BlueprintStateViewModel state, bool isSelected)
    {
        var x = Math.Max(state.CanvasPositionX, 0.0);
        var y = state.CanvasPositionY;

        var loopX = x + 80;
        var loopY = y - 40;
        var endX = x + 60;
        var endY = y;

        var pen = new Pen(isSelected ? Brushes.Blue : Brushes.Orange, 1.5f);

        var figure = new PathFigure
        {
            StartPoint = new Point(x + 60, y),
            IsClosed = false
        };
        figure.Segments.Add(new BezierSegment(
            new Point(loopX, loopY),
            new Point(endX - 10, loopY),
            new Point(endX, endY),
            true));

        var geometry = new PathGeometry(new[] { figure });
        context.DrawGeometry(null, pen, geometry);

        var arrowAngle = Math.Atan2(endY - loopY, endX - (endX - 10));
        var arrowSize = 8.0;
        var arrowHeadX = endX;
        var arrowHeadY = endY;
        var p1x = arrowHeadX - arrowSize * Math.Cos(arrowAngle - Math.PI / 6);
        var p1y = arrowHeadY - arrowSize * Math.Sin(arrowAngle - Math.PI / 6);
        var p2x = arrowHeadX - arrowSize * Math.Cos(arrowAngle + Math.PI / 6);
        var p2y = arrowHeadY - arrowSize * Math.Sin(arrowAngle + Math.PI / 6);

        var arrowGeometry = new StreamGeometry();
        using (var ctx = arrowGeometry.Open())
        {
            ctx.BeginFigure(new Point(arrowHeadX, arrowHeadY), true, false);
            ctx.LineTo(new Point(p1x, p1y), true, false);
            ctx.LineTo(new Point(p2x, p2y), true, false);
            ctx.LineTo(new Point(arrowHeadX, arrowHeadY), true, false);
        }
        context.DrawGeometry(Brushes.White, pen, arrowGeometry);

        var hitPen = new Pen(Brushes.Transparent, 12f);
        context.DrawLine(hitPen, new Point(x + 60, y), new Point(loopX, loopY));
        context.DrawLine(hitPen, new Point(loopX, loopY), new Point(endX - 10, loopY));

        if (!string.IsNullOrEmpty(state.Text))
        {
            var formatted = new FormattedText(
                state.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                Typeface.Default,
                8,
                Brushes.Orange);
            context.DrawText(formatted, new Point(loopX - formatted.Width / 2, loopY - formatted.Height));
        }
    }

    private BlueprintTransactionViewModel? GetTransactionForStates(BlueprintStateViewModel start, BlueprintStateViewModel end)
    {
        if (EditorViewModel == null) return null;
        foreach (var tx in EditorViewModel.Transactions)
        {
            if (tx.StartState == start && tx.EndState == end)
                return tx;
        }
        return null;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        var now = DateTime.Now;

        // Double-click detection
        var timeDiff = (now - _lastClickTime).TotalMilliseconds;
        var dist = (position - _lastClickPosition).Length;
        if (timeDiff < SystemInformation.DoubleClickTime && dist < 5)
        {
            var state = HitTestState(position);
            if (state != null)
            {
                StateDoubleClicked?.Invoke(state);
            }
            _lastClickTime = now;
            return;
        }

        _lastClickTime = now;
        _lastClickPosition = position;

        // Check for transaction arrow click
        if (EditorViewModel != null)
        {
            var tx = HitTestTransaction(position);
            if (tx != null)
            {
                EditorViewModel.SelectedTransaction = tx;
                ArrowClicked?.Invoke(tx);
                e.Handled = true;
                return;
            }

            var loop = HitTestLoop(position);
            if (loop != null)
            {
                EditorViewModel.SelectedState = loop.State;
                e.Handled = true;
                return;
            }
        }

        // Check for state click
        var hitState = HitTestState(position);
        if (hitState != null)
        {
            if (EditorViewModel != null)
            {
                EditorViewModel.SelectedState = hitState;
            }
            StateSelected?.Invoke(hitState);

            _dragStartPoint = position;
            _dragState = hitState;
            _dragStartOffset = new Point(
                position.X - Math.Max(hitState.CanvasPositionX, 0.0),
                position.Y - hitState.CanvasPositionY);

            if (EditorViewModel != null && EditorViewModel.CurrentMode == BlueprintEditorMode.AddTransition)
            {
                _arrowStartPoint = position;
                _dragTransaction = new BlueprintTransactionViewModel
                {
                    StartState = hitState
                };
            }

            e.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragState != null && _dragStartPoint != null)
        {
            var pos = e.GetPosition(this);
            var newX = Math.Max(pos.X - _dragStartOffset.X, 0.0);
            var newY = Math.Max(pos.Y - _dragStartOffset.Y, 0.0);
            _dragState.CanvasPositionX = newX;
            _dragState.CanvasPositionY = newY;
            InvalidateVisual();
        }
        else if (_dragTransaction != null && _arrowStartPoint != null)
        {
            InvalidateVisual();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragTransaction != null && _arrowStartPoint != null && EditorViewModel != null)
        {
            var pos = e.GetPosition(this);
            var endState = HitTestState(pos);
            if (endState != null && endState != _dragTransaction.StartState)
            {
                _dragTransaction.EndState = endState;
                EditorViewModel.AddTransitionCommand.Execute(null);
            }
            _dragTransaction = null;
            _arrowStartPoint = null;
            InvalidateVisual();
        }

        _dragStartPoint = null;
        _dragState = null;
        _dragLoop = null;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _dragStartPoint = null;
        _dragState = null;
        _dragTransaction = null;
        _arrowStartPoint = null;
        _dragLoop = null;
    }

    private void OnDoubleClickTick(object? sender, EventArgs e)
    {
        if (_doubleClickTimer != null)
        {
            _doubleClickTimer.Stop();
        }
    }

    private BlueprintStateViewModel? HitTestState(Point position)
    {
        if (EditorViewModel == null)
            return null;

        foreach (var state in EditorViewModel.States)
        {
            var x = Math.Max(state.CanvasPositionX, 0.0);
            if (position.X >= x && position.X <= x + 120 &&
                position.Y >= state.CanvasPositionY && position.Y <= state.CanvasPositionY + 60)
            {
                return state;
            }
        }
        return null;
    }

    private BlueprintTransactionViewModel? HitTestTransaction(Point position)
    {
        if (EditorViewModel == null)
            return null;

        foreach (var tx in EditorViewModel.Transactions)
        {
            var startX = Math.Max(tx.StartState.CanvasPositionX, 0.0) + 60;
            var startY = tx.StartState.CanvasPositionY + 30;
            var endX = Math.Max(tx.EndState.CanvasPositionX, 0.0) + 60;
            var endY = tx.EndState.CanvasPositionY + 30;

            var dist = PointToLineDistance(position, new Point(startX, startY), new Point(endX, endY));
            if (dist < 15)
            {
                return tx;
            }
        }
        return null;
    }

    private BlueprintLoopTransactionViewModel? HitTestLoop(Point position)
    {
        if (EditorViewModel == null)
            return null;

        foreach (var loop in EditorViewModel.LoopTransactions)
        {
            var x = Math.Max(loop.State.CanvasPositionX, 0.0);
            var y = loop.State.CanvasPositionY;
            var loopX = x + 80;
            var loopY = y - 40;

            var dist1 = PointToLineDistance(position, new Point(x + 60, y), new Point(loopX, loopY));
            var dist2 = PointToLineDistance(position, new Point(loopX, loopY), new Point(x + 50, y));
            if (dist1 < 15 || dist2 < 15)
            {
                return loop;
            }
        }
        return null;
    }

    private static double PointToLineDistance(Point point, Point lineStart, Point lineEnd)
    {
        var dx = lineEnd.X - lineStart.X;
        var dy = lineEnd.Y - lineStart.Y;
        var lengthSq = dx * dx + dy * dy;
        if (lengthSq < 1.0)
            return (point - lineStart).Length;

        var t = Math.Max(0, Math.Min(1, ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSq));
        var projection = new Point(lineStart.X + t * dx, lineStart.Y + t * dy);
        return (point - projection).Length;
    }
}
