using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ISMA.ViewModels.ViewModels;

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

    public static readonly StyledProperty<BlueprintStateViewModel?> StateProperty =
        AvaloniaProperty.Register<LoopArrow, BlueprintStateViewModel?>(nameof(State));

    public BlueprintStateViewModel? State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
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
        AffectsRender<LoopArrow>(StateProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (State == null) return;

        var center = GetCenter(State);
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

    private Point GetCenter(BlueprintStateViewModel state)
    {
        var height = state.StateHeight > 0 ? state.StateHeight : StateHeight;
        return new Point(state.CanvasPositionX + StateWidth / 2, state.CanvasPositionY + height / 2);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (State == null) return;

        var position = e.GetPosition(this);
        var center = GetCenter(State);
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

        var clickPos = new Point(position.X, position.Y);

        if (headDist < ArrowheadSize * 2)
        {
            LoopArrowHeadClicked?.Invoke(this, new ArrowHitTestEventArgs(new ArrowHitTestResult { IsArrowHead = true }, clickPos));
        }
        else if (distFromEdge < 10)
        {
            LoopBodyClicked?.Invoke(this, new ArrowHitTestEventArgs(new ArrowHitTestResult { IsArrowBody = true }, clickPos));
        }

        e.Handled = true;
    }
}
