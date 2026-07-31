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

    public static readonly StyledProperty<Point?> StatePositionProperty =
        AvaloniaProperty.Register<LoopArrow, Point?>(nameof(StatePosition));

    /// <summary>
    /// Canvas position of the associated state.
    /// </summary>
    public Point? StatePosition
    {
        get => GetValue(StatePositionProperty);
        set => SetValue(StatePositionProperty, value);
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

    static LoopArrow()
    {
        AffectsRender<LoopArrow>(StateIdProperty, StatePositionProperty, AliasProperty, PredicateProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
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

        // Circle center offset per spec: (60, -40) from state center
        var circleCenter = new Point(center.X + LoopCircleCenterX, center.Y - r);

        // Draw circle with spec stroke width
        context.DrawEllipse(null, new Pen(Avalonia.Media.Brushes.Black, StrokeWidth), circleCenter, r, r);

        // Arrowhead at (100, 0) relative to circle center, pointing right
        var arrowheadPos = new Point(circleCenter.X + LoopArrowheadX, circleCenter.Y + LoopLabelYOffset);
        DrawArrowhead(context, arrowheadPos, 0.0); // 0 radians = pointing right

        // Label to the right of circle per spec: (120, -10)
        var labelText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(labelText))
        {
            var formattedText = new FormattedText(labelText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), LabelFontSize, Avalonia.Media.Brushes.Black);
            context.DrawText(formattedText, new Point(circleCenter.X + LoopLabelX - formattedText.Width, circleCenter.Y + LoopLabelYOffset - formattedText.Height / 2));
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

    private Point GetCenter()
    {
        return StatePosition ?? default;
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

        // Circle center per spec
        var circleCenter = new Point(center.X + LoopCircleCenterX, center.Y - r);

        // Check if click is near the circle edge (body click)
        var dx = position.X - circleCenter.X;
        var dy = position.Y - circleCenter.Y;
        var distFromCenter = Math.Sqrt(dx * dx + dy * dy);
        var distFromEdge = Math.Abs(distFromCenter - r);

        // Check if click is near the arrowhead at (100, 0) relative to circle
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
