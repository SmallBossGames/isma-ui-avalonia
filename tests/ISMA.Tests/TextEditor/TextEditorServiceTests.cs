global using global::Xunit;
using System.Collections.Immutable;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;
using Moq;
using SyntaxTokenDto = ISMA.Domain.Dtos.SyntaxTokenDto;

namespace ISMA.Tests.TextEditor;

public class TextEditorServiceTests
{
    private static Mock<ISyntaxHighlighter> CreateSyntaxHighlighterMock()
    {
        var mock = new Mock<ISyntaxHighlighter>();
        mock.Setup(m => m.Highlight(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<SyntaxTokenDto>());
        return mock;
    }

    private static Mock<ISimulationServerFacade> CreateFacadeMock()
    {
        var mock = new Mock<ISimulationServerFacade>();
        mock.Setup(f => f.HighlightSource(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<SyntaxTokenDto>());
        mock.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { Errors = default });
        mock.Setup(f => f.ValidateModel(It.IsAny<string>()))
            .ReturnsAsync(new ValidationResult { Errors = default });
        return mock;
    }

    [Fact]
    public async Task SyntaxHighlighter_Can_Highlight_Empty_Source()
    {
        var mockFacade = CreateFacadeMock();
        var tokens = await mockFacade.Object.HighlightSource("");
        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task SyntaxHighlighter_Can_Highlight_Valid_Source()
    {
        var mockFacade = CreateFacadeMock();
        var expectedTokens = new SyntaxTokenDto[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword },
            new SyntaxTokenDto { Start = 6, Length = 10, Kind = SyntaxTokenKind.Text },
            new SyntaxTokenDto { Start = 17, Length = 3, Kind = SyntaxTokenKind.Keyword },
        };
        mockFacade.Setup(f => f.HighlightSource(It.IsAny<string>()))
            .ReturnsAsync(expectedTokens);

        var source = "state TestState\nend";
        var tokens = await mockFacade.Object.HighlightSource(source);
        tokens.Should().NotBeEmpty();
        tokens.Length.Should().Be(3);
        tokens[0].Kind.Should().Be(SyntaxTokenKind.Keyword);
        tokens[1].Kind.Should().Be(SyntaxTokenKind.Text);
        tokens[2].Kind.Should().Be(SyntaxTokenKind.Keyword);
    }

    [Fact]
    public async Task SyntaxHighlighter_Returns_Empty_For_Invalid_Source()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        mockFacade.Setup(f => f.HighlightSource(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<SyntaxTokenDto>());

        var source = "invalid$$syntax";
        var tokens = await mockFacade.Object.HighlightSource(source);
        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task CompileModel_Can_Compile_Valid_Source()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { Errors = default });

        var source = "state TestState\nend";
        var result = await mockFacade.Object.CompileModel(source);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CompileModel_Returns_Errors_For_Invalid_Source()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult
            {
                Errors = new[] { new CompilationError { Message = "Unexpected token" } }.ToImmutableArray()
            });

        var source = "invalid syntax here";
        var result = await mockFacade.Object.CompileModel(source);
        result.Errors.Should().NotBeEmpty();
        result.Errors.Length.Should().Be(1);
        result.Errors[0].Message.Should().Be("Unexpected token");
    }

    [Fact]
    public async Task ValidateModel_Can_Validate_Valid_Source()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        mockFacade.Setup(f => f.ValidateModel(It.IsAny<string>()))
            .ReturnsAsync(new ValidationResult { Errors = default });

        var source = "state TestState\nend";
        var result = await mockFacade.Object.ValidateModel(source);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateModel_Returns_Errors_For_Invalid_Source()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        mockFacade.Setup(f => f.ValidateModel(It.IsAny<string>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = new[] { new CompilationError { Message = "Syntax error" } }.ToImmutableArray()
            });

        var source = "bad syntax";
        var result = await mockFacade.Object.ValidateModel(source);
        result.Errors.Should().NotBeEmpty();
        result.Errors.Length.Should().Be(1);
        result.Errors[0].Message.Should().Be("Syntax error");
    }

    [Fact]
    public void ModelErrorService_Can_Put_Error_List()
    {
        var errorList = new ErrorListViewModel();
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

    [Fact]
    public void ModelErrorService_Can_Clear_Errors()
    {
        var errorList = new ErrorListViewModel();
        errorList.PutErrorList(new[]
        {
            new ErrorInfo { Message = "Error 1", Row = 1, Position = 0, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 2", Row = 2, Position = 5, FragmentName = "Main" }
        });
        errorList.Errors.Should().HaveCount(2);

        errorList.ClearErrors();
        errorList.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ModelErrorService_Has_Zero_Error_Count_Initially()
    {
        var errorList = new ErrorListViewModel();
        errorList.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void ModelErrorService_Has_Correct_Error_Count_After_Adding_Errors()
    {
        var errorList = new ErrorListViewModel();
        errorList.PutErrorList(new[]
        {
            new ErrorInfo { Message = "Error 1", Row = 1, Position = 0, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 2", Row = 2, Position = 5, FragmentName = "Main" },
            new ErrorInfo { Message = "Error 3", Row = 3, Position = 10, FragmentName = "Main" }
        });
        errorList.ErrorCount.Should().Be(3);
    }
}
