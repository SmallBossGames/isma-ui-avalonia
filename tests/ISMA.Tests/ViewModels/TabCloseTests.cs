global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class TabCloseTests
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

    private static MainWindowViewModel CreateViewModel()
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
    public async Task TabCloseButton_ClosesSpecificTab()
    {
        var viewModel = CreateViewModel();
        await viewModel.NewTextCommand.ExecuteAsync(null);
        viewModel.Projects.Should().HaveCount(1);

        await viewModel.NewTextCommand.ExecuteAsync(null);
        viewModel.Projects.Should().HaveCount(2);

        var secondProject = viewModel.Projects[1];
        secondProject.Should().NotBeNull();

        await viewModel.CloseTabCommand.ExecuteAsync(secondProject);

        viewModel.Projects.Should().HaveCount(1);
        viewModel.Projects[0].Should().Be(viewModel.Projects.First());
    }

    [Fact]
    public async Task TabCloseButton_DisposesProject()
    {
        var viewModel = CreateViewModel();
        await viewModel.NewTextCommand.ExecuteAsync(null);
        viewModel.Projects.Should().HaveCount(1);

        var project = viewModel.Projects[0];
        project.Should().NotBeNull();

        await viewModel.CloseTabCommand.ExecuteAsync(project);

        viewModel.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task TabCloseButton_LastTab_CanStillClose()
    {
        var viewModel = CreateViewModel();
        await viewModel.NewTextCommand.ExecuteAsync(null);
        viewModel.Projects.Should().HaveCount(1);

        var project = viewModel.Projects[0];

        await viewModel.CloseTabCommand.ExecuteAsync(project);

        viewModel.Projects.Should().BeEmpty();
        viewModel.ActiveProject.Should().BeNull();
    }

    [Fact]
    public async Task TabCloseButton_CloseAll_ViaCloseAllCommand()
    {
        var viewModel = CreateViewModel();
        await viewModel.NewTextCommand.ExecuteAsync(null);
        await viewModel.NewTextCommand.ExecuteAsync(null);
        await viewModel.NewTextCommand.ExecuteAsync(null);
        viewModel.Projects.Should().HaveCount(3);

        await viewModel.CloseAllCommand.ExecuteAsync(null);

        viewModel.Projects.Should().BeEmpty();
    }
}
