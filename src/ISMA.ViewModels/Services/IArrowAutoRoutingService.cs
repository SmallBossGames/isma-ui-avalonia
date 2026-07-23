using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Services;

/// <summary>
/// Service for auto-routing arrows to avoid state overlaps.
/// </parameter>
public interface IArrowAutoRoutingService
{
    /// <summary>
    /// Calculates the optimal arrow control point to avoid state overlaps.
    /// </summary>
    /// <param name="startState">The start state.</param>
    /// <param name="endState">The end state.</param>
    /// <param name="existingTransitions">Existing transitions to consider.</param>
    /// <returns>The control point for the arrow curve.</returns>
    Avalonia.Point CalculateControlPoint(
        BlueprintStateViewModel startState,
        BlueprintStateViewModel endState,
        IEnumerable<BlueprintTransitionViewModel> existingTransitions);
}

/// <summary>
/// Default implementation of <see cref="IArrowAutoRoutingService"/>.
/// </summary>
public class ArrowAutoRoutingService : IArrowAutoRoutingService
{
    private const int DefaultOffset = 30;

    /// <inheritdoc />
    public Avalonia.Point CalculateControlPoint(
        BlueprintStateViewModel startState,
        BlueprintStateViewModel endState,
        IEnumerable<BlueprintTransitionViewModel> existingTransitions)
    {
        var startCenter = GetStateCenter(startState);
        var endCenter = GetStateCenter(endState);

        var dx = endCenter.X - startCenter.X;
        var dy = endCenter.Y - startCenter.Y;

        var distance = Math.Sqrt(dx * dx + dy * dy);
        var offset = Math.Min(DefaultOffset, distance / 4);

        if (Math.Abs(dx) < 10)
        {
            return new Avalonia.Point(startCenter.X + offset, startCenter.Y + dy / 2);
        }

        if (Math.Abs(dy) < 10)
        {
            return new Avalonia.Point(startCenter.X + dx / 2, startCenter.Y + offset);
        }

        var midX = startCenter.X + dx / 2;
        var midY = startCenter.Y + dy / 2;
        var angle = Math.Atan2(dy, dx);
        var perpX = -Math.Sin(angle) * offset;
        var perpY = Math.Cos(angle) * offset;

        return new Avalonia.Point(midX + perpX, midY + perpY);
    }

    private Avalonia.Point GetStateCenter(BlueprintStateViewModel state)
    {
        return new Avalonia.Point(
            state.CanvasPositionX + 55,
            state.CanvasPositionY + state.StateHeight / 2);
    }
}
