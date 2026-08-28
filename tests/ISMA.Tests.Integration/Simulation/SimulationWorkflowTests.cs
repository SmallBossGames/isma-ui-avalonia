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
/// End-to-end tests for the complete simulation workflow.
/// Tests the full business scenario from project creation to simulation results.
/// </summary>
public class SimulationWorkflowTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task FullSimulationWorkflow_CompletesSuccessfully()
    {
        // Step 1: Create new text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Step 2: Write model text using LISMA language via UI
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.SetEditorText(@"
main {
    x = 0;
}

state ""initial"" (1 > 0) {
    x = 1;
} from main;
");

        // Step 3: Setup simulation parameters
        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.1;
        _app.ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod = "RK4";
        _app.ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.001;

        // Step 4: Mock server to simulate successful compilation and run
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model-123" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = id => AsyncEnumerable.Multiple(
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 5 },
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 10 }
        );
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/test-result.bin",
            ColumnNames = ImmutableArray.Create("time", "x", "y")
        });

        // Step 5: Run simulation via UI
        _app.Window.ClickMenuItem("MenuRun");

        // Step 6: Watch simulation progress
        _app.ViewModel.SimulationService.TrackingTasks.Should().BeEmpty(); // Progress completed
        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();

        // Step 7: Check simulation completeness
        _app.ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");

        // Step 8: Verify the live settings panel values reached the server
        _app.MockServer.LastRunParams.Should().NotBeNull();
        _app.MockServer.LastRunParams!.StartTime.Should().Be(0.0);
        _app.MockServer.LastRunParams.EndTime.Should().Be(10.0);
        _app.MockServer.LastRunParams.InitialStep.Should().Be(0.1);
        _app.MockServer.LastRunParams.MethodName.Should().Be("RK4");
        _app.MockServer.LastRunParams.Accuracy.Should().Be(0.001);

        // Step 9: Verify project state
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task Simulation_MonitorFailure_TaskAppearsInFailedSection()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => throw new Exception("monitor down");

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.TrackingTasks.Should().BeEmpty();
        _app.ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
        _app.ViewModel.TasksPopOver.FailedCount.Should().Be(1);
        _app.ViewModel.TasksPopOver.Failed[0].ErrorText.Should().Be("Monitor error: monitor down");
    }

    [AvaloniaFact]
    public async Task Simulation_FailsGracefullyOnCompileErrors()
    {
        // Create project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Write invalid LISMA code via UI
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.SetEditorText("invalid syntax here {{{");

        // Mock server to return compilation errors
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        // Run simulation via UI (should fail at compile step)
        _app.Window.ClickMenuItem("MenuRun");

        // Verify graceful failure
        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.StatusText.Should().Be("Compilation failed");
        _app.ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
        _app.ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task Simulation_CanBeCancelled()
    {
        // Create project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Mock server to simulate a long-running simulation
        var cts = new System.Threading.CancellationTokenSource();
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = id => LongRunningSimulation(cts.Token);
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Start simulation via UI
        _app.Window.ClickMenuItem("MenuRun");

        // Wait for simulation to start
        await Task.Delay(100);
        _app.ViewModel.SimulationService.IsRunning.Should().BeTrue();

        // Cancel the simulation (no UI cancel button exists, so we use the service directly)
        var task = _app.ViewModel.SimulationService.TrackingTasks.FirstOrDefault();
        if (task is not null)
        {
            _app.ViewModel.SimulationService.StopSimulationAsync(task).Wait();
        }

        // Cancel the coroutine
        cts.Cancel();

        // Wait a bit for cancellation to propagate
        await Task.Delay(100);

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task Simulation_WithNoActiveProject_DoesNotThrow()
    {
        // Ensure no project is open (fresh window state)
        _app.Window.GetProjectCount().Should().Be(0);

        Action run = () => _app.Window.ClickMenuItem("MenuRun");
        run.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Simulation_AlreadyRunning_StartsAnother()
    {
        // Create project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Mock server; the monitor yields one progress then stalls so the
        // simulation stays in the "running" state, letting a second start.
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => StallingMonitor();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Start first simulation via UI
        _app.Window.ClickMenuItem("MenuRun");
        await WaitForCount(_app.ViewModel.SimulationService.TrackingTasks, 1);

        // Start a second simulation while the first is still running (concurrent).
        // Execute the Run command directly (reliable re-invocation in headless mode).
        var runItem = _app.Window.FindMenuItem("MenuRun")!;
        runItem.Command!.Execute(runItem.CommandParameter);
        await WaitForCount(_app.ViewModel.SimulationService.TrackingTasks, 2);

        _app.ViewModel.SimulationService.TrackingTasks.Count.Should().Be(2);
    }

    private static async IAsyncEnumerable<SimulationProgress> StallingMonitor()
    {
        yield return new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 5 };
        await Task.Delay(30_000);
    }

    [AvaloniaFact]
    public async Task Simulation_UsesDefaultParameters()
    {
        // Create project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Mock server
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = @params => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Run simulation via UI
        _app.Window.ClickMenuItem("MenuRun");

        // Verify simulation was called with default parameters
        _app.MockServer.LastRunParams.Should().NotBeNull();
        _app.MockServer.LastRunParams!.StartTime.Should().Be(0.0); // Default from SimulationParametersService
        _app.MockServer.LastRunParams.EndTime.Should().Be(10.0); // Default from SimulationParametersService
    }

    [AvaloniaFact]
    public async Task BlueprintWorkflow_ConvertsToLisma()
    {
        // Step 1: Create new blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().NotBeNull();

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // Step 2: Add states and transitions via the editor
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        editorVm.Should().NotBeNull();
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(1); // State 1 (Main and Init are not in States)

        // Step 3: Convert to LISMA text
        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    private static async IAsyncEnumerable<SimulationProgress> LongRunningSimulation([System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken token)
    {
        for (double t = 0; t <= 10 && !token.IsCancellationRequested; t += 0.1)
        {
            await Task.Delay(10, token);
            yield return new SimulationProgress
            {
                StartTime = 0,
                EndTime = 10,
                CurrentTime = t
            };
        }
    }

    private static async Task WaitForCount(System.Collections.ObjectModel.ObservableCollection<InProgressSimulationViewModel> tasks, int expected)
    {
        for (var i = 0; i < 200 && tasks.Count < expected; i++)
        {
            await Task.Delay(20);
        }
    }
}
