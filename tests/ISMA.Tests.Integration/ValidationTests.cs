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

        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project!.FullText = "invalid syntax {{{";

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
        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project!.FullText = "invalid syntax {{{";

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
        ViewModel.ActiveProject = null;

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
    public async Task ErrorList_CanBePopulatedAndCleared()
    {
        // Populate error list
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 1, FragmentName = "Main", Message = "Error 1" }
        });
        ViewModel.ErrorList.Errors.Should().HaveCount(1);

        // Clear manually
        ViewModel.ErrorList.ClearErrors();
        ViewModel.ErrorList.Errors.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task ErrorList_CanBeClearedManually()
    {
        // Populate error list
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 1, FragmentName = "Main", Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 2, FragmentName = "Main", Message = "Error 2" }
        });
        ViewModel.ErrorList.Errors.Should().HaveCount(2);

        // Clear manually
        ViewModel.ErrorList.ClearErrors();

        ViewModel.ErrorList.Errors.Should().BeEmpty();
        ViewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task ErrorList_CanBePopulatedMultipleTimes()
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
