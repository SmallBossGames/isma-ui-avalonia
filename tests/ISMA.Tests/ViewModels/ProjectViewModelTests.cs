global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class ProjectViewModelTests
{
    private static Mock<ISyntaxHighlighter> CreateSyntaxHighlighterMock()
    {
        var mock = new Mock<ISyntaxHighlighter>();
        mock.Setup(m => m.Highlight(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<SyntaxTokenDto>());
        return mock;
    }

    private static ProjectService CreateProjectService()
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockParamsService = new Mock<SimulationParametersService>();
        var mockSyntax = CreateSyntaxHighlighterMock();

        return new ProjectService(
            mockFileService.Object,
            mockFacade.Object,
            mockEditorFactory.Object,
            mockParamsService.Object,
            mockSyntax.Object);
    }

    private static LismaProjectViewModel CreateLismaProject(
        ISimulationServerFacade? facade = null,
        string? filePath = null,
        LismaTextModel? model = null)
    {
        var mockFacade = facade ?? new Mock<ISimulationServerFacade>().Object;
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockFileService = new Mock<IProjectFileService>();
        var mockSyntax = CreateSyntaxHighlighterMock();
        var lismaModel = model ?? new LismaTextModel("", Array.Empty<CodeRegion>());

        return new LismaProjectViewModel(
            mockFacade,
            mockEditorFactory.Object,
            mockFileService.Object,
            mockSyntax.Object,
            lismaModel,
            filePath);
    }

    private static BlueprintProjectViewModel CreateBlueprintProject(
        BlueprintModel? blueprintModel = null)
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var model = blueprintModel ?? BlueprintModel.Empty;

        return new BlueprintProjectViewModel(
            mockFileService.Object,
            mockEditorFactory.Object,
            model,
            null);
    }

    [Fact]
    public async Task CreateProject_CreatesLismaProjectViewModel()
    {
        var service = CreateProjectService();

        var project = await service.CreateNewAsync();

        project.Should().NotBeNull();
        project.Should().BeOfType<LismaProjectViewModel>();
        service.Projects.Should().HaveCount(1);
        service.ActiveProject.Should().Be(project);
    }

    [Fact]
    public async Task CreateBlueprintProject_CreatesBlueprintProjectViewModel()
    {
        var service = CreateProjectService();

        var project = await service.CreateNewBlueprintAsync();

        project.Should().NotBeNull();
        project.Should().BeOfType<BlueprintProjectViewModel>();
        service.Projects.Should().HaveCount(1);
        service.ActiveProject.Should().Be(project);
    }

    [Fact]
    public async Task CloseProject_RemovesFromList()
    {
        var service = CreateProjectService();

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
        var service = CreateProjectService();

        var result = await service.CloseAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CloseAllProjects_ClosesAll()
    {
        var service = CreateProjectService();

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
        var project = CreateLismaProject(filePath: "/tmp/test.isma");

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
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "initial content");

        var project = CreateLismaProject(filePath: tempFile, model: new LismaTextModel("initial content", Array.Empty<CodeRegion>()));

        project.SetContent("new content");

        var result = await project.SaveAsync();

        result.Should().BeTrue();
        File.ReadAllText(tempFile).Should().Be("new content");

        File.Delete(tempFile);
    }

    [Fact]
    public async Task BlueprintProject_GetBlueprintModel_ReturnsModel()
    {
        var project = CreateBlueprintProject();

        var model = project.GetBlueprintModel();

        model.Should().NotBeNull();
        model.Main.Name.Should().Be("Main");
        model.Init.Name.Should().Be("init");
    }

    [Fact]
    public async Task BlueprintProject_ConvertToLisma_ReturnsResult()
    {
        var project = CreateBlueprintProject();

        var result = project.ConvertToLisma();

        result.Should().NotBeNull();
        result.FullText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BlueprintProject_LoadFromFile_SetsNameAndPath()
    {
        var project = CreateBlueprintProject();

        project.LoadFromFile("/home/user/project.isma");

        project.Name.Should().Be("project");
        project.FilePath.Should().Be("/home/user/project.isma");
    }

    [Fact]
    public void ProjectViewModel_NameChanged_EventFiresOnLoadFromFile()
    {
        var eventFired = false;
        var project = CreateBlueprintProject();
        project.NameChanged += () => eventFired = true;

        project.LoadFromFile("/tmp/myfile.isma");

        eventFired.Should().BeTrue();
        project.Name.Should().Be("myfile");
    }

    [Fact]
    public void ProjectViewModel_Dispose_CleansUpEditor()
    {
        var mockEditorFactory = new Mock<ITextEditorFactory>();
        var mockEditor = new Mock<object>();
        var mockFileService = new Mock<IProjectFileService>();
        var mockSyntax = CreateSyntaxHighlighterMock();

        var project = new LismaProjectViewModel(
            new Mock<ISimulationServerFacade>().Object,
            mockEditorFactory.Object,
            mockFileService.Object,
            mockSyntax.Object,
            new LismaTextModel("", Array.Empty<CodeRegion>()),
            null);

        project.SetEditorInstance(mockEditor.Object);
        project.Dispose();

        mockEditorFactory.Verify(f => f.DisposeInstance(mockEditor.Object), Times.Once);
    }
}
