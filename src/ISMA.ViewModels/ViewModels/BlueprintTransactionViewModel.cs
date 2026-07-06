using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintTransactionViewModel : ObservableObject
{
    [ObservableProperty]
    private BlueprintStateViewModel _startState = new();

    [ObservableProperty]
    private BlueprintStateViewModel _endState = new();

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
    private bool _isSelected;
}
