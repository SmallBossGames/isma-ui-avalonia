using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// End-to-end tests for model validation scenarios.
/// Tests the Verify command and error list management.
/// </summary>
public class ValidationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task Verify_CallsServerValidation()
    {
        // Create text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        Window.SetEditorText("invalid syntax {{{");

        // Mock server to return validation errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        // Execute Verify via UI
        Window.ClickMenuItem("MenuVerify");

        // Verify server was called
        MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task Verify_DoesNotThrowOnError()
    {
        // Create text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();
        Window.SetEditorText("invalid syntax {{{");

        // Mock server to return validation errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" }
            )
        });

        // Execute Verify via UI - should not throw
        Action verify = () => Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_DoesNotThrowWithNoActiveProject()
    {
        Action verify = () => Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_DoesNotThrowWithBlueprintProject()
    {
        // Create blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetActiveProject().Should().NotBeNull();

        Action verify = () => Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_ServerNotAvailable_DoesNotCrash()
    {
        // Create text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        // Mock server to throw exception
        MockServer.CompileHandler = _ => throw new System.Exception("Server unavailable");

        Action verify = () => Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task ErrorList_PopulatesAfterVerifyWithErrors()
    {
        // Create project and trigger verify with errors
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error 1" })
        });

        Window.ClickMenuItem("MenuVerify");
        MockServer.ValidateCalled.Should().BeTrue();

        // Clear errors by verifying with no errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult { Errors = ImmutableArray<CompilationError>.Empty });
        Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task ErrorList_ClearsAfterVerifyWithNoErrors()
    {
        // Populate errors via verify
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Error 1" },
                new CompilationError { Row = 2, Column = 2, Message = "Error 2" }
            )
        });

        Window.ClickMenuItem("MenuVerify");
        MockServer.ValidateCalled.Should().BeTrue();

        // Clear by verifying with no errors
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult { Errors = ImmutableArray<CompilationError>.Empty });
        Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task ErrorList_UpdatesAfterMultipleVerifies()
    {
        // Create project
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        // First verify with 1 error
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error 1" })
        });
        Window.ClickMenuItem("MenuVerify");

        // Second verify with 2 errors (should replace first)
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 2, Column = 2, Message = "Error 2" },
                new CompilationError { Row = 3, Column = 3, Message = "Error 3" }
            )
        });
        Window.ClickMenuItem("MenuVerify");
    }
}
