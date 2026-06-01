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
/// </summary>
public class ArrowLine : Control
{
    private const double ArrowOffset = 10.0;
    private const double ArrowheadSize = 14.0;
    private const double StateWidth = 110;
    private const double StateHeight = 65;

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

    static ArrowLine()
    {
        AffectsRender<ArrowLine>(StartStateProperty, EndStateProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (StartState == null || EndState == null) return;

        var start = GetCenter(StartState);
        var end = GetCenter(EndState);

        var direction = end - start;
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length < 1.0) return;

        var unitDir = new Vector(direction.X / length, direction.Y / length);
        var startOffset = start + unitDir * ArrowOffset;
        var endOffset = end - unitDir * (length - ArrowOffset);

        // Draw line
        context.DrawLine(new Pen(Avalonia.Media.Brushes.Black, 1.5), startOffset, endOffset);

        // Draw arrowhead
        var arrowheadAngle = Math.Atan2(direction.Y, direction.X);
        var arrowheadCenter = endOffset;
        DrawArrowhead(context, arrowheadCenter, arrowheadAngle);

        // Draw label
        var midX = (startOffset.X + endOffset.X) / 2;
        var midY = (startOffset.Y + endOffset.Y) / 2;

        var displayText = !string.IsNullOrEmpty(Alias) ? Alias : Predicate;
        if (!string.IsNullOrEmpty(displayText))
        {
            var formattedText = new FormattedText(displayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 11, Avalonia.Media.Brushes.Black);
            context.DrawText(formattedText, new Point(midX - formattedText.Width / 2, midY - formattedText.Height / 2));
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
        var height = state.StateHeight > 0 ? state.StateHeight : StateHeight;
        return new Point(state.CanvasPositionX + StateWidth / 2, state.CanvasPositionY + height / 2);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (EndState == null) return;

        var position = e.GetPosition(this);
        var end = GetCenter(EndState);

        // Calculate arrowhead position (endOffset)
        var start = GetCenter(StartState);
        var direction = end - start;
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        
        if (length < 1.0) return;

        var unitDir = new Vector(direction.X / length, direction.Y / length);
        var endOffset = end - unitDir * (length - ArrowOffset);

        var distanceToArrowhead = Math.Sqrt(Math.Pow(position.X - endOffset.X, 2) + Math.Pow(position.Y - endOffset.Y, 2));

        var result = new ArrowHitTestResult();
        
        if (distanceToArrowhead < ArrowheadSize)
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
