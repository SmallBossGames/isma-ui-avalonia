global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.App.Services;
using ISMA.App.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class MainWindowTests
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
    public void MainWindow_Has_Empty_Projects_List()
    {
        var viewModel = CreateViewModel();
        viewModel.Projects.Should().BeEmpty();
    }

    [Fact]
    public void MainWindow_Has_No_Active_Project()
    {
        var viewModel = CreateViewModel();
        viewModel.ActiveProject.Should().BeNull();
    }

    [Fact]
    public void MainWindow_Has_ErrorList_With_Zero_Errors()
    {
        var viewModel = CreateViewModel();
        viewModel.ErrorList.Errors.Should().BeEmpty();
    }

    [Fact]
    public void MainWindow_Has_SimulationParameters()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_Has_TasksPopOver()
    {
        var viewModel = CreateViewModel();
        viewModel.TasksPopOver.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_SimulationParameters_Has_CauchyInitials()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_SimulationParameters_Has_IntegrationMethod()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_SimulationParameters_Has_EventDetection()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.EventDetection.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_SimulationParameters_Has_ResultSaving()
    {
        var viewModel = CreateViewModel();
        viewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_ShowSettings_Property_Exists()
    {
        var viewModel = CreateViewModel();
        viewModel.ShowSettings.Should().BeFalse();
        viewModel.ShowSettings = true;
        viewModel.ShowSettings.Should().BeTrue();
        viewModel.ShowSettings = false;
        viewModel.ShowSettings.Should().BeFalse();
    }

    [Fact]
    public void MainWindow_ErrorList_Has_Zero_Error_Count()
    {
        var viewModel = CreateViewModel();
        viewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void MainWindow_TasksPopOver_Has_Zero_Progress()
    {
        var viewModel = CreateViewModel();
        viewModel.TasksPopOver.InProgressCount.Should().Be(0);
        viewModel.TasksPopOver.CompletedCount.Should().Be(0);
    }
}
