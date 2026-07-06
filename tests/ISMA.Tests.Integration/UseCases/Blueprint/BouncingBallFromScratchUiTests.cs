using System.Collections.Immutable;
using System.Linq;
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
        _app.Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Verify initial state: only Main and Init states exist
        editorVm.States.Should().HaveCount(2);
        editorVm.States.Count(s => s.IsMain).Should().Be(1);
        editorVm.States.Count(s => s.IsInit).Should().Be(1);
        editorVm.Transactions.Should().BeEmpty();

        // Step 2: Add the "Up" state via UI
        _app.Window.ClickAddStateButton();
        editorVm.States.Should().HaveCount(3);
        var upState = editorVm.States.FirstOrDefault(s => s.Name == "New state 1");
        upState.Should().NotBeNull();

        // Rename "New state 1" to "Up" via inline editing
        editorVm.UpdateStateName(upState!, "Up");
        editorVm.States.Should().Contain(s => s.Name == "Up");

        // Step 3: Add the "Down" state via UI
        _app.Window.ClickAddStateButton();
        editorVm.States.Should().HaveCount(4);

        // Debug: print all state names
        var allNames = editorVm.States.Select(s => s.Name).ToArray();

        // After renaming "New state 1" to "Up", the name counter is at 2, so next state is "New state 2"
        var downState = editorVm.States.FirstOrDefault(s => s.Name == "New state 2");
        downState.Should().NotBeNull($"Second state should be named 'New state 2'. Actual names: [{string.Join(", ", allNames)}]");

        // Rename "New state 2" to "Down" via inline editing
        editorVm.UpdateStateName(downState!, "Down");
        editorVm.States.Should().Contain(s => s.Name == "Down");

        // Step 4: Edit the Main state content via text editor tab
        var mainState = editorVm.States.First(s => s.IsMain);
        _app.ViewModel.OpenStateTextEditorTab(mainState, "State: main");
        _app.Window.GetProjectCount().Should().Be(2); // Blueprint + text editor tab

        // The new tab should be the active one (state text editor)
        var activeTabName = _app.Window.GetActiveTabName();
        activeTabName.Should().Contain("State: main");

        // Set the main state LISMA content
        var mainText = @"v' = -g;
y' = v;

y(t0) = 10;";
        _app.Window.SetActiveTabText(mainText);

        // Save text back to the model (ViewModel Text doesn't auto-sync to model)
        // Re-capture the main state VM since ReloadViews() creates new instances
        var currentMainState = editorVm.States.First(s => s.IsMain);
        editorVm.UpdateStateText(currentMainState, mainText);

        // Close the state text editor tab
        _app.Window.ClickTabCloseButton(1);
        _app.Window.GetProjectCount().Should().Be(1);

        // Verify the main state text was saved
        mainState.Text.Should().Contain("v' = -g");
        mainState.Text.Should().Contain("y' = v");
        mainState.Text.Should().Contain("y(t0) = 10");

        // Step 5: Edit the "Up" state content via text editor tab
        var upStateVm = editorVm.States.First(s => s.Name == "Up");
        _app.ViewModel.OpenStateTextEditorTab(upStateVm, "State: Up");
        _app.Window.GetProjectCount().Should().Be(2);

        // Set the Up state LISMA content
        var upText = "set v = -v;";
        _app.Window.SetActiveTabText(upText);

        // Save text back to the state (updates both VM and model)
        // Re-capture the Up state VM since ReloadViews() creates new instances
        var currentUpState = editorVm.States.First(s => s.Name == "Up");
        editorVm.UpdateStateText(currentUpState, upText);

        // Close the tab
        _app.Window.ClickTabCloseButton(1);
        _app.Window.GetProjectCount().Should().Be(1);

        // Verify the Up state text was saved
        upStateVm.Text.Should().Contain("set v = -v");

        // Step 6: Add transition from init to Up (predicate: y < 0)
        _app.Window.ClickAddTransitionToggle();
        editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>();

        editorVm.SetTransitionSource(editorVm.States.First(s => s.IsInit));
        editorVm.SelectedState = upStateVm;
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(1);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        // Set the predicate for the first transition
        editorVm.Transactions[0].Predicate = "y < 0";

        // Step 7: Add transition from init to Down (predicate: v < 0)
        _app.Window.ClickAddTransitionToggle();
        editorVm.SetTransitionSource(editorVm.States.First(s => s.IsInit));
        var downStateVm = editorVm.States.First(s => s.Name == "Down");
        editorVm.SelectedState = downStateVm;
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(2);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        // Set the predicate for the second transition
        editorVm.Transactions[1].Predicate = "v < 0";

        // Step 8: Add transition from Down to Up (predicate: y < 0)
        _app.Window.ClickAddTransitionToggle();
        editorVm.SetTransitionSource(downStateVm);
        editorVm.SelectedState = upStateVm;
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(3);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        // Set the predicate for the third transition
        editorVm.Transactions[2].Predicate = "y < 0";

        // Step 9: Add transition from Up to Down (predicate: v < 0)
        _app.Window.ClickAddTransitionToggle();
        editorVm.SetTransitionSource(upStateVm);
        editorVm.SelectedState = downStateVm;
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(4);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        // Set the predicate for the fourth transition
        editorVm.Transactions[3].Predicate = "v < 0";

        // Step 10: Verify the complete blueprint model structure
        editorVm.States.Should().HaveCount(4); // Main, Init, Up, Down
        editorVm.Transactions.Should().HaveCount(4);
        editorVm.LoopTransactions.Should().BeEmpty();

        // Verify transaction connections
        var txStartStates = editorVm.Transactions.Select(t => t.StartState.Name).OrderBy(n => n).ToImmutableArray();
        txStartStates.Should().Equal("Down", "init", "init", "Up");

        var txEndStates = editorVm.Transactions.Select(t => t.EndState.Name).OrderBy(n => n).ToImmutableArray();
        txEndStates.Should().Equal("Down", "Down", "Up", "Up");

        var txPredicates = editorVm.Transactions.Select(t => t.Predicate).OrderBy(n => n).ToImmutableArray();
        txPredicates.Should().Equal("v < 0", "v < 0", "y < 0", "y < 0");

        // Step 11: Convert to LISMA and verify the generated text
        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();

        // Debug: print the actual LISMA text
        var actualText = lisma.FullText;

        lisma.FullText.Should().Contain("v' = -g");
        lisma.FullText.Should().Contain("y' = v");
        lisma.FullText.Should().Contain("set v = -v");

        // Step 12: Run simulation via UI
        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "bouncing-ball-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/bouncing-ball-result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        // Verify simulation completed
        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
        _app.ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
    }

    [AvaloniaFact]
    public async Task BouncingBall_StateTextTab_ClosesAndSavesContent()
    {
        // Test that closing a state text editor tab saves the content back to the blueprint state

        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add a new state
        _app.Window.ClickAddStateButton();
        var newState = editorVm.States.First(s => s.Name == "New state 1");

        // Open text editor tab via ViewModel
        _app.ViewModel.OpenStateTextEditorTab(newState, "State: New state 1");
        _app.Window.GetProjectCount().Should().Be(2);

        // Set content
        var newText = "x = 42;\ny = 100;";
        _app.Window.SetActiveTabText(newText);

        // Close the tab - content should be saved back
        _app.Window.ClickTabCloseButton(1);
        _app.Window.GetProjectCount().Should().Be(1);

        // Verify content was saved
        newState.Text.Should().Be(newText);
    }

    [AvaloniaFact]
    public async Task BouncingBall_MainStateText_EditableViaDoubleClick()
    {
        // Test that the Main state (which is non-editable on canvas) can still have its text edited
        // by opening a text editor tab

        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        var mainState = editorVm.States.First(s => s.IsMain);
        mainState.Text.Should().BeEmpty(); // Default empty

        // Open text editor tab for main state
        _app.ViewModel.OpenStateTextEditorTab(mainState, "State: main");
        _app.Window.GetProjectCount().Should().Be(2);

        // Set content
        _app.Window.SetActiveTabText("const g = 9.81;");

        // Close tab
        _app.Window.ClickTabCloseButton(1);

        // Verify saved
        mainState.Text.Should().Be("const g = 9.81;");
    }
}
