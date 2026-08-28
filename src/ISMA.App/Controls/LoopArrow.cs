using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ISMA.App.Controls;

/// <summary>
/// Renders a loop arrow (circle) for loop transactions.
/// Circle offset: (60, -40) from state center per spec.
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
    private const double StateWidth = 110.0;
    private const double StateHeight = 65.0;
    private const double StrokeWidth = 3.0;
    private const double LabelFontSize = 16;
    private const double LabelMaxHalfWidth = 80.0;

    public static readonly StyledProperty<Guid?> StateIdProperty =
        AvaloniaProperty.Register<LoopArrow, Guid?>(nameof(StateId));

    /// <summary>
    /// Gets or sets the stable identity of the state this loop arrow is associated with.
    /// </summary>
    public Guid? StateId
    {
        get => GetValue(StateIdProperty);
        set => SetValue(StateIdProperty, value);
    }

    public static readonly StyledProperty<(double X, double Y)?> StatePositionProperty =
        AvaloniaProperty.Register<LoopArrow, (double X, double Y)?>(nameof(StatePosition));

    /// <summary>
    /// Canvas position of the associated state. When set, used for rendering instead of PositionResolver.
    /// </summary>
    public (double X, double Y)? StatePosition
    {
        get => GetValue(StatePositionProperty);
        set => SetValue(StatePositionProperty, value);
    }

    public static readonly StyledProperty<Func<Guid, (double X, double Y)?>?> PositionResolverProperty =
        AvaloniaProperty.Register<LoopArrow, Func<Guid, (double X, double Y)?>?>(nameof(PositionResolver));

    /// <summary>
    /// Resolves a state center point by its Guid. Returns null if not found.
    /// Used as fallback when StatePosition is not set.
    /// </summary>
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
            // Absolute extent from the canvas origin so the control's bounds cover the drawn geometry
            // (the control is pinned at Canvas.Left=0, Canvas.Top=0).
            var maxExtentX = center.X + LoopLabelX + LabelMaxHalfWidth + ArrowheadSize;
            var maxExtentY = center.Y + LoopRadius + ArrowheadSize;
            return new Size(
                Math.Max(200, maxExtentX),
                Math.Max(200, maxExtentY));
        }
        return new Size(200, 200);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return base.ArrangeOverride(finalSize);
    }

    public override void Render(DrawingContext context)
    {
        var center = GetCenter();
        if (center == default) return;

        var r = LoopRadius;

        // Circle center: (60, 0) relative to state center (vertically centered on the state)
        var circleCenter = new Point(center.X + LoopCircleCenterX, center.Y);

        // Draw circle with spec stroke width
        context.DrawEllipse(null, new Pen(Avalonia.Media.Brushes.Black, StrokeWidth), circleCenter, r, r);

        // Arrowhead at (100, 0) relative to state center (the circle's right edge), pointing right
        var arrowheadPos = new Point(center.X + LoopArrowheadX, center.Y);
        DrawArrowhead(context, arrowheadPos, 0.0); // 0 radians = pointing right

        // Label centered at (120, -10) relative to state center
        var labelText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(labelText))
        {
            var formattedText = new FormattedText(labelText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), LabelFontSize, Avalonia.Media.Brushes.Black);
            context.DrawText(formattedText, new Point(center.X + LoopLabelX - formattedText.Width / 2, center.Y + LoopLabelYOffset - formattedText.Height / 2));
        }
    }

    private void DrawArrowhead(DrawingContext context, Point center, double angle)
    {
        // Right-pointing triangle per spec: Polygon(0,-7, 7,0, -7,0)
        var halfSize = ArrowheadSize;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        // Tip points to the right (angle=0)
        var tip = new Point(center.X + halfSize, center.Y);
        var base1 = new Point(center.X - halfSize, center.Y - halfSize * 0.7);
        var base2 = new Point(center.X - halfSize, center.Y + halfSize * 0.7);

        var polygon = new StreamGeometry();
        using var ctx = polygon.Open();
        ctx.BeginFigure(tip, false);
        ctx.LineTo(base1, true);
        ctx.LineTo(base2, true);
        ctx.EndFigure(true);

        context.DrawGeometry(Avalonia.Media.Brushes.Black, new Pen(Avalonia.Media.Brushes.Black, StrokeWidth), polygon);
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

        // Circle center: (60, 0) relative to state center
        var circleCenter = new Point(center.X + LoopCircleCenterX, center.Y);

        // Arrowhead at (100, 0) relative to state center
        var arrowheadPos = new Point(center.X + LoopArrowheadX, center.Y);
        var headDist = Math.Sqrt(Math.Pow(position.X - arrowheadPos.X, 2) + Math.Pow(position.Y - arrowheadPos.Y, 2));

        if (e.ClickCount > 1)
        {
            // Double-click on the arrowhead opens the loop editor (matches original ClickDisambiguator target)
            if (headDist < ArrowheadSize * 2)
            {
                LoopArrowDoubleClick?.Invoke(this, new ArrowHitTestEventArgs(new ArrowHitTestResult { IsArrowHead = true }, position));
                e.Handled = true;
            }
            return;
        }

        // Check if click is near the circle edge (body click)
        var dx = position.X - circleCenter.X;
        var dy = position.Y - circleCenter.Y;
        var distFromCenter = Math.Sqrt(dx * dx + dy * dy);
        var distFromEdge = Math.Abs(distFromCenter - r);

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
