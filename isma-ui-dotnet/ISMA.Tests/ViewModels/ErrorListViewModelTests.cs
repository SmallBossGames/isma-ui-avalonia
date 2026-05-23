using Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels;

public class ErrorListViewModelTests
{
    [Fact]
    public void PutErrorList_AddsErrorsToList()
    {
        var viewModel = new ErrorListViewModel();

        var errors = new[]
        {
            new ErrorInfo { Row = 1, Position = 5, Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 10, Message = "Error 2" }
        };

        viewModel.PutErrorList(errors);

        viewModel.Errors.Should().HaveCount(2);
        viewModel.Errors[0].Message.Should().Be("Error 1");
        viewModel.Errors[1].Message.Should().Be("Error 2");
        viewModel.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void ClearErrors_RemovesAllErrors()
    {
        var viewModel = new ErrorListViewModel();

        viewModel.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 5, Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 10, Message = "Error 2" }
        });

        viewModel.Errors.Should().HaveCount(2);

        viewModel.ClearErrors();

        viewModel.Errors.Should().BeEmpty();
        viewModel.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void ClearAndRepopulate_ClearsThenAddsNewErrors()
    {
        var viewModel = new ErrorListViewModel();

        viewModel.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 5, Message = "Old Error" }
        });

        viewModel.Errors.Should().HaveCount(1);
        viewModel.Errors[0].Message.Should().Be("Old Error");

        viewModel.ClearErrors();
        viewModel.Errors.Should().BeEmpty();

        viewModel.PutErrorList(new[]
        {
            new ErrorInfo { Row = 3, Position = 15, Message = "New Error 1" },
            new ErrorInfo { Row = 4, Position = 20, Message = "New Error 2" }
        });

        viewModel.Errors.Should().HaveCount(2);
        viewModel.Errors[0].Message.Should().Be("New Error 1");
        viewModel.Errors[1].Message.Should().Be("New Error 2");
        viewModel.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void PutErrorList_EmptyCollection_ClearsErrors()
    {
        var viewModel = new ErrorListViewModel();

        viewModel.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 5, Message = "Error" }
        });

        viewModel.PutErrorList(Array.Empty<ErrorInfo>());

        viewModel.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ErrorCount_ReflectsCurrentCount()
    {
        var viewModel = new ErrorListViewModel();

        viewModel.ErrorCount.Should().Be(0);

        viewModel.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 5, Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 10, Message = "Error 2" },
            new ErrorInfo { Row = 3, Position = 15, Message = "Error 3" }
        });

        viewModel.ErrorCount.Should().Be(3);

        viewModel.ClearErrors();
        viewModel.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void PutErrorList_PreservesAllErrorProperties()
    {
        var viewModel = new ErrorListViewModel();

        var error = new ErrorInfo
        {
            Row = 42,
            Position = 99,
            FragmentName = "Main",
            Message = "Test error message"
        };

        viewModel.PutErrorList(new[] { error });

        viewModel.Errors[0].Row.Should().Be(42);
        viewModel.Errors[0].Position.Should().Be(99);
        viewModel.Errors[0].FragmentName.Should().Be("Main");
        viewModel.Errors[0].Message.Should().Be("Test error message");
    }
}
