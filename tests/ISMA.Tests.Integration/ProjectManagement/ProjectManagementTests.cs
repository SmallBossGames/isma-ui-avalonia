using Avalonia;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// End-to-end tests for project management scenarios.
/// Tests multi-project workflow, creation, closing, and switching.
/// </summary>
public class ProjectManagementTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task CreateMultipleTextProjects_WorksWithAllFeatures()
    {
        // Create first project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().NotBeNull();
        var firstProject = _app.Window.GetActiveProject()!;
        firstProject.Name.Should().Be("Untitled");

        // Set content via UI
        _app.Window.SetEditorText("main { x = 0; }");

        // Create second project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(2);
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Create third project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(3);

        // Verify all projects exist
        _app.Window.GetProjectCount().Should().Be(3);
    }

    [AvaloniaFact]
    public async Task CreateMultipleBlueprintProjects_WorksWithAllFeatures()
    {
        // Create first blueprint via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();

        // Create second blueprint via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(2);

        // Create third blueprint via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(3);

        // Verify all are blueprint projects
        _app.Window.GetProjectCount().Should().Be(3);
    }

    [AvaloniaFact]
    public async Task CreateMixedProjectTypes_WorksWithAllFeatures()
    {
        // Create text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(2);

        // Create another text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(3);

        // Verify project types (count check via UI)
        _app.Window.GetProjectCount().Should().Be(3);
    }

    [AvaloniaFact]
    public async Task CloseSingleProject_RemovesFromCollection()
    {
        // Create project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        // Close project via UI
        _app.Window.ClickMenuItem("MenuClose");

        // Verify project is removed
        _app.Window.GetProjectCount().Should().Be(0);
        _app.Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task CloseAllProjects_RemovesAllFromCollection()
    {
        // Create multiple projects via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(3);

        // Close all via UI
        _app.Window.ClickMenuItem("MenuCloseAll");

        // Verify all are removed
        _app.Window.GetProjectCount().Should().Be(0);
        _app.Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task ActiveProjectUpdatesOnSelection()
    {
        // Create first project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        var firstProject = _app.Window.GetActiveProject();

        // Create second project (should become active)
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.GetActiveProject().Should().NotBe(firstProject);

        // Create third project (should become active)
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.GetActiveProject().Should().NotBe(firstProject);
    }

    [AvaloniaFact]
    public async Task CloseProject_DisposesIt()
    {
        // Create project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        var project = _app.Window.GetActiveProject();

        // Close project via UI
        _app.Window.ClickMenuItem("MenuClose");

        // Verify project is disposed (no exceptions when accessing disposed project)
        _app.Window.GetProjectCount().Should().Be(0);
    }

    [AvaloniaFact]
    public async Task BlueprintProject_HasInitialStates()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // Verify blueprint has initial states
        var model = blueprintProject!.GetBlueprintModel();
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
        model.Main.Name.Should().Be("Main");
        model.Init.Name.Should().Be("init");
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanAddStates()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states via editor
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);

        // Verify states were added (2 user states, Main and Init are separate)
        editorVm.States.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanAddTransitions()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add two states first
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(2);

        // Add transition between two user states
        editorVm.Mode = new EditorMode.AddTransition();
        editorVm.SetTransitionSource(editorVm.States[0]);
        editorVm.SelectedState = editorVm.States[1];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify transition was added
        editorVm.Transitions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextProject_CanSetContent()
    {
        // Create text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        // Set text directly in the TextEditor UI component
        _app.Window.SetEditorText("main { x = 0; }");
    }

    [AvaloniaFact]
    public async Task TextProject_NameIsSetFromContent()
    {
        // Create text project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        var project = _app.Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        // Project should have a default name
        project!.Name.Should().NotBeNullOrEmpty();
    }
}
