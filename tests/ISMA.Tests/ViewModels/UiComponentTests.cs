global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.App.Services;
using ISMA.App.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class UiComponentTests
{
    private static Mock<ISyntaxHighlighter> CreateSyntaxHighlighterMock()
    {
        var mock = new Mock<ISyntaxHighlighter>();
        mock.Setup(m => m.Highlight(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<ISMA.Domain.Dtos.SyntaxTokenDto>());
        return mock;
    }

    private static ProjectService CreateProjectService() => new(
        Mock.Of<IProjectFileService>(),
        Mock.Of<ISimulationServerFacade>(),
        Mock.Of<ITextEditorFactory>(),
        CreateSyntaxHighlighterMock().Object,
        Mock.Of<ISMA.App.Services.IProjectEditorPort>());

    private static SimulationServiceViewModel CreateSimulationService() => new(
        Mock.Of<ISimulationServerFacade>(),
        Mock.Of<IModelErrorService>(),
        Mock.Of<ISimulationResultService>(),
        new SimulationParametersViewModel());

    private static MainWindowViewModel CreateViewModel()
    {
        var projectService = CreateProjectService();
        var simulationService = CreateSimulationService();
        var errorList = new ErrorListViewModel();
        var simParams = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();
        var parametersStore = new Mock<ISimulationParametersStoreService>().Object;
        var syntaxHighlighter = CreateSyntaxHighlighterMock().Object;

        return new MainWindowViewModel(
            projectService,
            simulationService,
            errorList,
            simParams,
            tasksPopOver,
            parametersStore,
            Mock.Of<IModelErrorService>(),
            syntaxHighlighter);
    }

    [Fact]
    public void MainWindow_HasEmptyProjectsList()
    {
        var viewModel = CreateViewModel();
        viewModel.Projects.Should().BeEmpty();
    }

    [Fact]
    public void MainWindow_HasNoActiveProject()
    {
        var viewModel = CreateViewModel();
        viewModel.ActiveProject.Should().BeNull();
    }

    [Fact]
    public void MainWindow_HasErrorListWithZeroErrors()
    {
        var viewModel = CreateViewModel();
        viewModel.ErrorList.Errors.Should().BeEmpty();
    }

    [Fact]
    public void MainWindow_HasSimulationParameters()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_HasTasksPopOver()
    {
        var viewModel = CreateViewModel();
        viewModel.TasksPopOver.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_TasksPopOverHasZeroProgress()
    {
        var viewModel = CreateViewModel();
        viewModel.TasksPopOver.InProgressCount.Should().Be(0);
        viewModel.TasksPopOver.CompletedCount.Should().Be(0);
    }

    [Fact]
    public void MainWindow_ErrorListHasZeroErrorCount()
    {
        var viewModel = CreateViewModel();
        viewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void MainWindow_SimulationParametersHasAllSections()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
        viewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
        viewModel.SimulationParameters.EventDetection.Should().NotBeNull();
        viewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
    }
}
