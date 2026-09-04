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
/// A directed arrow between two states, custom-drawn in absolute canvas
/// coordinates (the control is pinned at 0,0 on the canvas). Ported from the
/// original Kotlin/JavaFX <c>TransactionArrow</c>: a line offset perpendicularly
/// from the state centers, a rotated arrowhead at the line midpoint, and a
/// label offset from the midpoint. The arrowhead and the line body are
/// separately hit-testable.
/// </summary>
public class TransactionArrow : Control, ICustomHitTest
{
    private const double ArrowheadHitTolerance = 8.0;
    private const double BodyHitTolerance = 6.0;

    private static readonly Pen LinePen = new(BlueprintEditorConstants.ArrowColor, BlueprintEditorConstants.ArrowLineStroke);
    private static readonly Pen ArrowheadPen = new(BlueprintEditorConstants.ArrowColor, BlueprintEditorConstants.ArrowheadStroke);
    private static readonly Typeface LabelTypeface = new("Arial");

    private TransactionViewModel? _transaction;
    private StateViewModel? _startState;
    private StateViewModel? _endState;
    private ArrowGeometry? _geometry;

    /// <summary>Identifies the clicked arrow and the click point (canvas coordinates).</summary>
    public TransactionArrow()
    {
    }

    /// <summary>The transaction this arrow renders.</summary>
    public static readonly StyledProperty<TransactionViewModel?> TransactionProperty =
        AvaloniaProperty.Register<TransactionArrow, TransactionViewModel?>(nameof(Transaction));

    /// <summary>The transaction this arrow renders.</summary>
    public TransactionViewModel? Transaction
    {
        get => GetValue(TransactionProperty);
        set => SetValue(TransactionProperty, value);
    }

    /// <summary>The source state of the arrow.</summary>
    public static readonly StyledProperty<StateViewModel?> StartStateProperty =
        AvaloniaProperty.Register<TransactionArrow, StateViewModel?>(nameof(StartState));

    /// <summary>The source state of the arrow.</summary>
    public StateViewModel? StartState
    {
        get => GetValue(StartStateProperty);
        set => SetValue(StartStateProperty, value);
    }

    /// <summary>The target state of the arrow.</summary>
    public static readonly StyledProperty<StateViewModel?> EndStateProperty =
        AvaloniaProperty.Register<TransactionArrow, StateViewModel?>(nameof(EndState));

    /// <summary>The target state of the arrow.</summary>
    public StateViewModel? EndState
    {
        get => GetValue(EndStateProperty);
        set => SetValue(EndStateProperty, value);
    }

    /// <summary>Raised when the arrowhead is clicked.</summary>
    public static readonly RoutedEvent<TransactionArrowRoutedEventArgs> ArrowHeadClickedEvent =
        RoutedEvent<TransactionArrowRoutedEventArgs>.Register<TransactionArrow, TransactionArrowRoutedEventArgs>(nameof(ArrowHeadClicked), RoutingStrategies.Bubble);

    /// <summary>Raised when the arrowhead is clicked.</summary>
    public event EventHandler<TransactionArrowRoutedEventArgs> ArrowHeadClicked
    {
        add => AddHandler(ArrowHeadClickedEvent, value);
        remove => RemoveHandler(ArrowHeadClickedEvent, value);
    }

    /// <summary>Raised when the arrow body (line) is clicked.</summary>
    public static readonly RoutedEvent<TransactionArrowRoutedEventArgs> BodyClickedEvent =
        RoutedEvent<TransactionArrowRoutedEventArgs>.Register<TransactionArrow, TransactionArrowRoutedEventArgs>(nameof(BodyClicked), RoutingStrategies.Bubble);

    /// <summary>Raised when the arrow body (line) is clicked.</summary>
    public event EventHandler<TransactionArrowRoutedEventArgs> BodyClicked
    {
        add => AddHandler(BodyClickedEvent, value);
        remove => RemoveHandler(BodyClickedEvent, value);
    }

    static TransactionArrow()
    {
        TransactionProperty.Changed.AddClassHandler<TransactionArrow, TransactionViewModel?>(
            (arrow, _) => arrow.OnTransactionChanged());
        StartStateProperty.Changed.AddClassHandler<TransactionArrow, StateViewModel?>(
            (arrow, _) => arrow.OnStateChanged());
        EndStateProperty.Changed.AddClassHandler<TransactionArrow, StateViewModel?>(
            (arrow, _) => arrow.OnStateChanged());
    }

    private void OnTransactionChanged()
    {
        if (_transaction is { } old)
        {
            old.PropertyChanged -= OnTransactionPropertyChanged;
        }

        _transaction = Transaction;

        if (_transaction is not { } tx)
        {
            return;
        }

        tx.PropertyChanged += OnTransactionPropertyChanged;
        InvalidateVisual();
    }

    private void OnTransactionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnStateChanged()
    {
        if (_startState is { } oldStart)
        {
            oldStart.PropertyChanged -= OnStateGeometryChanged;
        }

        if (_endState is { } oldEnd)
        {
            oldEnd.PropertyChanged -= OnStateGeometryChanged;
        }

        _startState = StartState;
        _endState = EndState;

        if (_startState is { } start)
        {
            start.PropertyChanged += OnStateGeometryChanged;
        }

        if (_endState is { } end)
        {
            end.PropertyChanged += OnStateGeometryChanged;
        }

        Recompute();
    }

    private void OnStateGeometryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StateViewModel.X) or nameof(StateViewModel.Y)
            or nameof(StateViewModel.SquareWidth) or nameof(StateViewModel.SquareHeight))
        {
            Recompute();
        }
    }

    private void Recompute()
    {
        if (StartState is { } start && EndState is { } end)
        {
            _geometry = ArrowGeometryCalculator.Calculate(
                start.CenterX,
                start.CenterY,
                end.CenterX,
                end.CenterY,
                layoutX: 0,
                layoutY: 0,
                lineOffset: BlueprintEditorConstants.ArrowLineOffset,
                textXOffset: BlueprintEditorConstants.ArrowTextXOffset,
                textYOffset: BlueprintEditorConstants.ArrowTextYOffset);
        }
        else
        {
            _geometry = null;
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>
    /// The arrowhead center in canvas coordinates (midpoint of the state centers
    /// plus the perpendicular line offset).
    /// </summary>
    private Point ArrowheadCenter
    {
        get
        {
            if (_geometry is not { } geometry || StartState is not { } start || EndState is not { } end)
            {
                return default(Point);
            }

            double midX = (start.CenterX + end.CenterX) / 2;
            double midY = (start.CenterY + end.CenterY) / 2;
            return new Point(midX + geometry.ArrowheadTranslateX, midY + geometry.ArrowheadTranslateY);
        }
    }

    /// <summary>The label center in canvas coordinates.</summary>
    private Point LabelCenter
    {
        get
        {
            if (_geometry is not { } geometry || StartState is not { } start || EndState is not { } end)
            {
                return default(Point);
            }

            double midX = (start.CenterX + end.CenterX) / 2;
            double midY = (start.CenterY + end.CenterY) / 2;
            return new Point(
                midX + geometry.LabelTextTranslateX,
                midY + geometry.LabelTextTranslateY + Constants.BlueprintEditorConstants.LoopLabelYOffset);
        }
    }

    /// <summary>The arrowhead polygon vertices (rotated, translated), in canvas coordinates.</summary>
    private Point[] ArrowheadVertices
    {
        get
        {
            if (_geometry is not { } geometry)
            {
                return Array.Empty<Point>();
            }

            var center = ArrowheadCenter;
            var local = new[]
            {
                new Point(BlueprintEditorConstants.ArrowheadWidth, -BlueprintEditorConstants.ArrowheadWidth),
                new Point(-BlueprintEditorConstants.ArrowheadWidth, 0),
                new Point(BlueprintEditorConstants.ArrowheadWidth, BlueprintEditorConstants.ArrowheadWidth),
            };

            return local.Select(p => Rotate(p, geometry.ArrowheadRotation) + center).ToArray();
        }
    }

    private static Point Rotate(Point p, double degrees)
    {
        double rad = degrees * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        return new Point(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_geometry is not { } geometry)
        {
            return default(Size);
        }

        double maxX = Math.Max(geometry.LineStartX, geometry.LineEndX);
        double maxY = Math.Max(geometry.LineStartY, geometry.LineEndY);

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
        if (_geometry is not { } geometry)
        {
            return;
        }

        var lineStart = new Point(geometry.LineStartX, geometry.LineStartY);
        var lineEnd = new Point(geometry.LineEndX, geometry.LineEndY);
        context.DrawLine(LinePen, lineStart, lineEnd);

        var vertices = ArrowheadVertices;
        if (vertices.Length == 3)
        {
            var stream = new StreamGeometry();
            using (var ctx = stream.Open())
            {
                ctx.BeginFigure(vertices[0], isFilled: true);
                ctx.LineTo(vertices[1], isStroked: true);
                ctx.LineTo(vertices[2], isStroked: true);
                ctx.EndFigure(isClosed: true);
            }

            context.DrawGeometry(BlueprintEditorConstants.ArrowColor, ArrowheadPen, stream);
        }

        if (Transaction is { } tx)
        {
            var text = tx.DisplayText;
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
        if (_geometry is null)
        {
            return false;
        }

        var vertices = ArrowheadVertices;
        if (vertices.Length == 3 && IsNearPolygon(point, vertices, ArrowheadHitTolerance))
        {
            return true;
        }

        var lineStart = new Point(_geometry.LineStartX, _geometry.LineStartY);
        var lineEnd = new Point(_geometry.LineEndX, _geometry.LineEndY);
        return DistanceToSegment(point, lineStart, lineEnd) < BodyHitTolerance;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Transaction is not { } tx || _geometry is null)
        {
            return;
        }

        var point = e.GetPosition(this);
        var vertices = ArrowheadVertices;
        if (vertices.Length == 3 && IsNearPolygon(point, vertices, ArrowheadHitTolerance))
        {
            RaiseEvent(new TransactionArrowRoutedEventArgs(ArrowHeadClickedEvent, tx, point));
            e.Handled = true;
            return;
        }

        var lineStart = new Point(_geometry.LineStartX, _geometry.LineStartY);
        var lineEnd = new Point(_geometry.LineEndX, _geometry.LineEndY);
        if (DistanceToSegment(point, lineStart, lineEnd) < BodyHitTolerance)
        {
            RaiseEvent(new TransactionArrowRoutedEventArgs(BodyClickedEvent, tx, point));
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
/// Routed event args carrying the clicked arrow's transaction and the click
/// point in canvas coordinates.
/// </summary>
/// <param name="RoutedEvent">The routed event.</param>
/// <param name="Transaction">The clicked transaction.</param>
/// <param name="Point">The click point in canvas coordinates.</param>
public sealed class TransactionArrowRoutedEventArgs(RoutedEvent routedEvent, TransactionViewModel transaction, Point point)
    : RoutedEventArgs(routedEvent)
{
    /// <summary>The clicked transaction.</summary>
    public TransactionViewModel Transaction { get; } = transaction;

    /// <summary>The click point in canvas coordinates.</summary>
    public Point Point { get; } = point;
}
