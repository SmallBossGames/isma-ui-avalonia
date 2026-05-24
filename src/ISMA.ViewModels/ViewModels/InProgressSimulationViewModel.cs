using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class InProgressSimulationViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _modelName = "";

    [ObservableProperty]
    private SimulationParameters _parameters = new();

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _canAbort;

    [RelayCommand]
    private void Abort()
    {
    }
}
