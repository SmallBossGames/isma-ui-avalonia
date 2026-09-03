namespace ISMA.BlueprintEditor.Utilities;

/// <summary>
/// Geometry of a transaction arrow between two states, ported verbatim from the
/// original ISMA Kotlin/JavaFX editor.
/// </summary>
/// <param name="LineStartX">X of the line start (state center + offset).</param>
/// <param name="LineStartY">Y of the line start (state center + offset).</param>
/// <param name="LineEndX">X of the line end (state center + offset).</param>
/// <param name="LineEndY">Y of the line end (state center + offset).</param>
/// <param name="ArrowheadTranslateX">Translation of the arrowhead group.</param>
/// <param name="ArrowheadTranslateY">Translation of the arrowhead group.</param>
/// <param name="ArrowheadRotation">Rotation of the arrowhead in degrees.</param>
/// <param name="LabelTextTranslateX">Translation of the label text.</param>
/// <param name="LabelTextTranslateY">Translation of the label text.</param>
public record ArrowGeometry(
    double LineStartX,
    double LineStartY,
    double LineEndX,
    double LineEndY,
    double ArrowheadTranslateX,
    double ArrowheadTranslateY,
    double ArrowheadRotation,
    double LabelTextTranslateX,
    double LabelTextTranslateY);

/// <summary>Pure arrow-geometry math, ported verbatim from the original editor.</summary>
public static class ArrowGeometryCalculator
{
    /// <summary>
    /// Calculates the arrow geometry between two state centers.
    /// </summary>
    /// <param name="startX">X of the source state center.</param>
    /// <param name="startY">Y of the source state center.</param>
    /// <param name="endX">X of the target state center.</param>
    /// <param name="endY">Y of the target state center.</param>
    /// <param name="layoutX">Layout X of the drawing surface (control offset).</param>
    /// <param name="layoutY">Layout Y of the drawing surface (control offset).</param>
    /// <param name="lineOffset">Offset of the line endpoints from the state centers.</param>
    /// <param name="textXOffset">X offset of the label from the line midpoint.</param>
    /// <param name="textYOffset">Y offset of the label from the line midpoint.</param>
    public static ArrowGeometry Calculate(
        double startX,
        double startY,
        double endX,
        double endY,
        double layoutX,
        double layoutY,
        double lineOffset = 10,
        double textXOffset = 75,
        double textYOffset = 50)
    {
        double dx = endX - startX;
        double dy = endY - startY;
        double angle = Math.Atan2(dx, dy) + Math.PI / 2;

        double offsetX = lineOffset * Math.Sin(angle);
        double offsetY = lineOffset * Math.Cos(angle);

        return new ArrowGeometry(
            startX - layoutX + offsetX,
            startY - layoutY + offsetY,
            endX - layoutX + offsetX,
            endY - layoutY + offsetY,
            offsetX,
            offsetY,
            -angle / Math.PI * 180.0,
            textXOffset * Math.Sin(angle),
            textYOffset * Math.Cos(angle));
    }
}
