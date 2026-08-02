using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// Renders a transition arrow line between two states with arrowhead and label.
/// Uses atan2-based perpendicular offset for rendering.
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

    public static readonly StyledProperty<Guid?> IdProperty =
        AvaloniaProperty.Register<ArrowLine, Guid?>(nameof(Id));

    public Guid? Id
    {
        get => GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

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

    public static readonly StyledProperty<(double X, double Y)?> StartStatePositionProperty =
        AvaloniaProperty.Register<ArrowLine, (double X, double Y)?>(nameof(StartStatePosition));

    public (double X, double Y)? StartStatePosition
    {
        get => GetValue(StartStatePositionProperty);
        set => SetValue(StartStatePositionProperty, value);
    }

    public static readonly StyledProperty<(double X, double Y)?> EndStatePositionProperty =
        AvaloniaProperty.Register<ArrowLine, (double X, double Y)?>(nameof(EndStatePosition));

    public (double X, double Y)? EndStatePosition
    {
        get => GetValue(EndStatePositionProperty);
        set => SetValue(EndStatePositionProperty, value);
    }

    public static readonly StyledProperty<Func<Guid, (double X, double Y)?>?> PositionResolverProperty =
        AvaloniaProperty.Register<ArrowLine, Func<Guid, (double X, double Y)?>?>(nameof(PositionResolver));

    public Func<Guid, (double X, double Y)?>? PositionResolver
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

    static ArrowLine()
    {
        AffectsRender<ArrowLine>(StartStateIdProperty, EndStateIdProperty, StartStatePositionProperty, EndStatePositionProperty, PositionResolverProperty, AliasProperty, PredicateProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var start = StartStatePosition;
        var end = EndStatePosition;

        if (start.HasValue && end.HasValue)
        {
            var minX = Math.Min(start.Value.X, end.Value.X);
            var minY = Math.Min(start.Value.Y, end.Value.Y);
            var maxX = Math.Max(start.Value.X, end.Value.X);
            var maxY = Math.Max(start.Value.Y, end.Value.Y);

            var padding = Math.Max(ArrowheadSize * 4, TextOffsetX + 20);
            var width = Math.Max(200, (maxX - minX) + padding * 2);
            var height = Math.Max(200, (maxY - minY) + padding * 2);

            return new Size(width, height);
        }

        if (StartStateId != null && EndStateId != null && PositionResolver != null)
        {
            var s = PositionResolver(StartStateId.Value);
            var e = PositionResolver(EndStateId.Value);
            if (s.HasValue && e.HasValue)
            {
                var minX = Math.Min(s.Value.X, e.Value.X);
                var minY = Math.Min(s.Value.Y, e.Value.Y);
                var maxX = Math.Max(s.Value.X, e.Value.X);
                var maxY = Math.Max(s.Value.Y, e.Value.Y);

                var padding = Math.Max(ArrowheadSize * 4, TextOffsetX + 20);
                var width = Math.Max(200, (maxX - minX) + padding * 2);
                var height = Math.Max(200, (maxY - minY) + padding * 2);

                return new Size(width, height);
            }
        }

        return new Size(200, 200);
    }

    public override void Render(DrawingContext context)
    {
        if (StartStateId == null || EndStateId == null) return;

        var start = GetStartCenter();
        var end = GetEndCenter();

        if (!start.HasValue || !end.HasValue) return;

        var dx = end.Value.X - start.Value.X;
        var dy = end.Value.Y - start.Value.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1.0) return;

        var angle = Math.Atan2(dx, dy) + Math.PI / 2;
        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);

        var offsetX = ArrowOffset * sin;
        var offsetY = ArrowOffset * cos;

        var startOffset = new Point(start.Value.X + offsetX, start.Value.Y + offsetY);
        var endOffset = new Point(end.Value.X + offsetX, end.Value.Y + offsetY);

        context.DrawLine(new Pen(Brushes.Black, StrokeWidth), startOffset, endOffset);

        var lineAngle = Math.Atan2(dy, dx);
        DrawArrowhead(context, endOffset, lineAngle);

        var midX = (startOffset.X + endOffset.X) / 2;
        var midY = (startOffset.Y + endOffset.Y) / 2;

        var textOffsetX = TextOffsetX * sin;
        var textOffsetY = TextOffsetY * cos;

        var displayText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(displayText))
        {
            var formattedText = new FormattedText(displayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), LabelFontSize, Brushes.Black);
            context.DrawText(formattedText, new Point(midX + textOffsetX - formattedText.Width / 2, midY + textOffsetY - formattedText.Height / 2));
        }
    }

    private void DrawArrowhead(DrawingContext context, Point center, double angle)
    {
        var halfSize = ArrowheadSize;
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

        context.DrawGeometry(Brushes.Black, new Pen(Brushes.Black, StrokeWidth), polygon);
    }

    private (double X, double Y)? GetStartCenter()
    {
        if (StartStatePosition.HasValue)
            return StartStatePosition;

        if (StartStateId != null && PositionResolver != null)
        {
            return PositionResolver(StartStateId.Value);
        }

        return null;
    }

    private (double X, double Y)? GetEndCenter()
    {
        if (EndStatePosition.HasValue)
            return EndStatePosition;

        if (EndStateId != null && PositionResolver != null)
        {
            return PositionResolver(EndStateId.Value);
        }

        return null;
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

        if (!start.HasValue || !end.HasValue) return;

        var dx = end.Value.X - start.Value.X;
        var dy = end.Value.Y - start.Value.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1.0) return;

        var angle = Math.Atan2(dx, dy) + Math.PI / 2;
        var offsetX = ArrowOffset * Math.Sin(angle);
        var offsetY = ArrowOffset * Math.Cos(angle);
        var endOffset = new Point(end.Value.X + offsetX, end.Value.Y + offsetY);

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
