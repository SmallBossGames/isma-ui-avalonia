using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class ResultProcessingViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSimplifyInUse;

    [ObservableProperty]
    private string _selectedSimplifyMethod = "Radial-Distance";

    [ObservableProperty]
    private double _tolerance = 0.001;
}
