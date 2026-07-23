namespace ISMA.ViewModels.Services;

/// <summary>
/// Service for grid snapping of blueprint elements.
/// </summary>
public interface IGridSnappingService
{
    /// <summary>
    /// Gets or sets the grid size in pixels. Default is 20.
    /// </summary>
    double GridSize { get; set; }

    /// <summary>
    /// Whether grid snapping is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Snaps a position to the nearest grid point.
    /// </summary>
    /// <param name="value">The original position value.</param>
    /// <returns>The snapped position value.</returns>
    double Snap(double value);
}

/// <summary>
/// Default implementation of <see cref="IGridSnappingService"/>.
/// </summary>
public class GridSnappingService : IGridSnappingService
{
    /// <inheritdoc />
    public double GridSize { get; set; } = 20;

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public double Snap(double value)
    {
        if (!IsEnabled || GridSize <= 0)
            return value;

        return Math.Round(value / GridSize) * GridSize;
    }
}
