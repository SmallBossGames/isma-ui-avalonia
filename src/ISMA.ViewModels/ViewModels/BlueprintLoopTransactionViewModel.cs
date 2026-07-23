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
    /// Gets the associated state view model for XAML bindings.
    /// </summary>
    public BlueprintStateViewModel? State => GetState(_states ?? []);
}
