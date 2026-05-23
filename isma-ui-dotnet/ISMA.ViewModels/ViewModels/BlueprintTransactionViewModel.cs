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

    [ObservableProperty]
    private string _alias = "";
}
