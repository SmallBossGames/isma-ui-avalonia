using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class TasksPopOverViewModel : ObservableObject
{
    private ObservableCollection<InProgressSimulationViewModel> _inProgress = new();
    private ObservableCollection<CompletedSimulationViewModel> _completed = new();
    private ObservableCollection<FailedSimulationViewModel> _failed = new();

    public ObservableCollection<InProgressSimulationViewModel> InProgress
    {
        get => _inProgress;
        set => SetProperty(ref _inProgress, value);
    }

    public ObservableCollection<CompletedSimulationViewModel> Completed
    {
        get => _completed;
        set => SetProperty(ref _completed, value);
    }

    public ObservableCollection<FailedSimulationViewModel> Failed
    {
        get => _failed;
        set => SetProperty(ref _failed, value);
    }

    public int InProgressCount => InProgress.Count;
    public int CompletedCount => Completed.Count;
    public int FailedCount => Failed.Count;

    public void AddInProgress(InProgressSimulationViewModel simulation)
    {
        InProgress.Add(simulation);
    }

    public void RemoveInProgress(InProgressSimulationViewModel simulation)
    {
        InProgress.Remove(simulation);
    }

    public void AddCompleted(CompletedSimulation completed)
    {
        Completed.Add(new CompletedSimulationViewModel(completed));
    }

    public void AddCompleted(CompletedSimulationViewModel simulation)
    {
        Completed.Add(simulation);
    }

    public void RemoveCompleted(CompletedSimulationViewModel simulation)
    {
        Completed.Remove(simulation);
    }

    public void ClearCompleted()
    {
        Completed.Clear();
    }

    public void ClearInProgress()
    {
        InProgress.Clear();
    }

    public void ClearFailed()
    {
        Failed.Clear();
    }
}
