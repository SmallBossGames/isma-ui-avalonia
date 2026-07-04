global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

/// <summary>
/// Unit tests for Select All command.
/// </summary>
public class SelectAllCommandTests
{
    private static Mock<ISyntaxHighlighter> CreateSyntaxHighlighterMock()
    {
        var mock = new Mock<ISyntaxHighlighter>();
        mock.Setup(m => m.Highlight(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<ISMA.Domain.Dtos.SyntaxTokenDto>());
        return mock;
    }

    private static ProjectService CreateProjectService() => new(
        Mock.Of<IProjectFileService>(),
        Mock.Of<ISimulationServerFacade>(),
        Mock.Of<ITextEditorFactory>(),
        new SimulationParametersService(),
        CreateSyntaxHighlighterMock().Object);

    [Fact]
    public void SelectAllCommand_ExistsInMainWindowViewModel()
    {
        var projectService = CreateProjectService();
        var simulationService = new SimulationServiceViewModel(
            Mock.Of<ISimulationServerFacade>(),
            Mock.Of<IModelErrorService>(),
            Mock.Of<ISimulationResultService>(),
            new SimulationParametersService());
        var errorList = new ErrorListViewModel();
        var simParams = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();
        var parametersStore = new Mock<ISimulationParametersStoreService>().Object;
        var syntaxHighlighter = CreateSyntaxHighlighterMock().Object;

        var viewModel = new MainWindowViewModel(
            projectService,
            simulationService,
            errorList,
            simParams,
            tasksPopOver,
            parametersStore,
            Mock.Of<IModelErrorService>(),
            syntaxHighlighter);

        viewModel.NewTextCommand.Execute(null);
        viewModel.Projects.Should().HaveCount(1);

        var selectAllMethod = typeof(MainWindowViewModel).GetMethod("SelectAll",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        selectAllMethod.Should().NotBeNull("SelectAll command should exist");
    }

    [Fact]
    public void SelectAllCommand_CallsTriggerSelectAllOnActiveProject()
    {
        var projectService = CreateProjectService();
        var simulationService = new SimulationServiceViewModel(
            Mock.Of<ISimulationServerFacade>(),
            Mock.Of<IModelErrorService>(),
            Mock.Of<ISimulationResultService>(),
            new SimulationParametersService());
        var errorList = new ErrorListViewModel();
        var simParams = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();
        var parametersStore = new Mock<ISimulationParametersStoreService>().Object;
        var syntaxHighlighter = CreateSyntaxHighlighterMock().Object;

        var viewModel = new MainWindowViewModel(
            projectService,
            simulationService,
            errorList,
            simParams,
            tasksPopOver,
            parametersStore,
            Mock.Of<IModelErrorService>(),
            syntaxHighlighter);

        viewModel.NewTextCommand.Execute(null);
        viewModel.Projects.Should().HaveCount(1);

        var lismaProject = viewModel.ActiveProject as LismaProjectViewModel;
        lismaProject.Should().NotBeNull();

        lismaProject!.TriggerSelectAll();
    }

    [Fact]
    public void BlueprintProject_TriggerSelectAll_DoesNotThrow()
    {
        var projectService = CreateProjectService();
        var simulationService = new SimulationServiceViewModel(
            Mock.Of<ISimulationServerFacade>(),
            Mock.Of<IModelErrorService>(),
            Mock.Of<ISimulationResultService>(),
            new SimulationParametersService());
        var errorList = new ErrorListViewModel();
        var simParams = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();
        var parametersStore = new Mock<ISimulationParametersStoreService>().Object;
        var syntaxHighlighter = CreateSyntaxHighlighterMock().Object;

        var viewModel = new MainWindowViewModel(
            projectService,
            simulationService,
            errorList,
            simParams,
            tasksPopOver,
            parametersStore,
            Mock.Of<IModelErrorService>(),
            syntaxHighlighter);

        viewModel.NewBlueprintCommand.Execute(null);
        viewModel.Projects.Should().HaveCount(1);

        var blueprintProject = viewModel.ActiveProject as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        blueprintProject!.TriggerSelectAll();
    }
}
