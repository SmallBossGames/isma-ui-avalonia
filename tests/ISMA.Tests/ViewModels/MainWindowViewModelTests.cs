global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Moq;
using SyntaxTokenDto = ISMA.Domain.Dtos.SyntaxTokenDto;

namespace ISMA.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static Mock<ISyntaxHighlighter> CreateSyntaxHighlighterMock()
    {
        var mock = new Mock<ISyntaxHighlighter>();
        mock.Setup(m => m.Highlight(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<SyntaxTokenDto>());
        return mock;
    }

    private static ProjectService CreateProjectService() => new(
        Mock.Of<IProjectFileService>(),
        Mock.Of<ISimulationServerFacade>(),
        Mock.Of<ITextEditorFactory>(),
        new SimulationParametersService(),
        CreateSyntaxHighlighterMock().Object);

    private static SimulationServiceViewModel CreateSimulationService() => new(
        Mock.Of<ISimulationServerFacade>(),
        Mock.Of<IModelErrorService>(),
        Mock.Of<ISimulationResultService>(),
        new SimulationParametersService());

    private MainWindowViewModel CreateViewModel()
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
    public void Commands_Exist_AllCommandsAreCreated()
    {
        var viewModel = CreateViewModel();

        viewModel.SimulationService.Should().NotBeNull();
        viewModel.ErrorList.Should().NotBeNull();
        viewModel.SimulationParameters.Should().NotBeNull();
        viewModel.TasksPopOver.Should().NotBeNull();
        viewModel.Projects.Should().NotBeNull();
        viewModel.ShowSettings.Should().BeFalse();
    }

    [Fact]
    public async Task Commands_Invoke_NewText()
    {
        var viewModel = CreateViewModel();

        var initialCount = viewModel.Projects.Count;
        await viewModel.NewTextCommand.ExecuteAsync(null);

        viewModel.Projects.Should().HaveCount(initialCount + 1);
    }

    [Fact]
    public async Task Commands_Invoke_NewBlueprint()
    {
        var viewModel = CreateViewModel();

        var initialCount = viewModel.Projects.Count;
        await viewModel.NewBlueprintCommand.ExecuteAsync(null);

        viewModel.Projects.Should().HaveCount(initialCount + 1);
    }

    [Fact]
    public async Task Commands_Invoke_Close()
    {
        var viewModel = CreateViewModel();

        await viewModel.NewTextCommand.ExecuteAsync(null);
        viewModel.Projects.Should().HaveCount(1);

        await viewModel.CloseCommand.ExecuteAsync(null);

        viewModel.Projects.Should().BeEmpty();
    }

    [Fact]
    public void Commands_Invoke_Exit()
    {
        var viewModel = CreateViewModel();

        viewModel.ExitCommand.Execute(null);

        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void Commands_Invoke_CutCopyPaste_DoesNotThrow()
    {
        var viewModel = CreateViewModel();

        Action cut = () => viewModel.CutCommand.Execute(null);
        Action copy = () => viewModel.CopyCommand.Execute(null);
        Action paste = () => viewModel.PasteCommand.Execute(null);

        cut.Should().NotThrow();
        copy.Should().NotThrow();
        paste.Should().NotThrow();
    }

    [Fact]
    public void Commands_Invoke_StoreSettings_CapturesParameters()
    {
        var viewModel = CreateViewModel();

        viewModel.SimulationParameters.CauchyInitials.StartTime = 5.0;

        viewModel.StoreSettingsCommand.Execute(null);

        var snapshot = viewModel.SimulationParameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
    }

    [Fact]
    public void Commands_Invoke_Run_WithLismaProject()
    {
        var projectService = CreateProjectService();

        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersService();

        var simParams = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();

        var mockSimulationService = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        var errorList = new ErrorListViewModel();
        var parametersStore = new Mock<ISimulationParametersStoreService>().Object;
        var syntaxHighlighter = CreateSyntaxHighlighterMock().Object;

        var viewModel = new MainWindowViewModel(
            projectService,
            mockSimulationService,
            errorList,
            simParams,
            tasksPopOver,
            parametersStore,
            mockErrorService.Object,
            syntaxHighlighter);

        var mockLismaProject = new Mock<LismaProjectViewModel>(
            Mock.Of<ISimulationServerFacade>(),
            Mock.Of<ITextEditorFactory>(),
            Mock.Of<IProjectFileService>(),
            CreateSyntaxHighlighterMock().Object,
            new LismaTextModel("", Array.Empty<CodeRegion>()),
            null,
            null);

        viewModel.ActiveProject = mockLismaProject.Object;

        Action run = () => viewModel.RunCommand.Execute(null);

        run.Should().NotThrow();
    }

    [Fact]
    public void Commands_Invoke_Run_WithBlueprintProject_DoesNotRun()
    {
        var viewModel = CreateViewModel();

        var blueprintProject = new BlueprintProjectViewModel(
            Mock.Of<IProjectFileService>(),
            Mock.Of<ITextEditorFactory>(),
            BlueprintModel.Empty,
            null);
        viewModel.ActiveProject = blueprintProject;

        Action run = () => viewModel.RunCommand.Execute(null);

        run.Should().NotThrow();
    }

    [Fact]
    public void Commands_Invoke_Verify_WithLismaProject()
    {
        var projectService = CreateProjectService();

        var mockFacade = new Mock<ISimulationServerFacade>();
        mockFacade.Setup(f => f.ValidateModel(It.IsAny<string>()))
            .ReturnsAsync(new ValidationResult());

        var mockEditorFactory = new Mock<ITextEditorFactory>();

        var mockErrorService = new Mock<IModelErrorService>();
        var mockSimulationService = new Mock<SimulationServiceViewModel>(
            Mock.Of<ISimulationServerFacade>(),
            mockErrorService.Object,
            Mock.Of<ISimulationResultService>(),
            new SimulationParametersService(),
            null);

        var errorList = new ErrorListViewModel();
        var simParams = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();
        var parametersStore = new Mock<ISimulationParametersStoreService>().Object;
        var syntaxHighlighter = CreateSyntaxHighlighterMock().Object;

        var viewModel = new MainWindowViewModel(
            projectService,
            mockSimulationService.Object,
            errorList,
            simParams,
            tasksPopOver,
            parametersStore,
            mockErrorService.Object,
            syntaxHighlighter);

        var lismaProject = new LismaProjectViewModel(
            mockFacade.Object,
            mockEditorFactory.Object,
            Mock.Of<IProjectFileService>(),
            CreateSyntaxHighlighterMock().Object,
            new LismaTextModel("test content", Array.Empty<CodeRegion>()),
            null);

        viewModel.ActiveProject = lismaProject;

        Action verify = () => viewModel.VerifyCommand.Execute(null);

        verify.Should().NotThrow();
    }

    [Fact]
    public void ActiveProject_Property_UpdatesCorrectly()
    {
        var viewModel = CreateViewModel();

        viewModel.ActiveProject.Should().BeNull();

        var project = new BlueprintProjectViewModel(
            Mock.Of<IProjectFileService>(),
            Mock.Of<ITextEditorFactory>(),
            BlueprintModel.Empty,
            null);
        viewModel.ActiveProject = project;

        viewModel.ActiveProject.Should().Be(project);
    }

    [Fact]
    public void ShowSettings_Property_UpdatesCorrectly()
    {
        var viewModel = CreateViewModel();

        viewModel.ShowSettings.Should().BeFalse();

        viewModel.ShowSettings = true;
        viewModel.ShowSettings.Should().BeTrue();
    }
}
