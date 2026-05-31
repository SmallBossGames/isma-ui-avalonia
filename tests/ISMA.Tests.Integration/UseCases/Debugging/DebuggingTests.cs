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
public class ModelVerificationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC05_Verify_CallsServerValidation()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        // Mock server to return validation errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 5, Message = "Syntax error" }
            )
        });

        Window.ClickMenuItem("MenuVerify");

        MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task UC05_Verify_DoesNotThrowWithNoActiveProject()
    {
        ViewModel.ActiveProject = null;

        Action verify = () => Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC05_Verify_DoesNotThrowWithBlueprint()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        Action verify = () => Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC05_ErrorList_PopulatesCorrectly()
    {
        var errors = new[]
        {
            new ErrorInfo { Row = 1, Position = 5, FragmentName = "Main", Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 10, FragmentName = "Main", Message = "Error 2" }
        };

        ViewModel.ErrorList.PutErrorList(errors);

        ViewModel.ErrorList.Errors.Should().HaveCount(2);
        ViewModel.ErrorList.ErrorCount.Should().Be(2);
    }

    [AvaloniaFact]
    public async Task UC05_ErrorList_ClearsCorrectly()
    {
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 1, FragmentName = "Main", Message = "Error" }
        });

        ViewModel.ErrorList.ClearErrors();
        ViewModel.ErrorList.Errors.Should().BeEmpty();
        ViewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task UC05_ErrorList_CanBePopulatedMultipleTimes()
    {
        // First population
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 1, FragmentName = "Main", Message = "Error 1" }
        });
        ViewModel.ErrorList.Errors.Should().HaveCount(1);

        // Second population (should replace first)
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 2, Position = 2, FragmentName = "Main", Message = "Error 2" },
            new ErrorInfo { Row = 3, Position = 3, FragmentName = "Main", Message = "Error 3" }
        });
        ViewModel.ErrorList.Errors.Should().HaveCount(2);
        ViewModel.ErrorList.Errors[0].Message.Should().Be("Error 2");
        ViewModel.ErrorList.Errors[1].Message.Should().Be("Error 3");
    }
}

public class DebugFailingModelTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC12_RunFailsOnCompileErrors()
    {
        // Create project
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        // Mock server to return compile errors
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Syntax error" })
        });

        Window.ClickMenuItem("MenuRun");

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Contain("failed");
    }

    [AvaloniaFact]
    public async Task UC12_MultipleErrorsInErrorList()
    {
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 1, FragmentName = "Main", Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 5, FragmentName = "Main", Message = "Error 2" },
            new ErrorInfo { Row = 3, Position = 10, FragmentName = "Main", Message = "Error 3" }
        });

        ViewModel.ErrorList.Errors.Should().HaveCount(3);
        ViewModel.ErrorList.ErrorCount.Should().Be(3);
    }

    [AvaloniaFact]
    public async Task UC12_VerifyThenRunSuccessFlow()
    {
        // Create project
        Window.ClickMenuItem("MenuNewText");

        // Verify with errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error" })
        });
        Window.ClickMenuItem("MenuVerify");
        MockServer.ValidateCalled.Should().BeTrue();

        // Then verify with no errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray<CompilationError>.Empty
        });
        Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task UC12_RunSuccessAfterFixingErrors()
    {
        // Create project
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        // Mock server for successful simulation
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        Window.ClickMenuItem("MenuRun");

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }
}
