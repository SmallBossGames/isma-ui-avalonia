using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.Tests.Integration;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

public class TextEditorIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task SyntaxHighlighter_Can_Highlight_Empty_Source()
    {
        var tokens = await MockServer.HighlightSource("");
        tokens.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task SyntaxHighlighter_Can_Highlight_Valid_Source()
    {
        var source = "state TestState\nend";
        MockServer.HighlightHandler = _ => Task.FromResult(new SyntaxTokenDto[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword },
            new SyntaxTokenDto { Start = 6, Length = 10, Kind = SyntaxTokenKind.Text },
            new SyntaxTokenDto { Start = 17, Length = 3, Kind = SyntaxTokenKind.Keyword },
        });

        var tokens = await MockServer.HighlightSource(source);
        tokens.Should().NotBeEmpty();
        tokens.Length.Should().Be(3);
        tokens[0].Kind.Should().Be(SyntaxTokenKind.Keyword);
        tokens[1].Kind.Should().Be(SyntaxTokenKind.Text);
        tokens[2].Kind.Should().Be(SyntaxTokenKind.Keyword);
    }

    [AvaloniaFact]
    public async Task SyntaxHighlighter_Returns_Empty_For_Invalid_Source()
    {
        var source = "invalid$$syntax";
        MockServer.HighlightHandler = _ => Task.FromResult(Array.Empty<SyntaxTokenDto>());

        var tokens = await MockServer.HighlightSource(source);
        tokens.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task CompileModel_Can_Compile_Valid_Source()
    {
        var source = "state TestState\nend";
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { Errors = default });

        var result = await MockServer.CompileModel(source);
        result.Should().NotBeNull();
        MockServer.CompileCalled.Should().BeTrue();
        MockServer.LastCompileSource.Should().Be(source);
    }

    [AvaloniaFact]
    public async Task CompileModel_Returns_Errors_For_Invalid_Source()
    {
        var source = "invalid syntax here";
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = new[] { new CompilationError { Message = "Unexpected token" } }.ToImmutableArray()
        });

        var result = await MockServer.CompileModel(source);
        result.Errors.Should().NotBeEmpty();
        result.Errors.Length.Should().Be(1);
        result.Errors[0].Message.Should().Be("Unexpected token");
    }

    [AvaloniaFact]
    public async Task ValidateModel_Can_Validate_Valid_Source()
    {
        var source = "state TestState\nend";
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult { Errors = default });

        var result = await MockServer.ValidateModel(source);
        result.Should().NotBeNull();
        MockServer.ValidateCalled.Should().BeTrue();
        MockServer.LastValidateSource.Should().Be(source);
    }

    [AvaloniaFact]
    public async Task ValidateModel_Returns_Errors_For_Invalid_Source()
    {
        var source = "bad syntax";
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = new[] { new CompilationError { Message = "Syntax error" } }.ToImmutableArray()
        });

        var result = await MockServer.ValidateModel(source);
        result.Errors.Should().NotBeEmpty();
        result.Errors.Length.Should().Be(1);
        result.Errors[0].Message.Should().Be("Syntax error");
    }

    [AvaloniaFact]
    public async Task ModelErrorService_Can_Put_Error_List()
    {
        var errorList = ViewModel.ErrorList;
        errorList.Errors.Should().BeEmpty();

        var errors = new List<ErrorInfo>
        {
            new ErrorInfo { Message = "Error 1", Row = 1, Position = 0, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 2", Row = 2, Position = 5, FragmentName = "Main" }
        };

        errorList.PutErrorList(errors);
        errorList.Errors.Should().HaveCount(2);
        errorList.Errors[0].Message.Should().Be("Error 1");
        errorList.Errors[1].Message.Should().Be("Error 2");
    }

    [AvaloniaFact]
    public async Task ModelErrorService_Can_Clear_Errors()
    {
        var errorList = ViewModel.ErrorList;
        errorList.PutErrorList(new[]
        {
            new ErrorInfo { Message = "Error 1", Row = 1, Position = 0, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 2", Row = 2, Position = 5, FragmentName = "Main" }
        });
        errorList.Errors.Should().HaveCount(2);

        errorList.ClearErrors();
        errorList.Errors.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task ModelErrorService_Has_Zero_Error_Count_Initially()
    {
        ViewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task ModelErrorService_Has_Correct_Error_Count_After_Adding_Errors()
    {
        var errorList = ViewModel.ErrorList;
        errorList.PutErrorList(new[]
        {
            new ErrorInfo { Message = "Error 1", Row = 1, Position = 0, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 2", Row = 2, Position = 5, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 3", Row = 3, Position = 10, FragmentName = "Main" }
        });
        errorList.ErrorCount.Should().Be(3);
    }
}
