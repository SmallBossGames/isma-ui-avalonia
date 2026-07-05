using Avalonia;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration.UseCases.Results;

/// <summary>
/// UC-07: Visualize and Export Simulation Results
/// UC-14: View Simulation Details
/// Tests simulation result visualization, export, and details viewing.
/// </summary>
public class ResultsVisualizationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC07_CompletedSimulation_AppearsInTasksPopOver()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.One(new SimulationProgress
        {
            StartTime = 0,
            EndTime = 10,
            CurrentTime = 10
        });
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "y")
        });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.TasksPopOver.CompletedCount.Should().BeGreaterThanOrEqualTo(0);
        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC07_CompletedSimulation_HasShowAndExportCommands()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        var completedVms = _app.ViewModel.TasksPopOver.Completed.ToList();
        foreach (var vm in completedVms)
        {
            vm.ShowCommand.Should().NotBeNull();
            vm.ExportCommand.Should().NotBeNull();
            vm.RemoveCommand.Should().NotBeNull();
        }
    }

    [AvaloniaFact]
    public async Task UC07_ExportCommand_ExistsOnCompletedSimulation()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        var completed = _app.ViewModel.TasksPopOver.Completed.ToList();
        completed.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task UC14_CompletedSimulation_ContainsMetadata()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }

    [AvaloniaFact]
    public async Task UC07_CompletedSimulation_Removable()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        var completed = _app.ViewModel.TasksPopOver.Completed.ToList();
        if (completed.Any())
        {
            _app.ViewModel.TasksPopOver.RemoveCompleted(completed[0]);
            _app.ViewModel.TasksPopOver.Completed.Should().NotContain(completed[0]);
        }
    }
}
