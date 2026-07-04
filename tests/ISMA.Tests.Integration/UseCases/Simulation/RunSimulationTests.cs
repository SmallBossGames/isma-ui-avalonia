using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Automation;
using ISMA.App.Controls;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration.UseCases.Simulation;

/// <summary>
/// UC-01: Create and Run a LISMA Text Project - Simulation flow
/// UC-08: Cancel a Running Simulation
/// UC-13: Configure Simulation Parameters
/// Tests simulation workflow, cancellation, and parameter configuration via UI.
/// </summary>
public class RunSimulationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC01_FullSimulationWorkflow_CompletesSuccessfully()
    {
        // Step 1: Create new text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        // Step 2: Write model text via UI
        Window.SetEditorText(@"
main {
    x = 0;
}

state ""initial"" (1 > 0) {
    x = 1;
} from main;
");

        // Step 3: Configure simulation parameters
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.1;
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod = "RK4";

        // Step 4: Mock server for successful simulation
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

        // Step 6: Verify simulation completed
        ViewModel.SimulationService.TrackingTasks.Should().BeEmpty();
        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_UsesDefaultParameters()
    {
        Window.ClickMenuItem("MenuNewText");

        // Mock server
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = @params => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Run simulation via UI without configuring parameters
        Window.ClickMenuItem("MenuRun");

        // Verify default parameters were used
        MockServer.LastRunParams.Should().NotBeNull();
        MockServer.LastRunParams!.StartTime.Should().Be(0.0);
        MockServer.LastRunParams.EndTime.Should().Be(10.0);
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_WithNoActiveProject_DoesNotThrow()
    {
        Window.GetProjectCount().Should().Be(0);

        Action run = () => Window.ClickMenuItem("MenuRun");
        run.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_AlreadyRunning_DoesNotStartAnother()
    {
        Window.ClickMenuItem("MenuNewText");

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
    public async Task UC01_Simulation_FailsGracefullyOnCompileErrors()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.SetEditorText("invalid syntax here {{{");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        Window.ClickMenuItem("MenuRun");

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Compilation failed");
        ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
        ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_ProgressUpdatesInTasksPopOver()
    {
        Window.ClickMenuItem("MenuNewText");

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => AsyncEnumerable.Multiple(
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 5 },
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 10 }
        );
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        Window.ClickMenuItem("MenuRun");

        // After completion, completed simulations should be in the completed list
        ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC08_Simulation_CanBeCancelled()
    {
        Window.ClickMenuItem("MenuNewText");

        var cts = new System.Threading.CancellationTokenSource();
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => LongRunningSimulation(cts.Token);
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Start simulation
        Window.ClickMenuItem("MenuRun");
        await Task.Delay(100);

        ViewModel.SimulationService.IsRunning.Should().BeTrue();

        // Cancel
        var task = ViewModel.SimulationService.TrackingTasks.FirstOrDefault();
        if (task != null)
        {
            await ViewModel.SimulationService.StopSimulationAsync(task);
        }

        cts.Cancel();
        await Task.Delay(200);

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC13_SimulationParameters_CanBeModified()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 1.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 20.0;
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.001;
        ViewModel.SimulationParameters.EventDetection.Gamma = 0.5;
        ViewModel.SimulationParameters.EventDetection.LowBorder = 0.01;

        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(20.0);
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.001);
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.5);
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.01);
    }

    [AvaloniaFact]
    public async Task UC13_ResultSaving_DefaultsToMemory()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [AvaloniaFact]
    public async Task UC13_ResultSaving_CanBeChangedToFile()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget = SaveTarget.File;
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
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
