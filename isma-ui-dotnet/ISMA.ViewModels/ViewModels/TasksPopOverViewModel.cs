using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class TasksPopOverViewModel : ObservableObject
{
    private ObservableCollection<InProgressSimulationViewModel> _inProgress = new();
    private ObservableCollection<CompletedSimulation> _completed = new();

    public ObservableCollection<InProgressSimulationViewModel> InProgress
    {
        get => _inProgress;
        set => SetProperty(ref _inProgress, value);
    }

    public ObservableCollection<CompletedSimulation> Completed
    {
        get => _completed;
        set => SetProperty(ref _completed, value);
    }

    public int InProgressCount => InProgress.Count;
    public int CompletedCount => Completed.Count;
}
