using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

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
        // Step 1: Create new text project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.ActiveProject.Should().NotBeNull();

        // Step 2: Write model text using LISMA language
        var project = ViewModel.ActiveProject as LismaProjectViewModel;
        project.Should().NotBeNull();
        project!.FullText = @"
main {
    x = 0;
}

state ""initial"" (1 > 0) {
    x = 1;
} from main;
";
        project.FullText.Should().NotBeNullOrEmpty();

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

        // Step 5: Run simulation
        await ViewModel.RunCommand.ExecuteAsync(null);

        // Step 6: Watch simulation progress
        ViewModel.SimulationService.TrackingTasks.Should().BeEmpty(); // Progress completed
        ViewModel.SimulationService.IsRunning.Should().BeFalse();

        // Step 7: Check simulation completeness
        ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");

        // Step 8: Verify project state
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.ActiveProject.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task Simulation_FailsGracefullyOnCompileErrors()
    {
        // Create project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();

        // Write invalid LISMA code
        var project = ViewModel.ActiveProject as LismaProjectViewModel;
        project!.FullText = "invalid syntax here {{{";

        // Mock server to return compilation errors
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        // Run simulation (should fail at compile step)
        await ViewModel.RunCommand.ExecuteAsync(null);

        // Verify graceful failure
        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Compilation failed");
        ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
        ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task Simulation_CanBeCancelled()
    {
        // Create project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();

        // Mock server to simulate a long-running simulation
        var cts = new System.Threading.CancellationTokenSource();
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => LongRunningSimulation(cts.Token);
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Start simulation
        var simulationTask = ViewModel.RunCommand.ExecuteAsync(null);

        // Wait for simulation to start
        await Task.Delay(100);
        ViewModel.SimulationService.IsRunning.Should().BeTrue();

        // Cancel the simulation
        ViewModel.SimulationService.StopSimulationAsync(ViewModel.SimulationService.TrackingTasks.FirstOrDefault()).Wait();

        // Cancel the coroutine
        cts.Cancel();

        // Wait for simulation to finish
        await simulationTask;

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task Simulation_WithNoActiveProject_DoesNotThrow()
    {
        ViewModel.ActiveProject = null;

        Action run = () => ViewModel.RunCommand.Execute(null);
        run.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Simulation_AlreadyRunning_DoesNotStartAnother()
    {
        // Create project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();

        // Mock server
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Set running manually
        ViewModel.SimulationService.IsRunning = true;

        // Try to run again
        await ViewModel.RunCommand.ExecuteAsync(null);

        // Should still be running (not started another)
        ViewModel.SimulationService.IsRunning.Should().BeTrue();
    }

[AvaloniaFact]
    public async Task Simulation_UsesDefaultParameters()
    {
        // Create project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();

        // Mock server
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = @params => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Run simulation
        await ViewModel.RunCommand.ExecuteAsync(null);

        // Verify simulation was called with default parameters
        MockServer.LastRunParams.Should().NotBeNull();
        MockServer.LastRunParams!.StartTime.Should().Be(0.0); // Default from SimulationParametersService
        MockServer.LastRunParams.EndTime.Should().Be(10.0); // Default from SimulationParametersService
    }

    [AvaloniaFact]
    public async Task BlueprintWorkflow_ConvertsToLisma()
    {
        // Step 1: Create new blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.ActiveProject.Should().NotBeNull();

        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
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

    private static async IAsyncEnumerable<SimulationProgress> LongRunningSimulation(System.Threading.CancellationToken token)
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
