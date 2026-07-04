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
public class SimulationWorkflowTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task FullSimulationWorkflow_CompletesSuccessfully()
    {
        // Step 1: Create new text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().NotBeNull();

        // Step 2: Write model text using LISMA language via UI
        Window.GetActiveProject().Should().NotBeNull();
        Window.SetEditorText(@"
main {
    x = 0;
}

state ""initial"" (1 > 0) {
    x = 1;
} from main;
");

        // Step 3: Setup simulation parameters
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.1;
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod = "RK4";
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.001;

        // Step 4: Mock server to simulate successful compilation and run
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model-123" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => AsyncEnumerable.Multiple(
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 5 },
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 10 }
        );
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/test-result.bin",
            ColumnNames = ImmutableArray.Create("time", "x", "y")
        });

        // Step 5: Run simulation via UI
        Window.ClickMenuItem("MenuRun");

        // Step 6: Watch simulation progress
        ViewModel.SimulationService.TrackingTasks.Should().BeEmpty(); // Progress completed
        ViewModel.SimulationService.IsRunning.Should().BeFalse();

        // Step 7: Check simulation completeness
        ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");

        // Step 8: Verify project state
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task Simulation_FailsGracefullyOnCompileErrors()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        // Write invalid LISMA code via UI
        Window.GetActiveProject().Should().NotBeNull();
        Window.SetEditorText("invalid syntax here {{{");

        // Mock server to return compilation errors
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        // Run simulation via UI (should fail at compile step)
        Window.ClickMenuItem("MenuRun");

        // Verify graceful failure
        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Compilation failed");
        ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
        ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task Simulation_CanBeCancelled()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        // Mock server to simulate a long-running simulation
        var cts = new System.Threading.CancellationTokenSource();
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => LongRunningSimulation(cts.Token);
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Start simulation via UI
        Window.ClickMenuItem("MenuRun");

        // Wait for simulation to start
        await Task.Delay(100);
        ViewModel.SimulationService.IsRunning.Should().BeTrue();

        // Cancel the simulation (no UI cancel button exists, so we use the service directly)
        var task = ViewModel.SimulationService.TrackingTasks.FirstOrDefault();
        if (task is not null)
        {
            ViewModel.SimulationService.StopSimulationAsync(task).Wait();
        }

        // Cancel the coroutine
        cts.Cancel();

        // Wait a bit for cancellation to propagate
        await Task.Delay(100);

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task Simulation_WithNoActiveProject_DoesNotThrow()
    {
        // Ensure no project is open (fresh window state)
        Window.GetProjectCount().Should().Be(0);

        Action run = () => Window.ClickMenuItem("MenuRun");
        run.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Simulation_AlreadyRunning_DoesNotStartAnother()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        // Mock server
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Set running manually
        ViewModel.SimulationService.IsRunning = true;

        // Try to run again via UI
        Window.ClickMenuItem("MenuRun");

        // Should still be running (not started another)
        ViewModel.SimulationService.IsRunning.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task Simulation_UsesDefaultParameters()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        // Mock server
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = @params => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Run simulation via UI
        Window.ClickMenuItem("MenuRun");

        // Verify simulation was called with default parameters
        MockServer.LastRunParams.Should().NotBeNull();
        MockServer.LastRunParams!.StartTime.Should().Be(0.0); // Default from SimulationParametersService
        MockServer.LastRunParams.EndTime.Should().Be(10.0); // Default from SimulationParametersService
    }

    [AvaloniaFact]
    public async Task BlueprintWorkflow_ConvertsToLisma()
    {
        // Step 1: Create new blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().NotBeNull();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // Step 2: Add states and transitions via the editor
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        editorVm.Should().NotBeNull();
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3); // Main, Init, New state 1

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
}
