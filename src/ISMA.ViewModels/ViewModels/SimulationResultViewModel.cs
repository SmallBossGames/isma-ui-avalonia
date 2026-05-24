using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class SimulationResultViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _modelName = "";

    [ObservableProperty]
    private string _cachedFile = "";

    [ObservableProperty]
    private double _startTime;

    [ObservableProperty]
    private double _endTime;

    [ObservableProperty]
    private int _pointCount;

    public SimulationResultViewModel()
    {
    }

    public SimulationResultViewModel(CompletedSimulation completed)
    {
        Id = completed.Id;
        ModelName = completed.ModelName;
        CachedFile = completed.CachedFile;
        StartTime = completed.MetricData?.StartTime ?? 0.0;
        EndTime = completed.MetricData?.EndTime ?? 0.0;
        PointCount = 0;
    }
}
