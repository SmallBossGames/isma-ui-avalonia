using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class CompletedSimulationViewModel : ObservableObject
{
    [ObservableProperty]
    private int _taskId;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _modelName = "";

    [ObservableProperty]
    private SimulationParameters _parameters = new();

    [ObservableProperty]
    private MetricData _metricData = new();

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
        MetricData = source.MetricData;
        CachedFile = source.CachedFile;
    }

    /// <summary>
    /// Multi-line details text, ported from the original Kotlin
    /// <c>TaskItemViewModel.detailsText</c>.
    /// </summary>
    public string DetailsText
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine("Model");
            sb.AppendLine($"Name: {ModelName}");
            sb.AppendLine();
            sb.AppendLine("Cauchy Initials");
            sb.AppendLine($"Start: {FormatDouble(Parameters.CauchyInitials.StartTime)}");
            sb.AppendLine($"End: {FormatDouble(Parameters.CauchyInitials.EndTime)}");
            sb.AppendLine($"Initial step: {FormatDouble(Parameters.CauchyInitials.InitialStep)}");
            sb.AppendLine();
            sb.AppendLine("Integration Method");
            sb.AppendLine($"Method: {Parameters.IntegrationMethod.SelectedMethod}");
            sb.AppendLine($"Is accurate: {FormatBool(Parameters.IntegrationMethod.IsAccuracyInUse)}");
            if (Parameters.IntegrationMethod.IsAccuracyInUse)
            {
                sb.AppendLine($"Accuracy: {FormatDouble(Parameters.IntegrationMethod.Accuracy)}");
            }
            sb.AppendLine($"Is stable: {FormatBool(Parameters.IntegrationMethod.IsStableInUse)}");
            sb.AppendLine();
            sb.AppendLine("Statistic");
            sb.AppendLine($"Simulation time: {MetricData.SimulationTime}ms");
            sb.AppendLine();
            return sb.ToString();
        }
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
            await _resultService.ShowExportDialog(_source);
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

    private static string FormatDouble(double value) =>
        KotlinDoubleJsonConverter.Format(value);

    private static string FormatBool(bool value) =>
        value ? "true" : "false";
}
