namespace ISMA.ViewModels.ViewModels;

/// <summary>
/// Interface for objects that can snap their position to a grid.
/// </summary>
public interface ISnapPosition
{
    double CanvasPositionX { get; set; }
    double CanvasPositionY { get; set; }
    void SnapPositionX(double gridSize);
    void SnapPositionY(double gridSize);
}
