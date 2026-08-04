using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

public partial class TransactionViewModel : ObservableObject
{
    private readonly string _startStateName;
    private readonly string _endStateName;

    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [ObservableProperty]
    private string _predicate;

    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [ObservableProperty]
    private string _alias;

    [ObservableProperty]
    private bool _selected;

    public TransactionViewModel(string startStateName, string endStateName, string predicate, string alias)
    {
        _startStateName = startStateName;
        _endStateName = endStateName;
        _predicate = predicate;
        _alias = alias;
    }

    public string DisplayText => string.IsNullOrWhiteSpace(Alias) ? Predicate : Alias;

    public string StartStateName => _startStateName;
    public string EndStateName => _endStateName;
}
