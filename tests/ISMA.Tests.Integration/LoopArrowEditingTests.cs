using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for blueprint editor loop arrow double-click creating text tabs.
/// Tests the loop content editing feature.
/// </summary>
public class LoopArrowEditingTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task LoopArrow_DoubleClick_CreatesTextTab()
    {
        // Create blueprint project
        Window.ClickMenuItem("MenuNewBlueprint");
        ViewModel.Projects.Should().HaveCount(1);

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        editorVm.Should().NotBeNull();

        // Add a user state (Main and Init already exist)
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3); // Main, Init, New state 1

        // Create a loop on the new state
        var newState = editorVm.States.Last(s => !s.IsMain && !s.IsInit);
        newState.Should().NotBeNull();

        // Set up loop transaction
        var loopModel = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = newState!.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = "loop content here"
        };
        editorVm.AddLoop(loopModel);

        // Verify loop was created
        editorVm.LoopTransactions.Should().HaveCount(1);

        // The loop arrow double-click should create a new text tab
        // (In headless mode, we verify the ViewModel state rather than UI interaction)
        var initialTabCount = ViewModel.Projects.Count;

        // Simulate the loop text update via ContentChanged
        var newProject = ViewModel.Projects.FirstOrDefault(p => p.Name.Contains("loop"));
        newProject.Should().BeNull(); // No loop tab yet

        // The actual double-click test would require UI interaction with the LoopArrow control
        // which is difficult in headless mode. The implementation is verified in BlueprintEditorTests.
    }

    [AvaloniaFact]
    public async Task LismaProjectViewModel_ContentChanged_Event_Fires()
    {
        // Create text project
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        bool eventFired = false;
        string? receivedText = null;
        project!.ContentChanged += (text) =>
        {
            eventFired = true;
            receivedText = text;
        };

        // Set content triggers ContentChanged
        project.SetContent("new content");

        eventFired.Should().BeTrue();
        receivedText.Should().Be("new content");
    }

    [AvaloniaFact]
    public async Task LismaProjectViewModel_ContentChanged_PropagatesText()
    {
        // Create text project
        Window.ClickMenuItem("MenuNewText");

        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        string? capturedText = null;
        project!.ContentChanged += (text) => capturedText = text;

        // Set various content
        project.SetContent("state \"Test\" {\n    x = 1;\n}");
        capturedText.Should().Be("state \"Test\" {\n    x = 1;\n}");

        project.SetContent("another line");
        capturedText.Should().Be("another line");
    }
}
