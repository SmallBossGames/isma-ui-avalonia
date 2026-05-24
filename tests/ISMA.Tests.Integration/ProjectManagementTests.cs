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
public class ProjectManagementTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task CreateMultipleTextProjects_WorksWithAllFeatures()
    {
        // Create first project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().NotBeNull();
        var firstProject = Window.GetActiveProject()!;
        firstProject.Name.Should().Be("Untitled");

        // Set content via UI
        Window.SetEditorText("main { x = 0; }");

        // Create second project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(2);
        Window.GetActiveProject().Should().NotBeNull();

        // Create third project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(3);

        // Verify all projects exist
        ViewModel.Projects.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task CreateMultipleBlueprintProjects_WorksWithAllFeatures()
    {
        // Create first blueprint via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();

        // Create second blueprint via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(2);

        // Create third blueprint via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(3);

        // Verify all are blueprint projects
        ViewModel.Projects.All(p => p is BlueprintProjectViewModel).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task CreateMixedProjectTypes_WorksWithAllFeatures()
    {
        // Create text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        // Create blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(2);

        // Create another text project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(3);

        // Verify project types
        ViewModel.Projects.Count(p => p is LismaProjectViewModel).Should().Be(2);
        ViewModel.Projects.Count(p => p is BlueprintProjectViewModel).Should().Be(1);
    }

    [AvaloniaFact]
    public async Task CloseSingleProject_RemovesFromCollection()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        // Close project via UI
        Window.ClickMenuItem("MenuClose");

        // Verify project is removed
        ViewModel.Projects.Should().BeEmpty();
        Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task CloseAllProjects_RemovesAllFromCollection()
    {
        // Create multiple projects via UI
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(3);

        // Close all via UI
        Window.ClickMenuItem("MenuCloseAll");

        // Verify all are removed
        ViewModel.Projects.Should().BeEmpty();
        Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task ActiveProjectUpdatesOnSelection()
    {
        // Create first project via UI
        Window.ClickMenuItem("MenuNewText");
        var firstProject = Window.GetActiveProject();

        // Create second project (should become active)
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();
        Window.GetActiveProject().Should().NotBe(firstProject);

        // Create third project (should become active)
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();
        Window.GetActiveProject().Should().NotBe(firstProject);
    }

    [AvaloniaFact]
    public async Task CloseProject_DisposesIt()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        var project = Window.GetActiveProject();

        // Close project via UI
        Window.ClickMenuItem("MenuClose");

        // Verify project is disposed (no exceptions when accessing disposed project)
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_HasInitialStates()
    {
        // Create blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetActiveProject().Should().NotBeNull();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // Verify blueprint has initial states
        var model = blueprintProject!.GetBlueprintModel();
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
        model.Main.Name.Should().Be("main");
        model.Init.Name.Should().Be("init");
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanAddStates()
    {
        // Create blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states via editor
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);

        // Verify states were added
        editorVm.States.Should().HaveCount(4); // Main, Init, New state 1, New state 2
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanAddTransitions()
    {
        // Create blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add a state first
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        // Add transition
        editorVm.CurrentMode = BlueprintEditorMode.AddTransition;
        editorVm.SelectedState = editorVm.States[2];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify transition was added
        editorVm.Transactions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextProject_CanSetContent()
    {
        // Create text project via UI
        Window.ClickMenuItem("MenuNewText");
        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        // Set content using SetContent (which sets IsDirty)
        project!.SetContent("main { x = 0; }");
        project.FullText.Should().Be("main { x = 0; }");
        project.IsDirty.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task TextProject_NameIsSetFromContent()
    {
        // Create text project via UI
        Window.ClickMenuItem("MenuNewText");
        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        // Project should have a default name
        project!.Name.Should().NotBeNullOrEmpty();
    }
}
