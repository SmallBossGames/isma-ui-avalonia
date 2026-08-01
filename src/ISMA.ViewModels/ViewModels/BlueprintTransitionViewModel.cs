using System.Collections.Generic;
using System.ComponentModel;
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
    /// <param name="states">The collection of available blueprint states for resolving state references.</param>
    public BlueprintTransitionViewModel(IEnumerable<BlueprintStateViewModel> states)
    {
        Id = Guid.NewGuid();
        _states = states;
        InitializeSubscriptions();
    }

    /// <summary>
    /// Creates a new transition with the specified identity.
    /// </summary>
    /// <param name="id">The stable identity for this transition.</param>
    /// <param name="states">The collection of available blueprint states for resolving state references.</param>
    public BlueprintTransitionViewModel(Guid id, IEnumerable<BlueprintStateViewModel> states)
    {
        Id = id;
        _states = states;
        InitializeSubscriptions();
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
    /// States collection for resolving state references. Set at construction time.
    /// </summary>
    private readonly IEnumerable<BlueprintStateViewModel> _states;

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

    private Avalonia.Point? _startStatePosition;
    private Avalonia.Point? _endStatePosition;
    private BlueprintStateViewModel? _previousStartState;
    private BlueprintStateViewModel? _previousEndState;

    /// <summary>
    /// Gets the start state view model for XAML bindings.
    /// </summary>
    public BlueprintStateViewModel? StartState => GetStartState(_states);

    /// <summary>
    /// Gets the end state view model for XAML bindings.
    /// </summary>
    public BlueprintStateViewModel? EndState => GetEndState(_states);

    /// <summary>
    /// Gets the canvas position of the start state center for arrow rendering.
    /// </summary>
    public Avalonia.Point? StartStatePosition
    {
        get => _startStatePosition;
        private set => SetProperty(ref _startStatePosition, value);
    }

    /// <summary>
    /// Gets the canvas position of the end state for arrow rendering.
    /// </summary>
    public Avalonia.Point? EndStatePosition
    {
        get => _endStatePosition;
        private set => SetProperty(ref _endStatePosition, value);
    }

    /// <summary>
    /// Refreshes the cached position values and notifies property changes.
    /// Call this when state positions may have changed.
    /// </summary>
    public void RefreshPositions()
    {
        var start = StartState;
        if (start != null)
        {
            var width = 110.0;
            var height = start.StateHeight > 0 ? start.StateHeight : width;
            _startStatePosition = new Avalonia.Point(start.CanvasPositionX + width / 2, start.CanvasPositionY + height / 2);
        }
        else
        {
            _startStatePosition = null;
        }

        var end = EndState;
        if (end != null)
        {
            var width = 110.0;
            var height = end.StateHeight > 0 ? end.StateHeight : width;
            _endStatePosition = new Avalonia.Point(end.CanvasPositionX + width / 2, end.CanvasPositionY + height / 2);
        }
        else
        {
            _endStatePosition = null;
        }

        OnPropertyChanged();
        OnPropertyChanged(nameof(StartStatePosition));
        OnPropertyChanged(nameof(EndStatePosition));
    }

    private void InitializeSubscriptions()
    {
        var start = StartState;
        var end = EndState;
        if (start != null)
        {
            _previousStartState = start;
            SubscribeToState(start);
        }
        if (end != null)
        {
            _previousEndState = end;
            SubscribeToState(end);
        }
        RefreshPositions();
    }

    private void SubscribeToState(BlueprintStateViewModel? state)
    {
        if (state != null)
        {
            state.PropertyChanged += OnStatePropertyChanged;
        }
    }

    private void UnsubscribeFromState(BlueprintStateViewModel? state)
    {
        if (state != null)
        {
            state.PropertyChanged -= OnStatePropertyChanged;
        }
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlueprintStateViewModel.CanvasPositionX) ||
            e.PropertyName == nameof(BlueprintStateViewModel.CanvasPositionY) ||
            e.PropertyName == nameof(BlueprintStateViewModel.StateHeight))
        {
            RefreshPositions();
        }
    }
}
