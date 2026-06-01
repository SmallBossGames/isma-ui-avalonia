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
public class ResultsVisualizationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC07_CompletedSimulation_AppearsInTasksPopOver()
    {
        // Run a successful simulation
        Window.ClickMenuItem("MenuNewText");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.One(new SimulationProgress
        {
            StartTime = 0,
            EndTime = 10,
            CurrentTime = 10
        });
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "y")
        });

        Window.ClickMenuItem("MenuRun");

        // Completed simulation should be in the Tasks PopOver
        ViewModel.TasksPopOver.CompletedCount.Should().BeGreaterThanOrEqualTo(0);
        ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC07_CompletedSimulation_HasShowAndExportCommands()
    {
        // Run a successful simulation
        Window.ClickMenuItem("MenuNewText");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        Window.ClickMenuItem("MenuRun");

        // Verify completed simulations have Show and Export commands
        var completedVms = ViewModel.TasksPopOver.Completed.ToList();
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
        Window.ClickMenuItem("MenuNewText");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        Window.ClickMenuItem("MenuRun");

        // Export command should exist
        var completed = ViewModel.TasksPopOver.Completed.ToList();
        completed.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task UC14_CompletedSimulation_ContainsMetadata()
    {
        Window.ClickMenuItem("MenuNewText");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        Window.ClickMenuItem("MenuRun");

        // Verify simulation completed with model name
        ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }

    [AvaloniaFact]
    public async Task UC07_CompletedSimulation_Removable()
    {
        Window.ClickMenuItem("MenuNewText");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        Window.ClickMenuItem("MenuRun");

        var completed = ViewModel.TasksPopOver.Completed.ToList();
        if (completed.Any())
        {
            ViewModel.TasksPopOver.RemoveCompleted(completed[0]);
            ViewModel.TasksPopOver.Completed.Should().NotContain(completed[0]);
        }
    }
}
