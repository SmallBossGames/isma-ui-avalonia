using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.App.ViewModels;

public partial class CauchyInitialsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _startTime = 0.0;

    [ObservableProperty]
    private double _endTime = 10.0;

    [ObservableProperty]
    private double _initialStep = 0.1;
}
