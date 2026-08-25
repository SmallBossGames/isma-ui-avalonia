using Avalonia;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// End-to-end tests for the Blueprint editor scenarios.
/// Tests state creation, transitions, and blueprint-to-LISMA conversion via UI.
/// </summary>
public class BlueprintEditorTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;

    [AvaloniaFact]
    public async Task BlueprintProject_CanCreateStates()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add states via UI toolbar button
        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();

        // Verify states were added (2 user states, Main and Init are separate properties)
        editorVm.States.Should().HaveCount(2);

        // Verify state names
        var mainState = editorVm.MainState;
        var initState = editorVm.InitState;
        var userState1 = editorVm.States.First(s => s.Name == "State 1");
        var userState2 = editorVm.States.First(s => s.Name == "State 2");

        mainState.Name.Should().Be("Main");
        initState.Name.Should().Be("init");
        userState1.Name.Should().Be("State 1");
        userState2.Name.Should().Be("State 2");
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanAddTransitions()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add states first via UI
        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();
        editorVm.States.Should().HaveCount(2);

        // Add transition between two user states via UI toggles
        _app.Window.ClickAddTransitionToggle();
        editorVm.Mode = new EditorMode.AddTransition();
        editorVm.SetTransitionSource(editorVm.States[0]);
        editorVm.SelectedState = editorVm.States[1];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify transition was added
        editorVm.Transitions.Should().HaveCount(1);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanRemoveStates()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add a state via UI
        _app.Window.ClickAddStateButton();
        editorVm.States.Should().HaveCount(1);

        // Remove the user state via UI toggle
        _app.Window.ClickRemoveStateToggle();
        editorVm.Mode = new EditorMode.RemoveState();
        editorVm.SelectedState = editorVm.States[0];
        editorVm.RemoveStateCommand.Execute(null);

        // Verify state was removed (no user states)
        editorVm.States.Should().HaveCount(0);
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanRemoveTransitions()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add states and transition via UI
        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddTransitionToggle();
        editorVm.Mode = new EditorMode.AddTransition();
        editorVm.SetTransitionSource(editorVm.States[0]);
        editorVm.SelectedState = editorVm.States[1];
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transitions.Should().HaveCount(1);

        // Remove the transition via UI toggle
        _app.Window.ClickRemoveTransitionToggle();
        editorVm.Mode = new EditorMode.RemoveTransition();
        editorVm.SelectedTransition = editorVm.Transitions[0];
        editorVm.RemoveTransitionCommand.Execute(null);

        // Verify transition was removed
        editorVm.Transitions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanConvertToLisma()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // Convert to LISMA
        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_WithStates_ConvertsToLisma()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add states via UI
        _app.Window.ClickAddStateButton();
        editorVm.States.Should().HaveCount(1);

        // Convert to LISMA
        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_StateNamesMustBeUnique()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add first state via UI
        _app.Window.ClickAddStateButton();
        var firstStateName = editorVm.States[0].Name;

        // Try to add another state with the same name (should fail)
        editorVm.AddStateWithName(firstStateName, 200, 200);
        editorVm.States.Should().HaveCount(1); // Still 1 (duplicate name rejected)
    }

    [AvaloniaFact]
    public async Task BlueprintProject_EditorModes_WorkCorrectly()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Verify initial mode
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        // Set AddTransition mode
        editorVm.Mode = new EditorMode.AddTransition();
        editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>();

        // Reset mode via UI
        editorVm.ResetEditorModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_ModelPersistsStateChanges()
    {
        // Create blueprint project via UI
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        // Add states via UI
        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();

        // Get model from editor (not from project, since project model is separate)
        var model = editorVm.GetBlueprintModel();
        model.States.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task BlueprintProject_EmptyModel_ConvertsToMinimalLisma()
    {
        var model = BlueprintModel.Empty;
        var lisma = BlueprintToLismaConverter.ConvertToLisma(model);
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }
}
