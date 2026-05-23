using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class CompletedSimulationViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _modelName = "";

    [ObservableProperty]
    private SimulationParameters _parameters = new();

    [ObservableProperty]
    private string _cachedFile = "";

    private readonly CompletedSimulation _source;

    public CompletedSimulationViewModel(CompletedSimulation source)
    {
        _source = source;
        Id = source.Id;
        ModelName = source.ModelName;
        Parameters = source.Parameters;
        CachedFile = source.CachedFile;
    }

    [RelayCommand]
    private void Show()
    {
    }

    [RelayCommand]
    private void Export()
    {
    }

    [RelayCommand]
    private void Remove()
    {
    }
}
