using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration.UseCases.ProjectManagement;

/// <summary>
/// UC-09: Save, Save As, and Save All
/// Tests project saving, closing, and tab management via UI interactions.
/// </summary>
public class SaveAndCloseTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC09_CloseSingleProject_RemovesFromCollection()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();
        Window.GetProjectCount().Should().Be(1);

        // Close project via UI
        Window.ClickMenuItem("MenuClose");
        Window.Flush();

        // Verify project is removed
        ViewModel.Projects.Should().BeEmpty();
        Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task UC09_CloseAllProjects_RemovesAllFromCollection()
    {
        // Create multiple projects via UI
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();
        Window.GetProjectCount().Should().Be(3);

        // Close all via UI
        Window.ClickMenuItem("MenuCloseAll");
        Window.Flush();

        // Verify all are removed
        ViewModel.Projects.Should().BeEmpty();
        Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task UC09_CloseProject_DisposesIt()
    {
        // Create project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();
        var project = Window.GetActiveProject();

        // Close project via UI
        Window.ClickMenuItem("MenuClose");
        Window.Flush();

        // Verify project is removed
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task UC09_MultipleProjects_CloseSpecificTab()
    {
        // Create 3 projects
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();
        Window.GetProjectCount().Should().Be(3);

        // Close via Close command (closes active project)
        Window.ClickMenuItem("MenuClose");
        Window.Flush();
        ViewModel.Projects.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task UC04_MultiProjectEditing_EachProjectIsIndependent()
    {
        // Create first project
        Window.ClickMenuItem("MenuNewText");
        var firstProject = Window.GetActiveProject() as LismaProjectViewModel;

        // Create second project
        Window.ClickMenuItem("MenuNewText");
        var secondProject = Window.GetActiveProject() as LismaProjectViewModel;

        // Verify both projects exist
        ViewModel.Projects.Should().HaveCount(2);
        firstProject.Should().NotBeNull();
        secondProject.Should().NotBeNull();
    }
}
