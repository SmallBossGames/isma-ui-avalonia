namespace ISMA.BlueprintEditor.Utilities;

public record ArrowGeometry(
    double LineStartX,
    double LineStartY,
    double LineEndX,
    double LineEndY,
    double ArrowheadTranslateX,
    double ArrowheadTranslateY,
    double ArrowheadRotation,
    double LabelTextTranslateX,
    double LabelTextTranslateY
);

public static class ArrowGeometryExtensions
{
    public static ArrowGeometry CalculateArrowGeometry(
        double startX,
        double startY,
        double endX,
        double endY,
        double layoutOffset,
        double textXOffset,
        double textYOffset)
    {
        // Center point
        var midX = (startX + endX) / 2.0;
        var midY = (startY + endY) / 2.0;

        // Direction from end to start
        var dx = startX - endX;
        var dy = startY - endY;

        // Angle (Y-down coordinate system)
        var angle = Math.Atan2(dx, dy) + Math.PI / 2;

        // Perpendicular offset
        var offsetX = layoutOffset * Math.Sin(angle);
        var offsetY = layoutOffset * Math.Cos(angle);

        // Line endpoints (center ± offset)
        var lineStartX = midX + offsetX;
        var lineStartY = midY + offsetY;
        var lineEndX = midX - offsetX;
        var lineEndY = midY - offsetY;

        // Arrowhead position (at line end, shifted by offset)
        var arrowheadTranslateX = lineEndX + offsetX;
        var arrowheadTranslateY = lineEndY + offsetY;

        // Arrowhead rotation (direction of line)
        var arrowheadRotation = Math.Atan2(dy, -dx) * 180.0 / Math.PI;

        // Label position (center + text offsets rotated by angle)
        var labelTextTranslateX = midX + textXOffset * Math.Cos(angle);
        var labelTextTranslateY = midY + textYOffset * Math.Sin(angle);

        return new ArrowGeometry(
            lineStartX,
            lineStartY,
            lineEndX,
            lineEndY,
            arrowheadTranslateX,
            arrowheadTranslateY,
            arrowheadRotation,
            labelTextTranslateX,
            labelTextTranslateY
        );
    }
}
