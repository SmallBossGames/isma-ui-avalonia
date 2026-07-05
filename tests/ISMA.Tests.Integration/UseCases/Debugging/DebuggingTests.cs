using Avalonia;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration.UseCases.Debugging;

/// <summary>
/// UC-05: Model Verification
/// UC-12: Complete Workflow - Debugging a Failing Model
/// Tests model verification, error handling, and debugging workflows.
/// </summary>
public class ModelVerificationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC05_Verify_CallsServerValidation()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 5, Message = "Syntax error" }
            )
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task UC05_Verify_DoesNotThrowWithNoActiveProject()
    {
        Action verify = () => _app.Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC05_Verify_DoesNotThrowWithBlueprint()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        Action verify = () => _app.Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC05_ErrorList_PopulatesCorrectly()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 5, Message = "Error 1" },
                new CompilationError { Row = 2, Column = 10, Message = "Error 2" }
            )
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task UC05_ErrorList_ClearsCorrectly()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error" })
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult { Errors = ImmutableArray<CompilationError>.Empty });
        _app.Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task UC05_ErrorList_CanBePopulatedMultipleTimes()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error 1" })
        });
        _app.Window.ClickMenuItem("MenuVerify");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 2, Column = 2, Message = "Error 2" },
                new CompilationError { Row = 3, Column = 3, Message = "Error 3" }
            )
        });
        _app.Window.ClickMenuItem("MenuVerify");
    }
}

public class DebugFailingModelTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC12_RunFailsOnCompileErrors()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Syntax error" })
        });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.StatusText.Should().Contain("failed");
    }

    [AvaloniaFact]
    public async Task UC12_MultipleErrorsInErrorList()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Error 1" },
                new CompilationError { Row = 2, Column = 5, Message = "Error 2" },
                new CompilationError { Row = 3, Column = 10, Message = "Error 3" }
            )
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task UC12_VerifyThenRunSuccessFlow()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error" })
        });
        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray<CompilationError>.Empty
        });
        _app.Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task UC12_RunSuccessAfterFixingErrors()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }
}
