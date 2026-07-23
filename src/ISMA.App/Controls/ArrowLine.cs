using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
/// Resolves state positions via a callback to decouple from direct ViewModel access.
/// </summary>
public class ArrowLine : Control
{
    private const double ArrowOffset = 10.0;
    private const double ArrowheadSize = 7.0;
    private const double StrokeWidth = 3.0;
    private const double LabelFontSize = 16;
    private const double TextOffsetX = 75.0;
    private const double TextOffsetY = 50.0;

    public static readonly StyledProperty<Guid?> StartStateIdProperty =
        AvaloniaProperty.Register<ArrowLine, Guid?>(nameof(StartStateId));

    public Guid? StartStateId
    {
        get => GetValue(StartStateIdProperty);
        set => SetValue(StartStateIdProperty, value);
    }

    public static readonly StyledProperty<Guid?> EndStateIdProperty =
        AvaloniaProperty.Register<ArrowLine, Guid?>(nameof(EndStateId));

    public Guid? EndStateId
    {
        get => GetValue(EndStateIdProperty);
        set => SetValue(EndStateIdProperty, value);
    }

    public static readonly StyledProperty<Point?> StartStatePositionProperty =
        AvaloniaProperty.Register<ArrowLine, Point?>(nameof(StartStatePosition));

    /// <summary>
    /// Canvas position of the start state. When set, used for rendering instead of PositionResolver.
    /// </summary>
    public Point? StartStatePosition
    {
        get => GetValue(StartStatePositionProperty);
        set => SetValue(StartStatePositionProperty, value);
    }

    public static readonly StyledProperty<Point?> EndStatePositionProperty =
        AvaloniaProperty.Register<ArrowLine, Point?>(nameof(EndStatePosition));

    /// <summary>
    /// Canvas position of the end state. When set, used for rendering instead of PositionResolver.
    /// </summary>
    public Point? EndStatePosition
    {
        get => GetValue(EndStatePositionProperty);
        set => SetValue(EndStatePositionProperty, value);
    }

    public static readonly StyledProperty<Func<Guid, Point?>?> PositionResolverProperty =
        AvaloniaProperty.Register<ArrowLine, Func<Guid, Point?>?>(nameof(PositionResolver));

    /// <summary>
    /// Resolves a state center point by its Guid. Returns null if not found.
    /// Used as fallback when StartStatePosition/EndStatePosition are not set.
    /// </summary>
    public Func<Guid, Point?>? PositionResolver
    {
        get => GetValue(PositionResolverProperty);
        set => SetValue(PositionResolverProperty, value);
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
        AffectsRender<ArrowLine>(StartStateIdProperty, EndStateIdProperty, StartStatePositionProperty, EndStatePositionProperty, PositionResolverProperty, AliasProperty, PredicateProperty);
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
        if (StartStateId == null || EndStateId == null) return;

        var start = GetStartCenter();
        var end = GetEndCenter();

        if (start == default || end == default) return;

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

    private Point GetStartCenter()
    {
        if (StartStatePosition.HasValue)
            return StartStatePosition.Value;

        if (StartStateId != null && PositionResolver != null)
        {
            var center = PositionResolver(StartStateId.Value);
            return center ?? default;
        }

        return default;
    }

    private Point GetEndCenter()
    {
        if (EndStatePosition.HasValue)
            return EndStatePosition.Value;

        if (EndStateId != null && PositionResolver != null)
        {
            var center = PositionResolver(EndStateId.Value);
            return center ?? default;
        }

        return default;
    }

    private Point GetCenter(Guid id)
    {
        if (PositionResolver == null)
            return default;

        var center = PositionResolver(id);
        return center ?? default;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (StartStateId == null || EndStateId == null) return;

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

        var start = GetStartCenter();
        var end = GetEndCenter();

        if (start == default || end == default) return;

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
            ArrowHeadClicked?.Invoke(this, new ArrowHitTestEventArgs(result, position));
        }
        else
        {
            result.IsArrowBody = true;
            ArrowBodyClicked?.Invoke(this, new ArrowHitTestEventArgs(result, position));
        }

        e.Handled = true;
    }
}
