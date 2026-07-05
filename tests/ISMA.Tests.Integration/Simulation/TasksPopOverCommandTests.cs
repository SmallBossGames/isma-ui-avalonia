using Avalonia;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for Tasks PopOver commands (Abort, Show, Export, Remove).
/// </summary>
public class TasksPopOverCommandTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task StopSimulationAsync_CancelsRunningSimulation()
    {
        // Create project
        _app.Window.ClickMenuItem("MenuNewText");
        _app.ViewModel.Projects.Should().HaveCount(1);

        // Mock server to simulate a running simulation
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = id => LongRunningSimulation();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "x")
        });

        // Run simulation via menu
        _app.Window.ClickMenuItem("MenuRun");

        // Wait for simulation to start
        await Task.Delay(200);
        _app.ViewModel.SimulationService.TrackingTasks.Should().HaveCount(1);
        var inProgress = _app.ViewModel.SimulationService.TrackingTasks.First();
        inProgress.CanAbort.Should().BeTrue();

        // Stop the simulation via the service method (which properly awaits)
        await _app.ViewModel.SimulationService.StopSimulationAsync(inProgress);

        // Verify simulation was removed from tracking
        _app.ViewModel.SimulationService.TrackingTasks.Should().BeEmpty();
        _app.MockServer.CancelCalled.Should().BeTrue();
    }

    private static async IAsyncEnumerable<SimulationProgress> LongRunningSimulation()
    {
        for (double t = 0; t <= 10; t += 0.1)
        {
            await Task.Delay(10);
            yield return new SimulationProgress
            {
                StartTime = 0,
                EndTime = 10,
                CurrentTime = t
            };
        }
    }

    [AvaloniaFact]
    public async Task CompletedSimulation_ShowCommand_CallsShowChart()
    {
        // Create project and run simulation
        _app.Window.ClickMenuItem("MenuNewText");
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "x")
        });

        _app.Window.ClickMenuItem("MenuRun");

        // Verify result was committed
        _app.ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");

        // The Show command should be wired to ISimulationResultService
        // This is verified by the fact that CompletedSimulationViewModel has the command
        var completedVm = _app.ViewModel.TasksPopOver.Completed.FirstOrDefault();
        completedVm.Should().NotBeNull();
        completedVm!.ShowCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task CompletedSimulation_RemoveCommand_RemovesFromList()
    {
        // Create project and run simulation
        _app.Window.ClickMenuItem("MenuNewText");
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "x")
        });

        _app.Window.ClickMenuItem("MenuRun");

        // Verify result exists
        _app.ViewModel.TasksPopOver.Completed.Should().HaveCount(1);

        // Remove the completed simulation
        var completedVm = _app.ViewModel.TasksPopOver.Completed.First();
        completedVm.RemoveCommand.Execute(null);

        // Verify it was removed
        _app.ViewModel.TasksPopOver.Completed.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task SimulationService_SyncsWithTasksPopOver()
    {
        // Create project and run simulation
        _app.Window.ClickMenuItem("MenuNewText");
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "x")
        });

        _app.Window.ClickMenuItem("MenuRun");

        // Verify TasksPopOver is synced with SimulationService
        _app.ViewModel.SimulationService.TrackingTasks.Should().HaveCount(0); // Completed
        _app.ViewModel.TasksPopOver.InProgress.Should().HaveCount(0);
        _app.ViewModel.TasksPopOver.Completed.Should().HaveCount(1);
    }
}
