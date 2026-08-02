using System;
using Avalonia;

namespace ISMA.BlueprintEditor.Utilities;

/// <summary>
/// Pure math functions for arrow geometry calculations.
/// Computes arrow line endpoints, arrowhead position/rotation, and label offset.
/// </summary>
public static class ArrowGeometry
{
    private const double ArrowOffset = 10.0;
    private const double ArrowheadSize = 7.0;
    private const double StrokeWidth = 3.0;
    private const double LabelFontSize = 16;
    private const double TextOffsetX = 75.0;
    private const double TextOffsetY = 50.0;

    /// <summary>
    /// Calculates the perpendicular offset for an arrow line between two states.
    /// Uses atan2-based perpendicular angle per spec.
    /// </summary>
    /// <param name="startX">Start state center X.</param>
    /// <param name="startY">Start state center Y.</param>
    /// <param name="endX">End state center X.</param>
    /// <param name="endY">End state center Y.</param>
    /// <returns>Tuple of (startOffsetX, startOffsetY, endOffsetX, endOffsetY, lineAngle, labelOffsetX, labelOffsetY).</returns>
    public static (double StartOffsetX, double StartOffsetY, double EndOffsetX, double EndOffsetY, double LineAngle, double LabelOffsetX, double LabelOffsetY) CalculateArrowLine(
        double startX, double startY, double endX, double endY)
    {
        var dx = endX - startX;
        var dy = endY - startY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1.0)
            return (startX, startY, endX, endY, 0, 0, 0);

        var angle = Math.Atan2(dx, dy) + Math.PI / 2;
        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);

        var offsetX = ArrowOffset * sin;
        var offsetY = ArrowOffset * cos;

        var startOffsetX = startX + offsetX;
        var startOffsetY = startY + offsetY;
        var endOffsetX = endX + offsetX;
        var endOffsetY = endY + offsetY;

        var lineAngle = Math.Atan2(dy, dx);

        var labelOffsetX = TextOffsetX * sin;
        var labelOffsetY = TextOffsetY * cos;

        return (startOffsetX, startOffsetY, endOffsetX, endOffsetY, lineAngle, labelOffsetX, labelOffsetY);
    }

    /// <summary>
    /// Calculates the arrowhead vertices rotated to match the line direction.
    /// </summary>
    /// <param name="center">Arrowhead tip center point.</param>
    /// <param name="angle">Line angle in radians.</param>
    /// <returns>Array of 3 points forming the arrowhead triangle.</returns>
    public static (Point Tip, Point Base1, Point Base2) CalculateArrowhead(Point center, double angle)
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

        return (tip, base1, base2);
    }

    /// <summary>
    /// Calculates the label position at perpendicular offset from the line midpoint.
    /// </summary>
    /// <param name="startOffsetX">Offset start X.</param>
    /// <param name="startOffsetY">Offset start Y.</param>
    /// <param name="endOffsetX">Offset end X.</param>
    /// <param name="endOffsetY">Offset end Y.</param>
    /// <param name="labelOffsetX">Perpendicular X offset.</param>
    /// <param name="labelOffsetY">Perpendicular Y offset.</param>
    /// <returns>Label position point.</returns>
    public static Point CalculateLabelPosition(double startOffsetX, double startOffsetY, double endOffsetX, double endOffsetY, double labelOffsetX, double labelOffsetY)
    {
        var midX = (startOffsetX + endOffsetX) / 2;
        var midY = (startOffsetY + endOffsetY) / 2;
        return new Point(midX + labelOffsetX - 37.5, midY + labelOffsetY - 25);
    }

    /// <summary>
    /// Calculates the loop arrow circle center position relative to state center.
    /// Circle offset: (60, -40) from state center per spec.
    /// </summary>
    /// <param name="stateCenterX">State center X.</param>
    /// <param name="stateCenterY">State center Y.</param>
    /// <param name="radius">Loop radius (default 40).</param>
    /// <returns>Circle center point.</returns>
    public static Point CalculateLoopCircleCenter(double stateCenterX, double stateCenterY, double radius = 40.0)
    {
        return new Point(stateCenterX + 60.0, stateCenterY - radius);
    }

    /// <summary>
    /// Calculates the loop arrowhead position.
    /// Arrowhead at (100, 0) relative to circle center, pointing right.
    /// </summary>
    /// <param name="circleCenter">Circle center point.</param>
    /// <returns>Arrowhead position point.</returns>
    public static Point CalculateLoopArrowheadPosition(Point circleCenter)
    {
        return new Point(circleCenter.X + 100.0, circleCenter.Y - 10.0);
    }

    /// <summary>
    /// Calculates the loop label position.
    /// Label at (120, -10) relative to circle center.
    /// </summary>
    /// <param name="circleCenter">Circle center point.</param>
    /// <returns>Label position point.</returns>
    public static Point CalculateLoopLabelPosition(Point circleCenter)
    {
        return new Point(circleCenter.X + 120.0, circleCenter.Y - 10.0);
    }
}
