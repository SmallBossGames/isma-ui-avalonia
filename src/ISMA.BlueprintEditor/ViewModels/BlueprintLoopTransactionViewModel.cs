using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// View model for a self-referencing loop transition on a state.
/// Uses stable Guid identity and indirect state references.
/// Subscribes to state PropertyChanged events to refresh positions when states move.
/// </summary>
public partial class BlueprintLoopTransactionViewModel : ObservableObject
{
    private readonly List<BlueprintStateViewModel> _allStates;
    private Guid _stateId;
    private string _predicate = "";
    private string _alias = "";
    private string _text = "";
    private bool _isSelected;
    private (double X, double Y)? _statePosition;

    /// <summary>
    /// Stable identity of this loop.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The state this loop belongs to.
    /// </summary>
    public Guid StateId
    {
        get => _stateId;
        set
        {
            if (SetProperty(ref _stateId, value))
            {
                RefreshPositions();
            }
        }
    }

    /// <summary>
    /// Loop guard condition.
    /// </summary>
    public string Predicate
    {
        get => _predicate;
        set => SetProperty(ref _predicate, value);
    }

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string Alias
    {
        get => _alias;
        set => SetProperty(ref _alias, value);
    }

    /// <summary>
    /// Loop body text.
    /// </summary>
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    /// <summary>
    /// Whether this loop is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Canvas position of the associated state center (cached for arrow rendering).
    /// </summary>
    public (double X, double Y)? StatePosition => _statePosition;

    /// <summary>
    /// Creates a new loop transaction view model.
    /// </summary>
    /// <param name="id">Unique identifier for this loop.</param>
    /// <param name="allStates">All available states for position resolution.</param>
    public BlueprintLoopTransactionViewModel(Guid id, IEnumerable<BlueprintStateViewModel> allStates)
    {
        Id = id;
        _allStates = allStates.ToList();
        RefreshPositions();
        SubscribeToStateChanges();
    }

    /// <summary>
    /// Refreshes the cached state position.
    /// </summary>
    public void RefreshPositions()
    {
        var state = _allStates.FirstOrDefault(s => s.Id == _stateId);
        var newStatePos = state != null
            ? (state.CanvasPositionX + 55, state.CanvasPositionY + state.StateHeight / 2)
            : (null as (double X, double Y)?);

        if (newStatePos != _statePosition)
        {
            _statePosition = newStatePos;
            OnPropertyChanged(nameof(StatePosition));
        }
    }

    /// <summary>
    /// Gets the state this loop belongs to.
    /// </summary>
    /// <param name="states">The collection of states to search.</param>
    /// <returns>The state, or null if not found.</returns>
    public BlueprintStateViewModel? GetState(IEnumerable<BlueprintStateViewModel> states)
    {
        return states.FirstOrDefault(s => s.Id == _stateId);
    }

    /// <summary>
    /// Unsubscribes from state property changed events to prevent memory leaks.
    /// </summary>
    public void UnsubscribeFromState()
    {
        foreach (var state in _allStates)
        {
            state.PropertyChanged -= OnStatePropertyChanged;
        }
    }

    private void SubscribeToStateChanges()
    {
        foreach (var state in _allStates)
        {
            state.PropertyChanged += OnStatePropertyChanged;
        }
    }

    private void OnStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlueprintStateViewModel.CanvasPositionX) ||
            e.PropertyName == nameof(BlueprintStateViewModel.CanvasPositionY) ||
            e.PropertyName == nameof(BlueprintStateViewModel.StateHeight))
        {
            RefreshPositions();
        }
    }
}
