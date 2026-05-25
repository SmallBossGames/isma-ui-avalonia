using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;
using System.Text.Json;

namespace ISMA.App.Services;

public class SimulationParametersService
{
    private readonly Window? _owner;
    private readonly SimulationParametersViewModel? _parametersVm;

    public SimulationParametersService(Window? owner = null, SimulationParametersViewModel? parametersVm = null)
    {
        _owner = owner;
        _parametersVm = parametersVm;
    }

    public static string[] SimplifyMethods => ["Radial-Distance", "Douglas-Peucker"];

    public string[] IntegrationMethods { get; set; } = Array.Empty<string>();

    public async Task<bool> Store(object? ownerWindow)
    {
        var control = ownerWindow as Avalonia.Visual ?? _owner as Avalonia.Visual;
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is null) return false;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Store Simulation Parameters",
            SuggestedFileName = "params.json",
            DefaultExtension = ".json",
            ShowOverwritePrompt = true
        });

        if (file is null) return false;

        var json = JsonSerializer.Serialize(Snapshot(), new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(file.Path.LocalPath, json);
        return true;
    }

    public async Task<bool> Load(object? ownerWindow, Action<SimulationParameters> applyCallback)
    {
        var control = ownerWindow as Avalonia.Visual ?? _owner as Avalonia.Visual;
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is null) return false;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Simulation Parameters",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
            }
        });

        if (files.Count == 0) return false;

        var json = await File.ReadAllTextAsync(files[0].Path.LocalPath);
        var paramsModel = JsonSerializer.Deserialize<SimulationParameters>(json);
        if (paramsModel is null) return false;

        applyCallback(paramsModel);
        return true;
    }

    public SimulationParameters Snapshot()
    {
        if (_parametersVm == null)
        {
            return new SimulationParameters
            {
                CauchyInitials = new CauchyInitials { StartTime = 0.0, EndTime = 10.0, InitialStep = 0.1 },
                IntegrationMethod = new IntegrationMethodParameters { Accuracy = 0.1, Server = "localhost", Port = 7890 },
                EventDetection = new EventDetectionParameters { Gamma = 0.8, LowBorder = 0.001 },
                ResultSaving = new ResultSavingParameters { SavingTarget = SaveTarget.Memory },
                ResultProcessing = new ResultProcessingParameters { SelectedSimplifyMethod = "Radial-Distance" }
            };
        }

        return new SimulationParameters
        {
            CauchyInitials = new CauchyInitials
            {
                StartTime = _parametersVm.CauchyInitials.StartTime,
                EndTime = _parametersVm.CauchyInitials.EndTime,
                InitialStep = _parametersVm.CauchyInitials.InitialStep
            },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = _parametersVm.IntegrationMethod.SelectedMethod,
                Accuracy = _parametersVm.IntegrationMethod.Accuracy,
                IsAccuracyInUse = _parametersVm.IntegrationMethod.IsAccuracyInUse,
                IsStableInUse = _parametersVm.IntegrationMethod.IsStableInUse,
                IsParallelInUse = _parametersVm.IntegrationMethod.IsParallelInUse,
                Server = _parametersVm.IntegrationMethod.Server,
                Port = _parametersVm.IntegrationMethod.Port
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = _parametersVm.EventDetection.IsEventDetectionInUse,
                IsStepLimitInUse = _parametersVm.EventDetection.IsStepLimitInUse,
                Gamma = _parametersVm.EventDetection.Gamma,
                LowBorder = _parametersVm.EventDetection.LowBorder
            },
            ResultSaving = new ResultSavingParameters
            {
                SavingTarget = _parametersVm.ResultSaving.SavingTarget
            },
            ResultProcessing = new ResultProcessingParameters
            {
                IsSimplifyInUse = _parametersVm.ResultProcessing.IsSimplifyInUse,
                SelectedSimplifyMethod = _parametersVm.ResultProcessing.SelectedSimplifyMethod,
                Tolerance = _parametersVm.ResultProcessing.Tolerance
            }
        };
    }

    public void Commit(SimulationParameters model)
    {
        if (_parametersVm == null) return;

        _parametersVm.Commit(model);
    }
}
