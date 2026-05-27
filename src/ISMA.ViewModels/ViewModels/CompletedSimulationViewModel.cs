using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
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
    private readonly ISimulationResultService? _resultService;
    private readonly TasksPopOverViewModel? _tasksPopOver;

    public CompletedSimulationViewModel(CompletedSimulation source, ISimulationResultService? resultService = null, TasksPopOverViewModel? tasksPopOver = null)
    {
        _source = source;
        _resultService = resultService;
        _tasksPopOver = tasksPopOver;
        Id = source.Id;
        ModelName = source.ModelName;
        Parameters = source.Parameters;
        CachedFile = source.CachedFile;
    }

    [RelayCommand]
    private async Task Show()
    {
        if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
        {
            await _resultService.ShowChart(_source);
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
        {
            await _resultService.ExportToFile(_source, CachedFile);
        }
    }

    [RelayCommand]
    private void Remove()
    {
        if (_resultService != null && !string.IsNullOrEmpty(CachedFile))
        {
            _resultService.RemoveResult(_source);
            _tasksPopOver?.RemoveCompleted(this);
        }
    }
}
