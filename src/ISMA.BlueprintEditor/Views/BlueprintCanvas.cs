using Avalonia;
using Avalonia.Controls;

namespace ISMA.BlueprintEditor.Views;

/// <summary>
/// A <see cref="Canvas"/> that measures itself to the extent of its children
/// (plus padding), so an enclosing ScrollViewer scrolls to the full content
/// area. Mirrors the original JavaFX Pane behavior of the blueprint canvas.
/// </summary>
public class BlueprintCanvas : Canvas
{
    /// <summary>Padding added around the children extent.</summary>
    public const double Padding = 40;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double maxX = 0;
        double maxY = 0;

        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            // Canvas.Left/Top default to NaN in Avalonia (unlike WPF's 0);
            // unpositioned children sit at the canvas origin.
            double left = GetLeft(child);
            double top = GetTop(child);
            if (double.IsNaN(left))
            {
                left = 0;
            }

            if (double.IsNaN(top))
            {
                top = 0;
            }

            maxX = Math.Max(maxX, left + child.DesiredSize.Width);
            maxY = Math.Max(maxY, top + child.DesiredSize.Height);
        }

        return new Size(maxX + Padding, maxY + Padding);
    }
}
