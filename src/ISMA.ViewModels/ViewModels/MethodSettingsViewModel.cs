using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class MethodSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedMethod = "";

    [ObservableProperty]
    private double _accuracy = 0.1;

    [ObservableProperty]
    private bool _isAccuracyInUse;

    [ObservableProperty]
    private bool _isStableAllowedInUse;

    [ObservableProperty]
    private bool _isStableInUse;
}
