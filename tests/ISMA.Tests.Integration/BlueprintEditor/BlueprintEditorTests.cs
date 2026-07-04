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
/// Tests state creation, transitions, and blueprint-to-LISMA conversion.
/// </summary>
public class BlueprintEditorTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task BlueprintProject_CanCreateStates()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();

        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);

        // Verify states were added (Main, Init, New state 1, New state 2)
        editorVm.States.Should().HaveCount(4);

        // Verify state names
        var mainState = editorVm.States.First(s => s.IsMain);
        var initState = editorVm.States.First(s => s.IsInit);
        var userState1 = editorVm.States.First(s => s.Name == "New state 1");
        var userState2 = editorVm.States.First(s => s.Name == "New state 2");

        mainState.Name.Should().Be("main");
        initState.Name.Should().Be("init");
        userState1.Name.Should().Be("New state 1");
        userState2.Name.Should().Be("New state 2");
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanAddTransitions()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states first
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(4);

        // Add transition between two user states
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify transition was added
        editorVm.Transactions.Should().HaveCount(1);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanRemoveStates()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add a state
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        // Remove the user state
        editorVm.Mode = new EditorMode.RemoveState();
        editorVm.SelectedState = editorVm.States[2];
        editorVm.RemoveStateCommand.Execute(null);

        // Verify state was removed (Main, Init only)
        editorVm.States.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanRemoveTransitions()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states and transition
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(1);

        // Remove the transition
        editorVm.Mode = new EditorMode.RemoveTransition();
        editorVm.SelectedTransaction = editorVm.Transactions[0];
        editorVm.RemoveTransitionCommand.Execute(null);

        // Verify transition was removed
        editorVm.Transactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_CanConvertToLisma()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // Convert to LISMA
        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_WithStates_ConvertsToLisma()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        // Convert to LISMA
        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_StateNamesMustBeUnique()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add first state
        editorVm!.AddStateCommand.Execute(null);
        var firstStateName = editorVm.States[2].Name;

        // Try to add another state with the same name (should fail)
        editorVm.AddStateWithName(firstStateName, 200, 200);
        editorVm.States.Should().HaveCount(3); // Still 3 (Main, Init, New state 1)
    }

    [AvaloniaFact]
    public async Task BlueprintProject_EditorModes_WorkCorrectly()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Verify initial mode
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        // Set AddTransition mode
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>();

        // Reset mode
        editorVm.ResetEditorModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [AvaloniaFact]
    public async Task BlueprintProject_ModelPersistsStateChanges()
    {
        // Create blueprint project
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add states
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);

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
