using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class EventDetectionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isEventDetectionInUse;

    [ObservableProperty]
    private bool _isStepLimitInUse;

    [ObservableProperty]
    private double _gamma = 0.8;

    [ObservableProperty]
    private double _lowBorder = 0.001;
}
