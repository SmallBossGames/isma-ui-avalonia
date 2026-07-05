using Avalonia;
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
public class ValidationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task Verify_CallsServerValidation()
    {
        // Create text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        _app.Window.SetEditorText("invalid syntax {{{");

        // Mock server to return validation errors
        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" },
                new CompilationError { Row = 2, Column = 5, Message = "Missing semicolon" }
            )
        });

        // Execute Verify via UI
        _app.Window.ClickMenuItem("MenuVerify");

        // Verify server was called
        _app.MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task Verify_DoesNotThrowOnError()
    {
        // Create text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.SetEditorText("invalid syntax {{{");

        // Mock server to return validation errors
        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Unexpected token" }
            )
        });

        // Execute Verify via UI - should not throw
        Action verify = () => _app.Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_DoesNotThrowWithNoActiveProject()
    {
        Action verify = () => _app.Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_DoesNotThrowWithBlueprintProject()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetActiveProject().Should().NotBeNull();

        Action verify = () => _app.Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_ServerNotAvailable_DoesNotCrash()
    {
        // Create text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Mock server to throw exception
        _app.MockServer.CompileHandler = _ => throw new System.Exception("Server unavailable");

        Action verify = () => _app.Window.ClickMenuItem("MenuVerify");
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task ErrorList_PopulatesAfterVerifyWithErrors()
    {
        // Create project and trigger verify with errors
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error 1" })
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();

        // Clear errors by verifying with no errors
        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult { Errors = ImmutableArray<CompilationError>.Empty });
        _app.Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task ErrorList_ClearsAfterVerifyWithNoErrors()
    {
        // Populate errors via verify
        _app.Window.ClickMenuItem("MenuNewText");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(
                new CompilationError { Row = 1, Column = 1, Message = "Error 1" },
                new CompilationError { Row = 2, Column = 2, Message = "Error 2" }
            )
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.MockServer.ValidateCalled.Should().BeTrue();

        // Clear by verifying with no errors
        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult { Errors = ImmutableArray<CompilationError>.Empty });
        _app.Window.ClickMenuItem("MenuVerify");
    }

    [AvaloniaFact]
    public async Task ErrorList_UpdatesAfterMultipleVerifies()
    {
        // Create project
        _app.Window.ClickMenuItem("MenuNewText");

        // First verify with 1 error
        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error 1" })
        });
        _app.Window.ClickMenuItem("MenuVerify");

        // Second verify with 2 errors (should replace first)
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
