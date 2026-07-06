using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintLoopTransactionViewModel : ObservableObject
{
    [ObservableProperty]
    private BlueprintStateViewModel _state = new();

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
}
