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
    private readonly SimulationParametersViewModel _parametersViewModel;
    private readonly TasksPopOverViewModel? _tasksPopOver;

    private long _nextTaskId = 1;

    public ISimulationServerFacade SimulationServerFacade => _serverFacade;

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
        SimulationParametersViewModel parametersViewModel,
        TasksPopOverViewModel? tasksPopOver = null)
    {
        _serverFacade = serverFacade;
        _errorService = errorService;
        _resultService = resultService;
        _parametersViewModel = parametersViewModel;
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
            var snapshot = _parametersViewModel.Snapshot();
            var lismaText = project.GetModel();

            CompileResult compileResult;
            try
            {
                compileResult = await _serverFacade.CompileModel(lismaText.FullText);
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                return;
            }

            if (compileResult.Errors.Length > 0)
            {
                var errors = compileResult.Errors.Select(e => new ErrorInfo
                {
                    Row = e.Row,
                    Position = e.Column,
                    FragmentName = lismaText.FragmentNameByLine(e.Row),
                    Message = e.Message
                });
                _errorService.PutErrorList(errors);
                StatusText = "Compilation failed";
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
                IsStabilityControlInUse = snapshot.IntegrationMethod.IsStableInUse,
                CompiledModelId = compileResult.ModelId,
                IsEventDetectionInUse = snapshot.EventDetection.IsEventDetectionInUse,
                EventDetectionGamma = snapshot.EventDetection.Gamma,
                EventDetectionLowBorder = snapshot.EventDetection.LowBorder
            };

            long simulationId;
            try
            {
                simulationId = await _serverFacade.RunSimulation(runParams);
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                return;
            }

            var inProgress = new InProgressSimulationViewModel(
                NextTaskId(),
                (int)simulationId,
                project.Name,
                snapshot,
                _serverFacade,
                _tasksPopOver);
            TrackingTasks.Add(inProgress);
            _tasksPopOver?.AddInProgress(inProgress);

            StatusText = "Monitoring simulation...";

            try
            {
                await foreach (var progress in _serverFacade.MonitorSimulation(simulationId))
                {
                    inProgress.Progress = NormalizeProgress(progress);
                }
            }
            catch (Exception ex)
            {
                FailTask(inProgress, $"Monitor error: {ex.Message}");
                return;
            }

            StatusText = "Downloading results...";

            CachedSimulationResult cachedResult;
            try
            {
                cachedResult = await _serverFacade.DownloadResult(simulationId);
            }
            catch (Exception ex)
            {
                FailTask(inProgress, $"Download error: {ex.Message}");
                return;
            }

            var completed = new CompletedSimulation
            {
                Id = (int)simulationId,
                ModelName = project.Name,
                Parameters = snapshot,
                MetricData = new MetricData(),
                CachedFile = cachedResult.File,
                CachedColumnNames = cachedResult.ColumnNames
            };

            _resultService.CommitResult(completed);
            TrackingTasks.Remove(inProgress);
            _tasksPopOver?.RemoveInProgress(inProgress);

            var completedVm = new CompletedSimulationViewModel(completed, _resultService, _tasksPopOver)
            {
                TaskId = inProgress.TaskId
            };
            _tasksPopOver?.AddCompleted(completedVm);

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

    private int NextTaskId() => (int)_nextTaskId++;

    private void FailTask(InProgressSimulationViewModel inProgress, string message)
    {
        TrackingTasks.Remove(inProgress);
        _tasksPopOver?.RemoveInProgress(inProgress);
        _tasksPopOver?.Failed.Add(new FailedSimulationViewModel(inProgress.TaskId, message, _tasksPopOver));
        StatusText = message;
    }

    private static double NormalizeProgress(SimulationProgress progress)
    {
        if (progress.EndTime <= progress.StartTime)
            return 0.0;

        var value = (progress.CurrentTime - progress.StartTime) / (progress.EndTime - progress.StartTime);
        return Math.Clamp(value, 0.0, 1.0);
    }
}
