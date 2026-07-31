using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

/// <summary>
/// Represents a loop transaction in a blueprint with stable identity and state reference.
/// </summary>
public partial class BlueprintLoopTransactionViewModel : ObservableObject
{
    /// <summary>
    /// Stable identity for this loop transaction.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Creates a new loop transaction with a generated identity.
    /// </summary>
    public BlueprintLoopTransactionViewModel()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Creates a new loop transaction with the specified identity.
    /// </summary>
    /// <param name="id">The stable identity for this loop transaction.</param>
    public BlueprintLoopTransactionViewModel(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Guid-based reference to the associated blueprint state.
    /// </summary>
    public Guid StateId { get; set; }

    [ObservableProperty]
    private string _predicate = "";

    public Action<string?>? PredicateChangedCallback { get; set; }

    partial void OnPredicateChanged(string value)
    {
        PredicateChangedCallback?.Invoke(value);
    }

    [ObservableProperty]
    private string _alias = "";

    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// States collection for resolving state references. Set by the parent ViewModel.
    /// </summary>
    internal IEnumerable<BlueprintStateViewModel>? _states;

    /// <summary>
    /// Resolves the <see cref="BlueprintStateViewModel"/> by <see cref="StateId"/>.
    /// </summary>
    /// <param name="states">The collection of available blueprint states.</param>
    /// <returns>The matching state, or null if not found.</returns>
    public BlueprintStateViewModel? GetState(IEnumerable<BlueprintStateViewModel> states)
    {
        return states.FirstOrDefault(s => s.Id == StateId);
    }

    /// <summary>
    /// Gets the name of the associated state.
    /// </summary>
    public string? StateName => GetState(_states ?? [])?.Name;

    /// <summary>
    /// Gets the canvas X position of the center of the associated state.
    /// </summary>
    public double? StatePositionX => State?.CanvasPositionX + 55.0;

    /// <summary>
    /// Gets the canvas Y position of the center of the associated state.
    /// </summary>
    public double? StatePositionY => State?.CanvasPositionY + (State?.StateHeight > 0 ? State.StateHeight / 2 : 32.5);

    /// <summary>
    /// Gets the canvas center position of the associated state as a single Point.
    /// </summary>
    public Avalonia.Point? StatePosition => State != null
        ? new Avalonia.Point(StatePositionX ?? 0, StatePositionY ?? 0)
        : null;

    /// <summary>
    /// Gets the associated state view model for XAML bindings.
    /// </summary>
    public BlueprintStateViewModel? State => GetState(_states ?? []);

    /// <summary>
    /// Resolves the canvas center position of a state by its GUID.
    /// Used by <see cref="Controls.LoopArrow"/> to position loop arrows on the canvas.
    /// </summary>
    /// <param name="stateId">The GUID of the state to resolve.</param>
    /// <returns>The center point of the state, or null if not found.</returns>
    public Avalonia.Point? ResolveStatePosition(Guid stateId)
    {
        var state = _states?.FirstOrDefault(s => s.Id == stateId);
        if (state == null) return null;

        var width = 110.0;
        var height = state.StateHeight > 0 ? state.StateHeight : width;
        return new Avalonia.Point(state.CanvasPositionX + width / 2, state.CanvasPositionY + height / 2);
    }
}
