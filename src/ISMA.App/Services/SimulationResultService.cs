using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ISMA.App.Views;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ExternalServices.ChartViewer;
using ISMA.ExternalServices.Server;
using ISMA.App.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ISMA.App.Services;

public class SimulationResultService : ISimulationResultService
{
    private readonly GrinProcessLauncher _grinLauncher;
    private readonly Window? _owner;
    private readonly WindowProvider? _windowProvider;
    private readonly ObservableCollection<CompletedSimulation> _trackingTasksResults = new();
    private readonly object _lock = new();

    public SimulationResultService(GrinProcessLauncher grinLauncher, Window? owner = null, WindowProvider? windowProvider = null)
    {
        _grinLauncher = grinLauncher;
        _owner = owner;
        _windowProvider = windowProvider;
    }

    private Window? Owner => _owner ?? _windowProvider?.Current;

    public IEnumerable<CompletedSimulation> TrackingTasksResults => _trackingTasksResults;

    public void CommitResult(CompletedSimulation simulation)
    {
        lock (_lock)
        {
            _trackingTasksResults.Add(simulation);
        }
    }

    public void RemoveResult(CompletedSimulation simulation)
    {
        lock (_lock)
        {
            _trackingTasksResults.Remove(simulation);
        }
    }

    public async Task ShowChart(CompletedSimulation simulation)
    {
        var owner = Owner;
        if (owner is null) return;

        var columns = simulation.CachedColumnNames.ToArray();
        if (columns.Length == 0) return;

        var dialog = new SelectVariablesDialogViewModel();
        dialog.InitializeColumns(columns);
        var window = new SelectVariablesDialogWindow(dialog);

        var result = await window.ShowDialog<bool?>(owner);

        if (result == true && !string.IsNullOrEmpty(dialog.SelectedXAxis))
        {
            var yAxes = dialog.SelectedYAxes.ToArray();
            await _grinLauncher.RunAsync(simulation.CachedFile, dialog.SelectedXAxis, yAxes);
        }
    }

    public async Task ExportToFile(CompletedSimulation simulation, string filePath)
    {
        await Task.Run(() =>
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.WriteLine(BuildHeader(simulation));

            var pointProvider = BinaryFilePointProvider.Read(simulation.CachedFile);
            foreach (var point in pointProvider.Results)
            {
                writer.WriteLine(ToCsvLine(point));
            }
        });
    }

    public async Task ShowExportDialog(CompletedSimulation simulation)
    {
        var topLevel = TopLevel.GetTopLevel(Owner);
        if (topLevel is null || string.IsNullOrEmpty(simulation.CachedFile))
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Results",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Comma separate file") { Patterns = new[] { "*.csv" } }
            },
            DefaultExtension = "csv"
        });

        if (file is not null)
        {
            await ExportToFile(simulation, file.Path.LocalPath);
        }
    }

    private static string BuildHeader(CompletedSimulation simulation)
    {
        var parts = new List<string> { "x" };
        var provider = simulation.EquationIndexProvider;

        if (provider is not null)
        {
            for (var i = 0; i < provider.GetDifferentialEquationCount(); i++)
                parts.Add(provider.GetDifferentialEquationCode(i));

            for (var i = 0; i < provider.GetAlgebraicEquationCount(); i++)
                parts.Add(provider.GetAlgebraicEquationCode(i));

            for (var i = 0; i < provider.GetDifferentialEquationCount(); i++)
                parts.Add($"f{i}");
        }
        else
        {
            parts.AddRange(simulation.CachedColumnNames);
        }

        return string.Join(", ", parts);
    }

    private static string ToCsvLine(SimulationPoint point)
    {
        var parts = new List<string> { FormatDouble(point.X) };
        parts.AddRange(point.YForDe.Select(FormatDouble));

        if (point.Rhs.Length > RhsAePartIdx)
            parts.AddRange(point.Rhs[RhsAePartIdx].Select(FormatDouble));

        if (point.Rhs.Length > RhsDePartIdx)
            parts.AddRange(point.Rhs[RhsDePartIdx].Select(FormatDouble));

        return string.Join(", ", parts);
    }

    private static string FormatDouble(double value) =>
        KotlinDoubleJsonConverter.Format(value);

    private const int RhsDePartIdx = 0;
    private const int RhsAePartIdx = 1;
}
