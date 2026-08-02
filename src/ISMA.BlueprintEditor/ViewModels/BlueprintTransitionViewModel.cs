using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// View model for a transition between two states.
/// Uses stable Guid identity and indirect state references to decouple from direct state object references.
/// Subscribes to state PropertyChanged events to refresh positions when states move.
/// </summary>
public partial class BlueprintTransitionViewModel : ObservableObject
{
    private readonly List<BlueprintStateViewModel> _allStates;
    private Guid _startStateId;
    private Guid _endStateId;
    private string _predicate = "";
    private string _alias = "";
    private bool _isSelected;
    private (double X, double Y)? _startStatePosition;
    private (double X, double Y)? _endStatePosition;

    /// <summary>
    /// Stable identity of this transition.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Source state identifier.
    /// </summary>
    public Guid StartStateId
    {
        get => _startStateId;
        set
        {
            if (SetProperty(ref _startStateId, value))
            {
                RefreshPositions();
            }
        }
    }

    /// <summary>
    /// Destination state identifier.
    /// </summary>
    public Guid EndStateId
    {
        get => _endStateId;
        set
        {
            if (SetProperty(ref _endStateId, value))
            {
                RefreshPositions();
            }
        }
    }

    /// <summary>
    /// Guard condition expression.
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
    /// Whether this transition is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Canvas position of the start state center (cached for arrow rendering).
    /// </summary>
    public (double X, double Y)? StartStatePosition => _startStatePosition;

    /// <summary>
    /// Canvas position of the end state center (cached for arrow rendering).
    /// </summary>
    public (double X, double Y)? EndStatePosition => _endStatePosition;

    /// <summary>
    /// Creates a new transition view model.
    /// </summary>
    /// <param name="id">Unique identifier for this transition.</param>
    /// <param name="allStates">All available states for position resolution.</param>
    public BlueprintTransitionViewModel(Guid id, IEnumerable<BlueprintStateViewModel> allStates)
    {
        Id = id;
        _allStates = allStates.ToList();
        RefreshPositions();
        SubscribeToStateChanges();
    }

    /// <summary>
    /// Refreshes the cached state positions.
    /// </summary>
    public void RefreshPositions()
    {
        var start = _allStates.FirstOrDefault(s => s.Id == _startStateId);
        var end = _allStates.FirstOrDefault(s => s.Id == _endStateId);

        var newStart = start != null
            ? (start.CanvasPositionX + 55, start.CanvasPositionY + start.StateHeight / 2)
            : (null as (double X, double Y)?);

        var newEnd = end != null
            ? (end.CanvasPositionX + 55, end.CanvasPositionY + end.StateHeight / 2)
            : (null as (double X, double Y)?);

        if (newStart != _startStatePosition)
        {
            _startStatePosition = newStart;
            OnPropertyChanged(nameof(StartStatePosition));
        }

        if (newEnd != _endStatePosition)
        {
            _endStatePosition = newEnd;
            OnPropertyChanged(nameof(EndStatePosition));
        }
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
