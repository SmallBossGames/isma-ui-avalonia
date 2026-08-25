using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ISMA.ViewModels.ViewModels;

public partial class FailedSimulationViewModel : ObservableObject
{
    [ObservableProperty]
    private int _taskId;

    [ObservableProperty]
    private string _errorText = "";

    private readonly TasksPopOverViewModel? _tasksPopOver;

    public FailedSimulationViewModel(int taskId, string errorText, TasksPopOverViewModel? tasksPopOver = null)
    {
        TaskId = taskId;
        ErrorText = errorText;
        _tasksPopOver = tasksPopOver;
    }

    [RelayCommand]
    private void Remove()
    {
        _tasksPopOver?.Failed.Remove(this);
    }
}
