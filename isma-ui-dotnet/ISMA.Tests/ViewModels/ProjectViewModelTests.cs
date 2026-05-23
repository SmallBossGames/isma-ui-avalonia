using Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class ProjectViewModelTests
{
    [Fact]
    public async Task CreateProject_CreatesLismaProjectViewModel()
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockParamsService = new Mock<SimulationParametersService>();

        var service = new ProjectService(
            mockFileService.Object,
            mockFacade.Object,
            mockEditorFactory.Object,
            mockParamsService.Object);

        var project = await service.CreateNewAsync();

        project.Should().NotBeNull();
        project.Should().BeOfType<LismaProjectViewModel>();
        service.Projects.Should().HaveCount(1);
        service.ActiveProject.Should().Be(project);
    }

    [Fact]
    public async Task CreateBlueprintProject_CreatesBlueprintProjectViewModel()
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockParamsService = new Mock<SimulationParametersService>();

        var service = new ProjectService(
            mockFileService.Object,
            mockFacade.Object,
            mockEditorFactory.Object,
            mockParamsService.Object);

        var project = await service.CreateNewBlueprintAsync();

        project.Should().NotBeNull();
        project.Should().BeOfType<BlueprintProjectViewModel>();
        service.Projects.Should().HaveCount(1);
        service.ActiveProject.Should().Be(project);
    }

    [Fact]
    public async Task CloseProject_RemovesFromList()
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockParamsService = new Mock<SimulationParametersService>();

        var service = new ProjectService(
            mockFileService.Object,
            mockFacade.Object,
            mockEditorFactory.Object,
            mockParamsService.Object);

        var project = await service.CreateNewAsync();
        service.Projects.Should().HaveCount(1);

        var result = await service.CloseAsync();

        result.Should().BeTrue();
        service.Projects.Should().BeEmpty();
        service.ActiveProject.Should().BeNull();
    }

    [Fact]
    public async Task CloseProject_WhenNoActive_ReturnsFalse()
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockParamsService = new Mock<SimulationParametersService>();

        var service = new ProjectService(
            mockFileService.Object,
            mockFacade.Object,
            mockEditorFactory.Object,
            mockParamsService.Object);

        var result = await service.CloseAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CloseAllProjects_ClosesAll()
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockParamsService = new Mock<SimulationParametersService>();

        var service = new ProjectService(
            mockFileService.Object,
            mockFacade.Object,
            mockEditorFactory.Object,
            mockParamsService.Object);

        await service.CreateNewAsync();
        await service.CreateNewAsync();
        service.Projects.Should().HaveCount(2);

        await service.CloseAllAsync();

        service.Projects.Should().BeEmpty();
        service.ActiveProject.Should().BeNull();
    }

    [Fact]
    public async Task ChangeName_FiresNameChangedEvent()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();

        var project = new LismaProjectViewModel(
            mockFacade.Object,
            mockEditorFactory.Object,
            new LismaTextModel("", Array.Empty<CodeRegion>()),
            "/tmp/test.isma");

        bool nameChangedFired = false;
        project.NameChanged += () => nameChangedFired = true;

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "content");
        project.LoadFromFile(tempFile);
        File.Delete(tempFile);

        nameChangedFired.Should().BeTrue();
        var expectedName = Path.GetFileNameWithoutExtension(tempFile);
        project.Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task SaveLismaProject_SavesToFile()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "initial content");

        var project = new LismaProjectViewModel(
            mockFacade.Object,
            mockEditorFactory.Object,
            new LismaTextModel("initial content", Array.Empty<CodeRegion>()),
            tempFile);

        project.SetContent("new content");

        var result = await project.SaveAsync();

        result.Should().BeTrue();
        File.ReadAllText(tempFile).Should().Be("new content");

        File.Delete(tempFile);
    }

    [Fact]
    public async Task BlueprintProject_GetBlueprintModel_ReturnsModel()
    {
        var project = new BlueprintProjectViewModel();

        var model = project.GetBlueprintModel();

        model.Should().NotBeNull();
        model.Main.Name.Should().Be("main");
        model.Init.Name.Should().Be("init");
    }

    [Fact]
    public async Task BlueprintProject_ConvertToLisma_ReturnsResult()
    {
        var project = new BlueprintProjectViewModel();

        var result = project.ConvertToLisma();

        result.Should().NotBeNull();
        result.FullText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BlueprintProject_LoadFromFile_SetsNameAndPath()
    {
        var project = new BlueprintProjectViewModel();

        project.LoadFromFile("/home/user/project.isma");

        project.Name.Should().Be("project");
        project.FilePath.Should().Be("/home/user/project.isma");
    }

    [Fact]
    public void ProjectViewModel_NameChanged_EventFiresOnLoadFromFile()
    {
        var eventFired = false;
        var project = new BlueprintProjectViewModel();
        project.NameChanged += () => eventFired = true;

        project.LoadFromFile("/tmp/myfile.isma");

        eventFired.Should().BeTrue();
        project.Name.Should().Be("myfile");
    }

    [Fact]
    public void ProjectViewModel_Dispose_CleansUpEditor()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockEditor = new Mock<object>();

        var project = new LismaProjectViewModel(
            mockFacade.Object,
            mockEditorFactory.Object,
            new LismaTextModel("", Array.Empty<CodeRegion>()),
            null);

        project.SetEditorInstance(mockEditor.Object);
        project.Dispose();

        mockEditorFactory.Verify(f => f.DisposeInstance(mockEditor.Object), Times.Once);
    }
}
