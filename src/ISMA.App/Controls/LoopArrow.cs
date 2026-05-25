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
/// </summary>
public class LoopArrow : Control
{
    private const double LoopRadius = 40.0;
    private const double ArrowheadSize = 14.0;
    private const double StateWidth = 120;
    private const double StateHeight = 60;

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

    public static readonly AttachedProperty<Action<LoopArrow>?> LoopClickedProperty =
        AvaloniaProperty.RegisterAttached<LoopArrow, Canvas, Action<LoopArrow>?>("LoopClicked");

    public static void SetLoopClicked(Canvas element, Action<LoopArrow> value)
        => element.SetValue(LoopClickedProperty, value);

    public static Action<LoopArrow>? GetLoopClicked(Canvas element)
        => element.GetValue(LoopClickedProperty);

    public static readonly AttachedProperty<Action<LoopArrow>?> LoopArrowHeadClickedProperty =
        AvaloniaProperty.RegisterAttached<LoopArrow, Canvas, Action<LoopArrow>?>("LoopArrowHeadClicked");

    public static void SetLoopArrowHeadClicked(Canvas element, Action<LoopArrow> value)
        => element.SetValue(LoopArrowHeadClickedProperty, value);

    public static Action<LoopArrow>? GetLoopArrowHeadClicked(Canvas element)
        => element.GetValue(LoopArrowHeadClickedProperty);

    static LoopArrow()
    {
        AffectsRender<LoopArrow>(StateProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (State == null) return;

        var center = GetCenter(State);
        var r = LoopRadius;
        var circleCenter = new Point(center.X + r, center.Y - r);

        // Draw circle
        context.DrawEllipse(null, new Pen(Avalonia.Media.Brushes.Black, 1.5), circleCenter, r, r);

        // Arrowhead at top-left of circle (135 degrees)
        var arrowheadAngle = Math.PI * 0.75;
        var arrowheadPos = new Point(
            circleCenter.X + r * Math.Cos(arrowheadAngle),
            circleCenter.Y + r * Math.Sin(arrowheadAngle));
        DrawArrowhead(context, arrowheadPos, arrowheadAngle);

        // Label to the right of circle
      var labelText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(labelText))
        {
            var formattedText = new FormattedText(labelText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 11, Avalonia.Media.Brushes.Black);
            context.DrawText(formattedText, new Point(circleCenter.X + r + 10, circleCenter.Y - 5));
        }
    }

    private void DrawArrowhead(DrawingContext context, Point center, double angle)
    {
        var halfSize = ArrowheadSize / 2;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        var tip = center;
        var base1 = new Point(
            center.X - cos * halfSize + sin * halfSize * 0.5,
            center.Y - sin * halfSize - cos * halfSize * 0.5);
        var base2 = new Point(
            center.X - cos * halfSize - sin * halfSize * 0.5,
            center.Y - sin * halfSize + cos * halfSize * 0.5);

      var polygon = new StreamGeometry();
        using var ctx = polygon.Open();
        ctx.BeginFigure(tip, false);
        ctx.LineTo(base1, true);
        ctx.LineTo(base2, true);
        ctx.EndFigure(true);

        context.DrawGeometry(Avalonia.Media.Brushes.Black, new Pen(Avalonia.Media.Brushes.Black, 1), polygon);
    }

    private Point GetCenter(BlueprintStateViewModel state)
    {
        return new Point(state.CanvasPositionX + StateWidth / 2, state.CanvasPositionY + StateHeight / 2);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (State == null) return;

        var position = e.GetPosition(this);
        var center = GetCenter(State);
        var r = LoopRadius;
        var circleCenter = new Point(center.X + r, center.Y - r);

        var dx = position.X - (circleCenter.X + r);
        var dy = position.Y - (circleCenter.Y - r);
        var dist = Math.Sqrt(dx * dx + dy * dy);

        var canvas = FindParentCanvas();
        if (canvas == null) return;

        // Check if near arrowhead
        var arrowheadAngle = Math.PI * 0.75;
        var arrowheadPos = new Point(
            circleCenter.X + r * Math.Cos(arrowheadAngle),
            circleCenter.Y + r * Math.Sin(arrowheadAngle));
        var headDist = Math.Sqrt(Math.Pow(position.X - arrowheadPos.X, 2) + Math.Pow(position.Y - arrowheadPos.Y, 2));

        var headAction = GetLoopArrowHeadClicked(canvas);
        var clickAction = GetLoopClicked(canvas);

        if (headDist < ArrowheadSize && headAction != null)
        {
            headAction(this);
        }
        else if (Math.Abs(dist - r) < 8 && clickAction != null)
        {
            clickAction(this);
        }

        e.Handled = true;
    }

  private Canvas? FindParentCanvas()
    {
        Avalonia.Visual? current = this;
        while (current != null)
        {
            if (current is Canvas canvas) return canvas;
            current = current.GetVisualParent();
        }
        return null;
    }
}
