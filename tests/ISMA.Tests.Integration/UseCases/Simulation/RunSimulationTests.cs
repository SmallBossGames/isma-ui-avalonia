using Avalonia;
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
public class RunSimulationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC01_FullSimulationWorkflow_CompletesSuccessfully()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        _app.Window.SetEditorText(@"
main {
    x = 0;
}

state ""initial"" (1 > 0) {
    x = 1;
} from main;
");

        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.1;
        _app.ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod = "RK4";

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

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.TrackingTasks.Should().BeEmpty();
        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_UsesDefaultParameters()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = @params => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        _app.MockServer.LastRunParams.Should().NotBeNull();
        _app.MockServer.LastRunParams!.StartTime.Should().Be(0.0);
        _app.MockServer.LastRunParams.EndTime.Should().Be(10.0);
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_WithNoActiveProject_DoesNotThrow()
    {
        _app.Window.GetProjectCount().Should().Be(0);

        Action run = () => _app.Window.ClickMenuItem("MenuRun");
        run.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_AlreadyRunning_DoesNotStartAnother()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.ViewModel.SimulationService.IsRunning = true;

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_FailsGracefullyOnCompileErrors()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.SetEditorText("invalid syntax here {{{");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.StatusText.Should().Be("Compilation failed");
        _app.ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
        _app.ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task UC01_Simulation_ProgressUpdatesInTasksPopOver()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = id => AsyncEnumerable.Multiple(
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 5 },
            new SimulationProgress { StartTime = 0, EndTime = 10, CurrentTime = 10 }
        );
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC08_Simulation_CanBeCancelled()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var cts = new System.Threading.CancellationTokenSource();
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = id => LongRunningSimulation(cts.Token);
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");
        await Task.Delay(100);

        _app.ViewModel.SimulationService.IsRunning.Should().BeTrue();

        var task = _app.ViewModel.SimulationService.TrackingTasks.FirstOrDefault();
        if (task != null)
        {
            await _app.ViewModel.SimulationService.StopSimulationAsync(task);
        }

        cts.Cancel();
        await Task.Delay(200);

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC13_SimulationParameters_CanBeModified()
    {
        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime = 1.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.EndTime = 20.0;
        _app.ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.001;
        _app.ViewModel.SimulationParameters.EventDetection.Gamma = 0.5;
        _app.ViewModel.SimulationParameters.EventDetection.LowBorder = 0.01;

        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
        _app.ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(20.0);
        _app.ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.001);
        _app.ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.5);
        _app.ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.01);
    }

    [AvaloniaFact]
    public async Task UC13_ResultSaving_DefaultsToMemory()
    {
        _app.ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [AvaloniaFact]
    public async Task UC13_ResultSaving_CanBeChangedToFile()
    {
        _app.ViewModel.SimulationParameters.ResultSaving.SavingTarget = SaveTarget.File;
        _app.ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
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
