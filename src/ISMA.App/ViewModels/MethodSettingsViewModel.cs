using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.App.ViewModels;

public partial class MethodSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedMethod = "";

    [ObservableProperty]
    private int _selectedMethodIndex = -1;

    [ObservableProperty]
    private ObservableCollection<string> _integrationMethods = new();

    [ObservableProperty]
    private double _accuracy = 0.1;

    [ObservableProperty]
    private bool _isAccuracyInUse;

    [ObservableProperty]
    private bool _isStableAllowedInUse;

    [ObservableProperty]
    private bool _isStableInUse;

    [ObservableProperty]
    private bool _isParallelInUse;

    [ObservableProperty]
    private string _server = "localhost";

    [ObservableProperty]
    private int _port = 7890;

    partial void OnSelectedMethodIndexChanged(int value)
    {
        if (value >= 0 && value < IntegrationMethods.Count)
        {
            SelectedMethod = IntegrationMethods[value];
        }
    }

    partial void OnSelectedMethodChanged(string value)
    {
        var index = IntegrationMethods.IndexOf(value);
        if (index >= 0)
        {
            SelectedMethodIndex = index;
        }
    }
}
