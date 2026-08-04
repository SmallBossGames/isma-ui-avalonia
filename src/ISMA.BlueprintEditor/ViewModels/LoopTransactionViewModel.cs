using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

public partial class LoopTransactionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _predicate;

    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [ObservableProperty]
    private string _alias;

    [ObservableProperty]
    private bool _selected;

    private readonly string _stateName;
    private readonly string _text;

    public LoopTransactionViewModel(string stateName, string predicate, string alias, string text)
    {
        _stateName = stateName;
        _predicate = predicate;
        _alias = alias;
        _text = text;
    }

    public string DisplayText => string.IsNullOrWhiteSpace(Alias) ? Predicate : Alias;

    public string StateName => _stateName;
    public string Text => _text;
}
