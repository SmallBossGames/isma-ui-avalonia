using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class SimulationServiceViewModel : ObservableObject
{
    private readonly ISimulationServerFacade _serverFacade;
    private readonly IModelErrorService _errorService;
    private readonly ISimulationResultService _resultService;
    private readonly SimulationParametersService _parametersService;
    private readonly TasksPopOverViewModel? _tasksPopOver;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Ready";

    private ObservableCollection<InProgressSimulationViewModel> _trackingTasks = new();

    public ObservableCollection<InProgressSimulationViewModel> TrackingTasks
    {
        get => _trackingTasks;
        set => SetProperty(ref _trackingTasks, value);
    }

    public SimulationServiceViewModel(
        ISimulationServerFacade serverFacade,
        IModelErrorService errorService,
        ISimulationResultService resultService,
        SimulationParametersService parametersService,
        TasksPopOverViewModel? tasksPopOver = null)
    {
        _serverFacade = serverFacade;
        _errorService = errorService;
        _resultService = resultService;
        _parametersService = parametersService;
        _tasksPopOver = tasksPopOver;
    }

    public async Task SimulateAsync(LismaProjectViewModel project)
    {
        if (IsRunning)
            return;

        IsRunning = true;
        StatusText = "Compiling...";

        try
        {
            var snapshot = _parametersService.GetParameters();
            var source = project.FullText;

            var compileResult = await _serverFacade.CompileModel(source);
            if (compileResult.Errors.Length > 0)
            {
                var errors = compileResult.Errors.Select(e => new ErrorInfo
                {
                    Row = e.Row,
                    Position = e.Column,
                    FragmentName = "Main",
                    Message = e.Message
                });
                _errorService.PutErrorList(errors);
                StatusText = "Compilation failed";
                IsRunning = false;
                return;
            }

            StatusText = "Running simulation...";

            var runParams = new RunSimulationParams
            {
                StartTime = snapshot.CauchyInitials.StartTime,
                EndTime = snapshot.CauchyInitials.EndTime,
                InitialStep = snapshot.CauchyInitials.InitialStep,
                MethodName = snapshot.IntegrationMethod.SelectedMethod,
                Accuracy = snapshot.IntegrationMethod.Accuracy,
                IsAccuracyInUse = snapshot.IntegrationMethod.IsAccuracyInUse,
                IsStabilityControlInUse = snapshot.IntegrationMethod.IsStableInUse || snapshot.IntegrationMethod.IsStableAllowedInUse,
                CompiledModelId = compileResult.ModelId,
                EventDetectionGamma = snapshot.EventDetection.IsEventDetectionInUse ? snapshot.EventDetection.Gamma : null,
                EventDetectionLowBorder = snapshot.EventDetection.IsEventDetectionInUse ? snapshot.EventDetection.LowBorder : null,
                IsParallelInUse = snapshot.IntegrationMethod.IsParallelInUse,
                Server = snapshot.IntegrationMethod.Server,
                Port = snapshot.IntegrationMethod.Port
            };

            long simulationId = await _serverFacade.RunSimulation(runParams);

            var inProgress = new InProgressSimulationViewModel(
                (int)simulationId,
                project.Name,
                snapshot,
                _serverFacade,
                _tasksPopOver);
            TrackingTasks.Add(inProgress);
            _tasksPopOver?.AddInProgress(inProgress);

            StatusText = "Monitoring simulation...";

            await foreach (var progress in _serverFacade.MonitorSimulation(simulationId))
            {
                inProgress.Progress = progress.EndTime > progress.StartTime
                    ? (progress.CurrentTime - progress.StartTime) / (progress.EndTime - progress.StartTime)
                    : 0.0;
            }

            StatusText = "Downloading results...";

            var cachedResult = await _serverFacade.DownloadResult(simulationId);

            var completed = new CompletedSimulation
            {
                Id = (int)simulationId,
                ModelName = project.Name,
                CachedFile = cachedResult.File,
                CachedColumnNames = cachedResult.ColumnNames
            };

            _resultService.CommitResult(completed);
            TrackingTasks.Remove(inProgress);
            _tasksPopOver?.RemoveInProgress(inProgress);

            var completedVm = new CompletedSimulationViewModel(completed, _resultService, _tasksPopOver);
            _tasksPopOver?.Completed.Add(completedVm);

            StatusText = "Simulation complete";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

   public async Task StopSimulationAsync(InProgressSimulationViewModel simulation)
    {
        if (simulation == null)
            return;

        simulation.CanAbort = false;

        try
        {
            await _serverFacade.CancelSimulation(simulation.Id);
        }
        finally
        {
            TrackingTasks.Remove(simulation);
            _tasksPopOver?.RemoveInProgress(simulation);
            StatusText = "Simulation stopped";
        }
    }

    public void ClearTrackingTasks()
    {
        _trackingTasks.Clear();
    }
}
