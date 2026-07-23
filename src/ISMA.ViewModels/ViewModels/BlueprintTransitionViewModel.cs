using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

/// <summary>
/// View-model for a blueprint transition (edge between two states).
/// Uses stable <see cref="Guid"/> identity and indirect state references
/// to decouple transitions from direct <see cref="BlueprintStateViewModel"/> references.
/// </summary>
public partial class BlueprintTransitionViewModel : ObservableObject
{
    /// <summary>
    /// Stable identity — set once at construction.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Guid reference to the start state of this transition.
    /// </summary>
    public Guid StartStateId { get; set; }

    /// <summary>
    /// Guid reference to the end state of this transition.
    /// </summary>
    public Guid EndStateId { get; set; }

    /// <summary>
    /// Creates a new transition with a generated identity.
    /// </summary>
    public BlueprintTransitionViewModel()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Creates a new transition with the specified identity.
    /// </summary>
    /// <param name="id">The stable identity for this transition.</param>
    public BlueprintTransitionViewModel(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Predicate expression for this transition.
    /// </summary>
    [ObservableProperty]
    private string _predicate = "";

    /// <summary>
    /// Optional alias for this transition.
    /// </summary>
    [ObservableProperty]
    private string _alias = "";

    /// <summary>
    /// Whether this transition is currently selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Callback invoked when the predicate changes.
    /// </summary>
    public Action<string?>? PredicateChangedCallback { get; set; }

    partial void OnPredicateChanged(string value)
    {
        PredicateChangedCallback?.Invoke(value);
    }

    /// <summary>
    /// States collection for resolving state references. Set by the parent ViewModel.
    /// </summary>
    internal IEnumerable<BlueprintStateViewModel>? _states;

    /// <summary>
    /// Resolves the start state from a collection by matching <see cref="StartStateId"/>.
    /// </summary>
    public BlueprintStateViewModel? GetStartState(IEnumerable<BlueprintStateViewModel> states)
    {
        return states.FirstOrDefault(s => s.Id == StartStateId);
    }

    /// <summary>
    /// Resolves the end state from a collection by matching <see cref="EndStateId"/>.
    /// </summary>
    public BlueprintStateViewModel? GetEndState(IEnumerable<BlueprintStateViewModel> states)
    {
        return states.FirstOrDefault(s => s.Id == EndStateId);
    }

    /// <summary>
    /// Gets the start state view model for XAML bindings.
    /// </summary>
    public BlueprintStateViewModel? StartState => GetStartState(_states ?? []);

    /// <summary>
    /// Gets the end state view model for XAML bindings.
    /// </summary>
    public BlueprintStateViewModel? EndState => GetEndState(_states ?? []);

    /// <summary>
    /// Gets the canvas position of the start state center for arrow rendering.
    /// </summary>
    public Avalonia.Point? StartStatePosition
    {
        get
        {
            var start = StartState;
            if (start == null) return null;
            var width = 110.0;
            var height = start.StateHeight > 0 ? start.StateHeight : width;
            return new Avalonia.Point(start.CanvasPositionX + width / 2, start.CanvasPositionY + height / 2);
        }
    }

    /// <summary>
    /// Gets the canvas position of the end state for arrow rendering.
    /// </summary>
    public Avalonia.Point? EndStatePosition
    {
        get
        {
            var end = EndState;
            if (end == null) return null;
            var width = 110.0;
            var height = end.StateHeight > 0 ? end.StateHeight : width;
            return new Avalonia.Point(end.CanvasPositionX + width / 2, end.CanvasPositionY + height / 2);
        }
    }
}
