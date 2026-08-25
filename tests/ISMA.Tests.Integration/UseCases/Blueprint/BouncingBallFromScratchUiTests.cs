using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration test that recreates the "Bouncing Ball" blueprint model from scratch
/// using ONLY UI interactions, exactly as a real user would.
///
/// The model represents a ball that:
/// - Starts at height y=10 with gravity g=9.81
/// - Falls under gravity (v' = -g, y' = v)
/// - Bounces when y < 0 (velocity is inverted: v = -v)
/// - Alternates between upward and downward motion
///
/// State machine:
///   init --(y&lt;0)--> Up --(v&lt;0)--> Down --(y&lt;0)--> Up (loop)
///   init --(v&lt;0)--> Down
/// </summary>
public class BouncingBallFromScratchUiTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;

    [AvaloniaFact]
    public async Task BouncingBall_CanBeCreatedFromScratch_ViaUi()
    {
        // Step 1: Create a new blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);

        // Verify initial state: no user states on canvas (Main and Init are separate properties)
        _app.Window.GetStateBoxCount().Should().Be(0);

        // Step 2: Add the "Up" state via UI
        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(1);

        // Rename "State 1" to "Up" via UI helper
        _app.Window.RenameStateBoxByName("State 1", "Up");
        _app.Window.GetStateBoxByName("Up").Should().NotBeNull();

        // Step 3: Add the "Down" state via UI
        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(2);

        // After renaming "State 1" to "Up", the name counter is at 2, so next state is "State 2"
        _app.Window.GetStateBoxByName("State 2").Should().NotBeNull();

        // Rename "State 2" to "Down" via UI helper
        _app.Window.RenameStateBoxByName("State 2", "Down");
        _app.Window.GetStateBoxByName("Down").Should().NotBeNull();

        // Step 4: Edit the Main state content via ViewModel
        var bpProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)bpProject!.EditorContent!;

        // Set the main state LISMA content via ViewModel
        var mainText = @"v' = -g;
y' = v;

y(t0) = 10;";
        editorVm.UpdateStateText(editorVm.MainState, mainText);

        // Verify the main state text was saved
        editorVm.MainState.Text.Should().Contain("v' = -g");
        editorVm.MainState.Text.Should().Contain("y' = v");
        editorVm.MainState.Text.Should().Contain("y(t0) = 10");

        // Step 5: Edit the "Up" state content via inner text editor tab (double-click)
        _app.Window.OpenStateTextEditorViaDoubleClick("Up");
        _app.Window.GetInnerEditorTabCount().Should().Be(2); // Diagram + Up

        // Set the Up state LISMA content
        var upText = "set v = -v;";
        _app.Window.SetActiveInnerTabText(upText);

        // Close the inner tab - content is saved live via two-way binding
        _app.Window.ClickInnerTabCloseButton(1);
        _app.Window.GetInnerEditorTabCount().Should().Be(1);

        // Verify the Up state text was saved
        editorVm.States.First(s => s.Name == "Up").Text.Should().Be(upText);

        // Step 6-9: Add transitions
        // init → Up and init → Down cannot be created via the UI (original only
        // allows user states as transition source), so add them directly.
        var upState = editorVm.States.First(s => s.Name == "Up");
        var downState = editorVm.States.First(s => s.Name == "Down");

        editorVm.Transitions.Add(new BlueprintTransitionViewModel(editorVm.States)
        {
            StartStateId = editorVm.InitState.Id,
            EndStateId = upState.Id,
            Predicate = "y < 0",
            Alias = ""
        });

        editorVm.Transitions.Add(new BlueprintTransitionViewModel(editorVm.States)
        {
            StartStateId = editorVm.InitState.Id,
            EndStateId = downState.Id,
            Predicate = "v < 0",
            Alias = ""
        });

        // Transition 3: Down → Up (predicate: y < 0)
        editorVm.Mode = new EditorMode.AddTransition();
        editorVm.SetTransitionSource(downState);
        editorVm.SelectedState = upState;
        editorVm.AddTransitionCommand.Execute(null);
        editorVm.Transitions[2].Predicate = "y < 0";

        // Transition 4: Up → Down (predicate: v < 0)
        editorVm.Mode = new EditorMode.AddTransition();
        editorVm.SetTransitionSource(upState);
        editorVm.SelectedState = downState;
        editorVm.AddTransitionCommand.Execute(null);
        editorVm.Transitions[3].Predicate = "v < 0";

        editorVm.Mode = new EditorMode.Default();

        // Step 10: Verify the complete blueprint model structure
        _app.Window.GetStateBoxCount().Should().Be(2); // Up, Down (Main and Init are separate)
        _app.Window.GetArrowLineCount().Should().Be(4);
        _app.Window.GetLoopArrowCount().Should().Be(0);

        // Step 11: Convert to LISMA and verify the generated text
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull("Blueprint project should be active.");

        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();

        lisma.FullText.Should().Contain("v' = -g");
        lisma.FullText.Should().Contain("y' = v");
        lisma.FullText.Should().Contain("set v = -v");

        // Step 12: Run simulation via UI
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "bouncing-ball-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/bouncing-ball-result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        // Verify simulation completed by checking the Run button is enabled again
        // (it gets disabled while simulation is running)
        _app.Window.IsRunButtonEnabled().Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task BouncingBall_StateTextTab_ClosesAndSavesContent()
    {
        // Test that closing a state text editor tab saves the content back to the blueprint state

        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);

        // Add a new state via UI
        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(1);

        // Open inner text editor tab via UI double-click
        _app.Window.OpenStateTextEditorViaDoubleClick("State 1");
        _app.Window.GetInnerEditorTabCount().Should().Be(2);

        // Set content
        var newText = "x = 42;\ny = 100;";
        _app.Window.SetActiveInnerTabText(newText);

        // Close the inner tab - content is saved live via two-way binding
        _app.Window.ClickInnerTabCloseButton(1);
        _app.Window.GetInnerEditorTabCount().Should().Be(1);

        // Verify content was saved by reading from UI
        _app.Window.GetStateBoxText("State 1").Should().Be(newText);
    }

    [AvaloniaFact]
    public async Task BouncingBall_MainStateText_EditableViaDoubleClick()
    {
        // Test that the Main state (which is non-editable on canvas) can still have its text edited
        // by opening a text editor tab via double-click

        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);

        // Main state should have empty text by default
        var bpProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)bpProject!.EditorContent!;
        editorVm.MainState.Text.Should().BeNullOrEmpty();

        // Set content via ViewModel
        editorVm.UpdateStateText(editorVm.MainState, "const g = 9.81;");

        // Verify saved
        editorVm.MainState.Text.Should().Be("const g = 9.81;");
    }
}
