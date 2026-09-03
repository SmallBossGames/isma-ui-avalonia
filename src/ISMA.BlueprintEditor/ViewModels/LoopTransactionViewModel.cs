using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// A self-transition (loop) on a state, with its own LISMA body.
/// </summary>
public partial class LoopTransactionViewModel : ObservableObject
{
    public LoopTransactionViewModel(string stateName, string predicate, string alias, string text)
    {
        StateName = stateName;
        Predicate = predicate;
        Alias = alias;
        Text = text;
    }

    [ObservableProperty]
    private string stateName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string predicate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string alias;

    [ObservableProperty]
    private string text;

    /// <summary>Resolved live reference to the state the loop is attached to (set by CanvasViewModel).</summary>
    public StateViewModel? State { get; set; }

    /// <summary>Label shown on the loop: alias if non-blank, else predicate.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Alias) ? Predicate : Alias;
}
