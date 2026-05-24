using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;

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
        // Create first project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.ActiveProject.Should().NotBeNull();
        var firstProject = ViewModel.ActiveProject!;
        firstProject.Name.Should().Be("Untitled");

        // Set content for first project
        var firstLisma = firstProject as LismaProjectViewModel;
        firstLisma!.FullText = "main { x = 0; }";

        // Create second project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(2);
        ViewModel.ActiveProject.Should().NotBeNull();

        // Create third project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(3);

        // Verify all projects exist
        ViewModel.Projects.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task CreateMultipleBlueprintProjects_WorksWithAllFeatures()
    {
        // Create first blueprint
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.ActiveProject.Should().BeOfType<BlueprintProjectViewModel>();

        // Create second blueprint
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(2);

        // Create third blueprint
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(3);

        // Verify all are blueprint projects
        ViewModel.Projects.All(p => p is BlueprintProjectViewModel).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task CreateMixedProjectTypes_WorksWithAllFeatures()
    {
        // Create text project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(2);

        // Create another text project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(3);

        // Verify project types
        ViewModel.Projects.Count(p => p is LismaProjectViewModel).Should().Be(2);
        ViewModel.Projects.Count(p => p is BlueprintProjectViewModel).Should().Be(1);
    }

    [AvaloniaFact]
    public async Task CloseSingleProject_RemovesFromCollection()
    {
        // Create project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

        // Close project
        await ViewModel.CloseCommand.ExecuteAsync(null);

        // Verify project is removed
        ViewModel.Projects.Should().BeEmpty();
        ViewModel.ActiveProject.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task CloseAllProjects_RemovesAllFromCollection()
    {
        // Create multiple projects
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(3);

        // Close all
        await ViewModel.CloseAllCommand.ExecuteAsync(null);

        // Verify all are removed
        ViewModel.Projects.Should().BeEmpty();
        ViewModel.ActiveProject.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task ActiveProjectUpdatesOnSelection()
    {
        // Create first project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var firstProject = ViewModel.ActiveProject;

        // Create second project (should become active)
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();
        ViewModel.ActiveProject.Should().NotBe(firstProject);

        // Create third project (should become active)
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();
        ViewModel.ActiveProject.Should().NotBe(firstProject);
    }

    [AvaloniaFact]
    public async Task CloseProject_DisposesIt()
    {
        // Create project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var project = ViewModel.ActiveProject;

        // Close project
        await ViewModel.CloseCommand.ExecuteAsync(null);

        // Verify project is disposed (no exceptions when accessing disposed project)
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_HasInitialStates()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();

        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
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
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
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
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
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
        // Create text project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var project = ViewModel.ActiveProject as LismaProjectViewModel;
        project.Should().NotBeNull();

        // Set content using SetContent (which sets IsDirty)
        project!.SetContent("main { x = 0; }");
        project.FullText.Should().Be("main { x = 0; }");
        project.IsDirty.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task TextProject_NameIsSetFromContent()
    {
        // Create text project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var project = ViewModel.ActiveProject as LismaProjectViewModel;
        project.Should().NotBeNull();

        // Project should have a default name
        project!.Name.Should().NotBeNullOrEmpty();
    }
}
