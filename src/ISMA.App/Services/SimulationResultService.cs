using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ISMA.App.Views;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.Infrastructure.ChartViewer;
using ISMA.Infrastructure.Server;
using ISMA.ViewModels.ViewModels;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace ISMA.App.Services;

public class SimulationResultService : ISimulationResultService
{
    private readonly GrinProcessLauncher _grinLauncher;
    private readonly Window? _owner;
    private readonly ObservableCollection<CompletedSimulation> _trackingTasksResults = new();
    private readonly object _lock = new();

    public SimulationResultService(GrinProcessLauncher grinLauncher, Window? owner = null)
    {
        _grinLauncher = grinLauncher;
        _owner = owner;
    }

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
        var topLevel = TopLevel.GetTopLevel(_owner);
        if (topLevel is null) return;

        var columns = simulation.CachedColumnNames.ToArray();
        if (columns.Length == 0) return;

        var dialog = new SelectVariablesDialogViewModel();
        dialog.InitializeColumns(columns);
        var window = new SelectVariablesDialogWindow(dialog);

        bool? result;
        if (_owner is not null)
            result = await window.ShowDialog<bool?>(_owner);
        else
            result = null;

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
            var provider = simulation.EquationIndexProvider;
            var header = new StringBuilder();
            header.Append("x");
            if (provider is not null)
            {
                for (int i = 0; i < provider.GetDifferentialEquationCount(); i++)
                    header.Append(",").Append(provider.GetDifferentialEquationCode(i));
                for (int i = 0; i < provider.GetAlgebraicEquationCount(); i++)
                    header.Append(",").Append(provider.GetAlgebraicEquationCode(i));
            }
            else
            {
                foreach (var col in simulation.CachedColumnNames)
                    header.Append(",").Append(col);
            }
            writer.WriteLine(header.ToString());

            var pointProvider = BinaryFilePointProvider.Read(simulation.CachedFile);
            foreach (var point in pointProvider.Results)
            {
                var line = new StringBuilder();
                line.Append(point.X);
                foreach (var y in point.YForDe) line.Append(",").Append(y);
                foreach (var rhs in point.Rhs)
                    foreach (var v in rhs) line.Append(",").Append(v);
                writer.WriteLine(line.ToString());
            }
        });
    }

    public async Task ShowExportDialog(CompletedSimulation simulation)
    {
        var topLevel = TopLevel.GetTopLevel(_owner);
        if (topLevel is null || string.IsNullOrEmpty(simulation.CachedFile))
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Export CSV",
            FileTypeFilter = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
        });

        if (files.Count > 0)
        {
            await ExportToFile(simulation, files[0].Path.LocalPath);
        }
    }
}
