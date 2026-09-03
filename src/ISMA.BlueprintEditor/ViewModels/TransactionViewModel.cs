using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// A directed transition between two states, referencing them by name.
/// </summary>
public partial class TransactionViewModel : ObservableObject
{
    public TransactionViewModel(string startStateName, string endStateName, string predicate, string alias)
    {
        StartStateName = startStateName;
        EndStateName = endStateName;
        Predicate = predicate;
        Alias = alias;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string startStateName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string endStateName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string predicate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string alias;

    /// <summary>Resolved live reference to the source state (set by CanvasViewModel).</summary>
    public StateViewModel? StartState { get; set; }

    /// <summary>Resolved live reference to the target state (set by CanvasViewModel).</summary>
    public StateViewModel? EndState { get; set; }

    /// <summary>Label shown on the arrow: alias if non-blank, else predicate.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Alias) ? Predicate : Alias;
}
