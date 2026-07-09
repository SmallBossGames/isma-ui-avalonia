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
/// Hit test result for arrow controls.
/// </summary>
public class ArrowHitTestResult
{
    public bool IsArrowHead { get; set; }
    public bool IsArrowBody { get; set; }
}

/// <summary>
/// Event args for arrow hit test results.
/// </summary>
public class ArrowHitTestEventArgs : EventArgs
{
    public ArrowHitTestResult Result { get; }
    public Point ClickPosition { get; }

    public ArrowHitTestEventArgs(ArrowHitTestResult result, Point clickPosition)
    {
        Result = result;
        ClickPosition = clickPosition;
    }
}

/// <summary>
/// Renders a transition arrow line between two states with arrowhead and label.
/// Uses atan2-based perpendicular offset per spec.
/// </summary>
public class ArrowLine : Control
{
    private const double ArrowOffset = 10.0;
    private const double ArrowheadSize = 7.0;
    private const double StateWidth = 110.0;
    private const double StrokeWidth = 3.0;
    private const double LabelFontSize = 16;
    private const double TextOffsetX = 75.0;
    private const double TextOffsetY = 50.0;

    public static readonly StyledProperty<BlueprintStateViewModel?> StartStateProperty =
        AvaloniaProperty.Register<ArrowLine, BlueprintStateViewModel?>(nameof(StartState));

    public BlueprintStateViewModel? StartState
    {
        get => GetValue(StartStateProperty);
        set => SetValue(StartStateProperty, value);
    }

    public static readonly StyledProperty<BlueprintStateViewModel?> EndStateProperty =
        AvaloniaProperty.Register<ArrowLine, BlueprintStateViewModel?>(nameof(EndState));

    public BlueprintStateViewModel? EndState
    {
        get => GetValue(EndStateProperty);
        set => SetValue(EndStateProperty, value);
    }

    public static readonly StyledProperty<string?> AliasProperty =
        AvaloniaProperty.Register<ArrowLine, string?>(nameof(Alias));

    public string? Alias
    {
        get => GetValue(AliasProperty);
        set => SetValue(AliasProperty, value);
    }

    public static readonly StyledProperty<string?> PredicateProperty =
        AvaloniaProperty.Register<ArrowLine, string?>(nameof(Predicate));

    public string? Predicate
    {
        get => GetValue(PredicateProperty);
        set => SetValue(PredicateProperty, value);
    }

    /// <summary>
    /// Raised when the arrowhead is clicked.
    /// </summary>
    public event EventHandler<ArrowHitTestEventArgs>? ArrowHeadClicked;

    /// <summary>
    /// Raised when the arrow body is clicked.
    /// </summary>
    public event EventHandler<ArrowHitTestEventArgs>? ArrowBodyClicked;

    /// <summary>
    /// Raises the ArrowBodyClicked event. For testing purposes.
    /// </summary>
    public void RaiseArrowBodyClicked()
    {
        var result = new ArrowHitTestResult { IsArrowBody = true };
        ArrowBodyClicked?.Invoke(this, new ArrowHitTestEventArgs(result, new Point(0, 0)));
    }

    /// <summary>
    /// Raises the ArrowHeadClicked event. For testing purposes.
    /// </summary>
    public void RaiseArrowHeadClicked()
    {
        var result = new ArrowHitTestResult { IsArrowHead = true };
        ArrowHeadClicked?.Invoke(this, new ArrowHitTestEventArgs(result, new Point(0, 0)));
    }

    static ArrowLine()
    {
        AffectsRender<ArrowLine>(StartStateProperty, EndStateProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (StartState == null || EndState == null) return;

        var start = GetCenter(StartState);
        var end = GetCenter(EndState);

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1.0) return;

        // atan2-based perpendicular angle per spec
        var angle = Math.Atan2(dx, dy) + Math.PI / 2;
        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);

        var offsetX = ArrowOffset * sin;
        var offsetY = ArrowOffset * cos;

        // Line endpoints with perpendicular offset
        var startOffset = new Point(start.X + offsetX, start.Y + offsetY);
        var endOffset = new Point(end.X + offsetX, end.Y + offsetY);

        // Draw line with spec stroke width
        context.DrawLine(new Pen(Avalonia.Media.Brushes.Black, StrokeWidth), startOffset, endOffset);

        // Arrowhead at end, rotated to match line direction (not perpendicular)
        var lineAngle = Math.Atan2(dy, dx);
        DrawArrowhead(context, endOffset, lineAngle);

        // Label at perpendicular offset from midpoint
        var midX = (startOffset.X + endOffset.X) / 2;
        var midY = (startOffset.Y + endOffset.Y) / 2;

        var textOffsetX = TextOffsetX * sin;
        var textOffsetY = TextOffsetY * cos;

        var displayText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(displayText))
        {
            var formattedText = new FormattedText(displayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), LabelFontSize, Avalonia.Media.Brushes.Black);
            context.DrawText(formattedText, new Point(midX + textOffsetX - formattedText.Width / 2, midY + textOffsetY - formattedText.Height / 2));
        }
    }

    private void DrawArrowhead(DrawingContext context, Point center, double angle)
    {
        // 14x14 isosceles triangle per spec: Polygon(7.0, -7.0, -7.0, 0.0, 7.0, 7.0)
        var halfSize = ArrowheadSize;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        // Tip points along the line direction
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

        context.DrawGeometry(Avalonia.Media.Brushes.Black, new Pen(Avalonia.Media.Brushes.Black, StrokeWidth), polygon);
    }

    private Point GetCenter(BlueprintStateViewModel state)
    {
        var height = state.StateHeight > 0 ? state.StateHeight : StateWidth;
        return new Point(state.CanvasPositionX + StateWidth / 2, state.CanvasPositionY + height / 2);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (StartState == null || EndState == null) return;

        var position = e.GetPosition(this);

        var start = GetCenter(StartState);
        var end = GetCenter(EndState);

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1.0) return;

        // Calculate arrowhead position (endOffset) with perpendicular offset
        var angle = Math.Atan2(dx, dy) + Math.PI / 2;
        var offsetX = ArrowOffset * Math.Sin(angle);
        var offsetY = ArrowOffset * Math.Cos(angle);
        var endOffset = new Point(end.X + offsetX, end.Y + offsetY);

        var distanceToArrowhead = Math.Sqrt(Math.Pow(position.X - endOffset.X, 2) + Math.Pow(position.Y - endOffset.Y, 2));

        var result = new ArrowHitTestResult();

        if (distanceToArrowhead < ArrowheadSize * 2)
        {
            result.IsArrowHead = true;
            ArrowHeadClicked?.Invoke(this, new ArrowHitTestEventArgs(result, new Point(position.X, position.Y)));
        }
        else
        {
            result.IsArrowBody = true;
            ArrowBodyClicked?.Invoke(this, new ArrowHitTestEventArgs(result, new Point(position.X, position.Y)));
        }

        e.Handled = true;
    }
}
