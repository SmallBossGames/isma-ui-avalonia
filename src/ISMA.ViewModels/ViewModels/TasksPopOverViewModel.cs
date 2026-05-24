using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class TasksPopOverViewModel : ObservableObject
{
    private ObservableCollection<InProgressSimulationViewModel> _inProgress = new();
    private ObservableCollection<CompletedSimulationViewModel> _completed = new();

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

    public int InProgressCount => InProgress.Count;
    public int CompletedCount => Completed.Count;

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
        var vm = new CompletedSimulationViewModel(completed);
        Completed.Add(vm);
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
}
