using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// Renders a loop arrow (circle) for loop transactions.
/// Circle offset: (60, -40) from state center.
/// Arrowhead at (100, 0) pointing right.
/// Label at (120, -10).
/// </summary>
public class LoopArrow : Control
{
    private const double LoopRadius = 40.0;
    private const double LoopCircleCenterX = 60.0;
    private const double LoopArrowheadX = 100.0;
    private const double LoopLabelX = 120.0;
    private const double LoopLabelYOffset = -10.0;
    private const double ArrowheadSize = 7.0;
    private const double StrokeWidth = 3.0;
    private const double LabelFontSize = 16;

    public static readonly StyledProperty<Guid?> StateIdProperty =
        AvaloniaProperty.Register<LoopArrow, Guid?>(nameof(StateId));

    public Guid? StateId
    {
        get => GetValue(StateIdProperty);
        set => SetValue(StateIdProperty, value);
    }

    public static readonly StyledProperty<(double X, double Y)?> StatePositionProperty =
        AvaloniaProperty.Register<LoopArrow, (double X, double Y)?>(nameof(StatePosition));

    public (double X, double Y)? StatePosition
    {
        get => GetValue(StatePositionProperty);
        set => SetValue(StatePositionProperty, value);
    }

    public static readonly StyledProperty<Func<Guid, (double X, double Y)?>?> PositionResolverProperty =
        AvaloniaProperty.Register<LoopArrow, Func<Guid, (double X, double Y)?>?>(nameof(PositionResolver));

    public Func<Guid, (double X, double Y)?>? PositionResolver
    {
        get => GetValue(PositionResolverProperty);
        set => SetValue(PositionResolverProperty, value);
    }

    public static readonly StyledProperty<string?> AliasProperty =
        AvaloniaProperty.Register<LoopArrow, string?>(nameof(Alias));

    public string? Alias
    {
        get => GetValue(AliasProperty);
        set => SetValue(AliasProperty, value);
    }

    public static readonly StyledProperty<string?> PredicateProperty =
        AvaloniaProperty.Register<LoopArrow, string?>(nameof(Predicate));

    public string? Predicate
    {
        get => GetValue(PredicateProperty);
        set => SetValue(PredicateProperty, value);
    }

    /// <summary>
    /// Raised when the loop arrowhead is clicked.
    /// </summary>
    public event EventHandler<ArrowHitTestEventArgs>? LoopArrowHeadClicked;

    /// <summary>
    /// Raised when the loop arrow body is clicked.
    /// </summary>
    public event EventHandler<ArrowHitTestEventArgs>? LoopBodyClicked;

    /// <summary>
    /// Raised when the loop arrowhead is double-clicked.
    /// </summary>
    public event EventHandler<ArrowHitTestEventArgs>? LoopArrowDoubleClick;

    static LoopArrow()
    {
        AffectsRender<LoopArrow>(StateIdProperty, StatePositionProperty, PositionResolverProperty, AliasProperty, PredicateProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var center = GetCenter();
        if (center != default)
        {
            var circleCenterX = center.X + LoopCircleCenterX;
            var arrowheadX = circleCenterX + LoopArrowheadX;
            var labelX = circleCenterX + LoopLabelX;
            var maxExtentX = Math.Max(arrowheadX, labelX) + ArrowheadSize;
            var maxExtentY = center.Y + LoopRadius + ArrowheadSize;
            var minExtentX = center.X - LoopRadius - ArrowheadSize;
            var minExtentY = center.Y - LoopRadius * 2 - ArrowheadSize;
            return new Size(
                Math.Max(200, maxExtentX - minExtentX),
                Math.Max(200, maxExtentY - minExtentY));
        }
        return new Size(200, 200);
    }

    public override void Render(DrawingContext context)
    {
        var center = GetCenter();
        if (center == default) return;

        var r = LoopRadius;

        var circleCenter = new Point(center.X + LoopCircleCenterX, center.Y - r);

        context.DrawEllipse(null, new Pen(Brushes.Black, StrokeWidth), circleCenter, r, r);

        var arrowheadPos = new Point(circleCenter.X + LoopArrowheadX, circleCenter.Y + LoopLabelYOffset);
        DrawArrowhead(context, arrowheadPos, 0.0);

        var labelText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(labelText))
        {
            var formattedText = new FormattedText(labelText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), LabelFontSize, Brushes.Black);
            context.DrawText(formattedText, new Point(circleCenter.X + LoopLabelX - formattedText.Width, circleCenter.Y + LoopLabelYOffset - formattedText.Height / 2));
        }
    }

    private void DrawArrowhead(DrawingContext context, Point center, double angle)
    {
        var halfSize = ArrowheadSize;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        var tip = new Point(center.X + halfSize, center.Y);
        var base1 = new Point(center.X - halfSize, center.Y - halfSize * 0.7);
        var base2 = new Point(center.X - halfSize, center.Y + halfSize * 0.7);

        var polygon = new StreamGeometry();
        using var ctx = polygon.Open();
        ctx.BeginFigure(tip, false);
        ctx.LineTo(base1, true);
        ctx.LineTo(base2, true);
        ctx.EndFigure(true);

        context.DrawGeometry(Brushes.Black, new Pen(Brushes.Black, StrokeWidth), polygon);
    }

    private (double X, double Y) GetCenter()
    {
        if (StatePosition.HasValue)
            return StatePosition.Value;

        if (StateId != null && PositionResolver != null)
        {
            var center = PositionResolver(StateId.Value);
            return center ?? default;
        }

        return default;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.ClickCount > 1)
        {
            LoopArrowDoubleClick?.Invoke(this, new ArrowHitTestEventArgs(new ArrowHitTestResult { IsArrowHead = true }, e.GetPosition(this)));
            e.Handled = true;
            return;
        }

        var center = GetCenter();
        if (center == default) return;

        var parent = Parent;
        Point? canvasPosition = null;
        while (parent is not null)
        {
            if (parent is Canvas canvas)
            {
                canvasPosition = e.GetPosition((Visual)canvas);
                break;
            }
            parent = parent.Parent;
        }

        var position = canvasPosition ?? e.GetPosition(this);
        var r = LoopRadius;

        var circleCenter = new Point(center.X + LoopCircleCenterX, center.Y - r);

        var dx = position.X - circleCenter.X;
        var dy = position.Y - circleCenter.Y;
        var distFromCenter = Math.Sqrt(dx * dx + dy * dy);
        var distFromEdge = Math.Abs(distFromCenter - r);

        var arrowheadPos = new Point(circleCenter.X + LoopArrowheadX, circleCenter.Y + LoopLabelYOffset);
        var headDist = Math.Sqrt(Math.Pow(position.X - arrowheadPos.X, 2) + Math.Pow(position.Y - arrowheadPos.Y, 2));

        if (headDist < ArrowheadSize * 2)
        {
            LoopArrowHeadClicked?.Invoke(this, new ArrowHitTestEventArgs(new ArrowHitTestResult { IsArrowHead = true }, position));
        }
        else if (distFromEdge < 10)
        {
            LoopBodyClicked?.Invoke(this, new ArrowHitTestEventArgs(new ArrowHitTestResult { IsArrowBody = true }, position));
        }

        e.Handled = true;
    }
}
