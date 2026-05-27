using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class InProgressSimulationViewModel : ObservableObject, IDisposable
{
    private readonly ISimulationServerFacade? _serverFacade;
    private readonly TasksPopOverViewModel? _tasksPopOver;

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

    public InProgressSimulationViewModel()
    {
    }

    public InProgressSimulationViewModel(int id, string modelName, SimulationParameters parameters, ISimulationServerFacade? serverFacade = null, TasksPopOverViewModel? tasksPopOver = null)
    {
        Id = id;
        ModelName = modelName;
        Parameters = parameters;
        _serverFacade = serverFacade;
        _tasksPopOver = tasksPopOver;
        Progress = 0.0;
        CanAbort = true;
    }

    [RelayCommand]
    private async Task Abort()
    {
        if (CanAbort && _serverFacade != null)
        {
            CanAbort = false;
            try
            {
                await _serverFacade.CancelSimulation(Id);
                _tasksPopOver?.RemoveInProgress(this);
            }
            catch
            {
                // Silently ignore — server may already be stopped
                _tasksPopOver?.RemoveInProgress(this);
            }
        }
    }

    public void RemoveFromTasks()
    {
        _tasksPopOver?.RemoveInProgress(this);
    }

    public void Dispose()
    {
    }
}
