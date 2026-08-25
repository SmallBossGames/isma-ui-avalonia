using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Text.Json;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Services;

public class SimulationParametersService : ISimulationParametersStoreService
{
    private readonly Window? _owner;
    private readonly WindowProvider? _windowProvider;
    private readonly SimulationParametersViewModel? _parametersVm;

    public SimulationParametersService(Window? owner = null, SimulationParametersViewModel? parametersVm = null, WindowProvider? windowProvider = null)
    {
        _owner = owner;
        _parametersVm = parametersVm;
        _windowProvider = windowProvider;
    }

    private Window? Owner => _owner ?? _windowProvider?.Current;

    public string[] IntegrationMethods { get; set; } = Array.Empty<string>();

    public async Task<bool> StoreAsync()
    {
        var topLevel = TopLevel.GetTopLevel(Owner);
        if (topLevel is null) return false;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Simulation Parameters",
            SuggestedFileName = "params.json",
            DefaultExtension = "params.json",
            ShowOverwritePrompt = true
        });

        if (file is null) return false;

        var json = JsonSerializer.Serialize(SimulationParametersFileModel.From(Snapshot()), SimulationParametersFileModel.JsonOptions);
        await File.WriteAllTextAsync(file.Path.LocalPath, json);
        return true;
    }

    public async Task<bool> LoadAsync()
    {
        var topLevel = TopLevel.GetTopLevel(Owner);
        if (topLevel is null) return false;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Simulation Parameters",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Simulation Parameters File") { Patterns = new[] { "*.params.json" } }
            }
        });

        if (files.Count == 0) return false;

        var json = await File.ReadAllTextAsync(files[0].Path.LocalPath);
        var fileModel = JsonSerializer.Deserialize<SimulationParametersFileModel>(json, SimulationParametersFileModel.JsonOptions);
        if (fileModel is null) return false;

        Commit(fileModel.ToModel());
        return true;
    }

    public SimulationParameters Snapshot()
    {
        if (_parametersVm == null)
        {
            return new SimulationParameters
            {
                CauchyInitials = new CauchyInitials { StartTime = 0.0, EndTime = 10.0, InitialStep = 0.1 },
                IntegrationMethod = new IntegrationMethodParameters { Accuracy = 0.1 },
                EventDetection = new EventDetectionParameters { Gamma = 0.8, LowBorder = 0.001 },
                ResultSaving = new ResultSavingParameters { SavingTarget = SaveTarget.Memory }
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
                IsStableAllowedInUse = _parametersVm.IntegrationMethod.IsStableAllowedInUse,
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
            }
        };
    }

    public void Commit(SimulationParameters model)
    {
        _parametersVm?.Commit(model);
    }
}
