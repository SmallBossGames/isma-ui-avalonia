using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.Utilities;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// A self-transition (loop) arrow of a state, custom-drawn in absolute canvas
/// coordinates relative to the state center: a circle to the right of the
/// state, a right-pointing arrowhead, and a label. Ported from the original
/// Kotlin/JavaFX <c>LoopTransactionArrow</c>. The arrowhead uses 200 ms
/// single/double click disambiguation (double click opens the loop body editor).
/// </summary>
public class LoopTransactionArrow : Control, ICustomHitTest
{
    private const double StrokeHitTolerance = 6.0;
    private const double ArrowheadHitTolerance = 8.0;

    private static readonly Pen StrokePen = new(BlueprintEditorConstants.ArrowColor, BlueprintEditorConstants.ArrowLineStroke);
    private static readonly Pen ArrowheadPen = new(BlueprintEditorConstants.ArrowColor, BlueprintEditorConstants.ArrowheadStroke);
    private static readonly Typeface LabelTypeface = new("Arial");

    private LoopTransactionViewModel? _loop;
    private StateViewModel? _state;
    private readonly ClickDisambiguator _disambiguator;

    /// <summary>Identifies the clicked loop arrow and the click point (canvas coordinates).</summary>
    public LoopTransactionArrow()
    {
        _disambiguator = new ClickDisambiguator(
            onSingleClick: () =>
            {
                if (Loop is { } loop)
                {
                    RaiseEvent(new LoopArrowRoutedEventArgs(ArrowHeadClickedEvent, loop, default(Point)));
                }
            },
            onDoubleClick: () =>
            {
                if (Loop is { } loop && State is { } state)
                {
                    RaiseEvent(new LoopArrowRoutedEventArgs(ArrowHeadDoubleClickedEvent, loop, default(Point)));
                }
            },
            clickDelayMs: (int)BlueprintEditorConstants.ClickDelayMs);
    }

    /// <summary>The loop transaction this arrow renders.</summary>
    public static readonly StyledProperty<LoopTransactionViewModel?> LoopProperty =
        AvaloniaProperty.Register<LoopTransactionArrow, LoopTransactionViewModel?>(nameof(Loop));

    /// <summary>The loop transaction this arrow renders.</summary>
    public LoopTransactionViewModel? Loop
    {
        get => GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

    /// <summary>The state this loop belongs to.</summary>
    public static readonly StyledProperty<StateViewModel?> StateProperty =
        AvaloniaProperty.Register<LoopTransactionArrow, StateViewModel?>(nameof(State));

    /// <summary>The state this loop belongs to.</summary>
    public StateViewModel? State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>Raised when the arrow body (circle stroke or label) is clicked.</summary>
    public static readonly RoutedEvent<LoopArrowRoutedEventArgs> BodyClickedEvent =
        RoutedEvent<LoopArrowRoutedEventArgs>.Register<LoopTransactionArrow, LoopArrowRoutedEventArgs>(nameof(BodyClicked), RoutingStrategies.Bubble);

    /// <summary>Raised when the arrow body (circle stroke or label) is clicked.</summary>
    public event EventHandler<LoopArrowRoutedEventArgs> BodyClicked
    {
        add => AddHandler(BodyClickedEvent, value);
        remove => RemoveHandler(BodyClickedEvent, value);
    }

    /// <summary>Raised on a single click on the arrowhead.</summary>
    public static readonly RoutedEvent<LoopArrowRoutedEventArgs> ArrowHeadClickedEvent =
        RoutedEvent<LoopArrowRoutedEventArgs>.Register<LoopTransactionArrow, LoopArrowRoutedEventArgs>(nameof(ArrowHeadClicked), RoutingStrategies.Bubble);

    /// <summary>Raised on a single click on the arrowhead.</summary>
    public event EventHandler<LoopArrowRoutedEventArgs> ArrowHeadClicked
    {
        add => AddHandler(ArrowHeadClickedEvent, value);
        remove => RemoveHandler(ArrowHeadClickedEvent, value);
    }

    /// <summary>Raised on a double click on the arrowhead.</summary>
    public static readonly RoutedEvent<LoopArrowRoutedEventArgs> ArrowHeadDoubleClickedEvent =
        RoutedEvent<LoopArrowRoutedEventArgs>.Register<LoopTransactionArrow, LoopArrowRoutedEventArgs>(nameof(ArrowHeadDoubleClicked), RoutingStrategies.Bubble);

    /// <summary>Raised on a double click on the arrowhead.</summary>
    public event EventHandler<LoopArrowRoutedEventArgs> ArrowHeadDoubleClicked
    {
        add => AddHandler(ArrowHeadDoubleClickedEvent, value);
        remove => RemoveHandler(ArrowHeadDoubleClickedEvent, value);
    }

    static LoopTransactionArrow()
    {
        LoopProperty.Changed.AddClassHandler<LoopTransactionArrow, LoopTransactionViewModel?>(
            (arrow, _) => arrow.OnLoopChanged());
        StateProperty.Changed.AddClassHandler<LoopTransactionArrow, StateViewModel?>(
            (arrow, _) => arrow.OnStateChanged());
    }

    private void OnLoopChanged()
    {
        if (_loop is { } old)
        {
            old.PropertyChanged -= OnLoopPropertyChanged;
        }

        _loop = Loop;

        if (_loop is not { } loop)
        {
            return;
        }

        loop.PropertyChanged += OnLoopPropertyChanged;
        InvalidateVisual();
    }

    private void OnLoopPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnStateChanged()
    {
        if (_state is { } old)
        {
            old.PropertyChanged -= OnStatePropertyChanged;
        }

        _state = State;

        if (_state is not { } state)
        {
            return;
        }

        state.PropertyChanged += OnStatePropertyChanged;
        Recompute();
    }

    private void OnStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StateViewModel.X) or nameof(StateViewModel.Y)
            or nameof(StateViewModel.SquareWidth) or nameof(StateViewModel.SquareHeight))
        {
            Recompute();
        }
    }

    private void Recompute()
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>The circle center in canvas coordinates.</summary>
    private Point CircleCenter => State is { } state
        ? new Point(state.CenterX + BlueprintEditorConstants.LoopCircleCenterX, state.CenterY)
        : default(Point);

    /// <summary>The arrowhead center in canvas coordinates.</summary>
    private Point ArrowheadCenter => State is { } state
        ? new Point(state.CenterX + BlueprintEditorConstants.LoopArrowheadX, state.CenterY)
        : default(Point);

    /// <summary>The label center in canvas coordinates.</summary>
    private Point LabelCenter => State is { } state
        ? new Point(
            state.CenterX + BlueprintEditorConstants.LoopLabelX,
            state.CenterY + BlueprintEditorConstants.LoopLabelYOffset)
        : default(Point);

    /// <summary>The arrowhead polygon vertices in canvas coordinates (points right).</summary>
    private Point[] ArrowheadVertices
    {
        get
        {
            if (State is null)
            {
                return Array.Empty<Point>();
            }

            var center = ArrowheadCenter;
            double w = BlueprintEditorConstants.ArrowheadWidth;
            return new[]
            {
                new Point(center.X, center.Y - w),
                new Point(center.X + w, center.Y),
                new Point(center.X - w, center.Y),
            };
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (State is null)
        {
            return default(Size);
        }

        var circle = CircleCenter;
        double maxX = circle.X + BlueprintEditorConstants.LoopCircleRadius;
        double maxY = circle.Y + BlueprintEditorConstants.LoopCircleRadius;

        var arrowhead = ArrowheadCenter;
        maxX = Math.Max(maxX, arrowhead.X + BlueprintEditorConstants.ArrowheadWidth);
        maxY = Math.Max(maxY, arrowhead.Y + BlueprintEditorConstants.ArrowheadWidth);

        var label = LabelCenter;
        double labelHalfW = BlueprintEditorConstants.ArrowLabelFieldWidth / 2;
        double labelHalfH = BlueprintEditorConstants.ArrowLabelFontSize;
        maxX = Math.Max(maxX, label.X + labelHalfW);
        maxY = Math.Max(maxY, label.Y + labelHalfH);

        return new Size(maxX + 1, maxY + 1);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (State is null)
        {
            return;
        }

        var circle = CircleCenter;
        context.DrawEllipse(
            null,
            StrokePen,
            circle,
            BlueprintEditorConstants.LoopCircleRadius,
            BlueprintEditorConstants.LoopCircleRadius);

        var vertices = ArrowheadVertices;
        var stream = new StreamGeometry();
        using (var ctx = stream.Open())
        {
            ctx.BeginFigure(vertices[0], isFilled: true);
            ctx.LineTo(vertices[1], isStroked: true);
            ctx.LineTo(vertices[2], isStroked: true);
            ctx.EndFigure(isClosed: true);
        }

        context.DrawGeometry(BlueprintEditorConstants.ArrowColor, ArrowheadPen, stream);

        if (Loop is { } loop)
        {
            var text = loop.DisplayText;
            if (!string.IsNullOrEmpty(text))
            {
                var ft = new FormattedText(
                    text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,
                    BlueprintEditorConstants.ArrowLabelFontSize,
                    BlueprintEditorConstants.ArrowColor);

                var label = LabelCenter;
                context.DrawText(ft, new Point(label.X - ft.Width / 2, label.Y - ft.Height / 2));
            }
        }
    }

    /// <inheritdoc />
    bool ICustomHitTest.HitTest(Point point)
    {
        if (State is null)
        {
            return false;
        }

        var vertices = ArrowheadVertices;
        if (IsNearPolygon(point, vertices, ArrowheadHitTolerance))
        {
            return true;
        }

        var circle = CircleCenter;
        double distToCircle = new Vector(point.X - circle.X, point.Y - circle.Y).Length;
        if (Math.Abs(distToCircle - BlueprintEditorConstants.LoopCircleRadius) <= StrokeHitTolerance)
        {
            return true;
        }

        var label = LabelCenter;
        double labelHalfW = BlueprintEditorConstants.ArrowLabelFieldWidth / 2;
        double labelHalfH = BlueprintEditorConstants.ArrowLabelFontSize;
        if (Math.Abs(point.X - label.X) <= labelHalfW && Math.Abs(point.Y - label.Y) <= labelHalfH)
        {
            return true;
        }

        return false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Loop is not { } loop || State is null)
        {
            return;
        }

        var point = e.GetPosition(this);
        var vertices = ArrowheadVertices;
        if (IsNearPolygon(point, vertices, ArrowheadHitTolerance))
        {
            _disambiguator.OnKeyPress();
            _disambiguator.OnClick(e.ClickCount);
            RaiseEvent(new LoopArrowRoutedEventArgs(BodyClickedEvent, loop, point));
            e.Handled = true;
            return;
        }

        var circle = CircleCenter;
        double distToCircle = new Vector(point.X - circle.X, point.Y - circle.Y).Length;
        if (Math.Abs(distToCircle - BlueprintEditorConstants.LoopCircleRadius) <= StrokeHitTolerance)
        {
            RaiseEvent(new LoopArrowRoutedEventArgs(BodyClickedEvent, loop, point));
            e.Handled = true;
            return;
        }

        var label = LabelCenter;
        double labelHalfW = BlueprintEditorConstants.ArrowLabelFieldWidth / 2;
        double labelHalfH = BlueprintEditorConstants.ArrowLabelFontSize;
        if (Math.Abs(point.X - label.X) <= labelHalfW && Math.Abs(point.Y - label.Y) <= labelHalfH)
        {
            RaiseEvent(new LoopArrowRoutedEventArgs(BodyClickedEvent, loop, point));
            e.Handled = true;
        }
    }

    private static bool IsNearPolygon(Point point, Point[] vertices, double tolerance)
    {
        if (IsPointInPolygon(point, vertices))
        {
            return true;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertices.Length];
            if (DistanceToSegment(point, a, b) <= tolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInPolygon(Point p, Point[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            var pi = poly[i];
            var pj = poly[j];
            if ((pi.Y > p.Y) != (pj.Y > p.Y)
                && p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0)
        {
            return new Vector(p.X - a.X, p.Y - a.Y).Length;
        }

        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared;
        t = Math.Clamp(t, 0, 1);
        var closest = a + new Vector(dx * t, dy * t);
        return new Vector(p.X - closest.X, p.Y - closest.Y).Length;
    }
}

/// <summary>
/// Routed event args carrying the clicked loop's transaction and the click
/// point in canvas coordinates.
/// </summary>
/// <param name="RoutedEvent">The routed event.</param>
/// <param name="Loop">The clicked loop transaction.</param>
/// <param name="Point">The click point in canvas coordinates.</param>
public sealed class LoopArrowRoutedEventArgs(RoutedEvent routedEvent, LoopTransactionViewModel loop, Point point)
    : RoutedEventArgs(routedEvent)
{
    /// <summary>The clicked loop transaction.</summary>
    public LoopTransactionViewModel Loop { get; } = loop;

    /// <summary>The click point in canvas coordinates.</summary>
    public Point Point { get; } = point;
}
