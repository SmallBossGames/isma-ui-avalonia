using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Conversion;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration.UseCases.Blueprint;

/// <summary>
/// UC-02: Create and Run a Blueprint Statechart Project
/// UC-11: Complete Workflow - Blueprint Authoring to Visualization
/// Tests blueprint creation, editing, conversion, and simulation.
/// </summary>
public class AuthorBlueprintTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC02_NewBlueprint_CreatesInitialStatechart()
    {
        // Create blueprint project via UI
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();

        // Verify blueprint has initial states
        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var model = blueprintProject!.GetBlueprintModel();
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
        model.Main.Name.Should().Be("main");
        model.Init.Name.Should().Be("init");
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_CanAddStates()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);

        editorVm.States.Should().HaveCount(4); // Main, Init, State1, State2
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_CanAddTransitions()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(4);

        editorVm.CurrentMode = BlueprintEditorMode.AddTransition;
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_RejectsDuplicateStateNames()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        var newStateName = editorVm.States[2].Name;
        editorVm.AddStateWithName(newStateName, 200, 200);
        editorVm.States.Should().HaveCount(3); // No new state added
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_AllowsUniqueStateNames()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        editorVm.AddStateWithName("UniqueState", 200, 200);
        editorVm.States.Should().HaveCount(4);
        editorVm.States[3].Name.Should().Be("UniqueState");
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_PreservesTransactionReferencesAfterRename()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.CurrentMode = BlueprintEditorMode.AddTransition;
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Transactions.Should().HaveCount(1);
        var oldTxStartState = editorVm.Transactions[0].StartState.Name;

        editorVm.UpdateStateName(editorVm.States[2], "RenamedState");
        editorVm.Transactions.Should().HaveCount(1);
        editorVm.Transactions[0].StartState.Name.Should().Be("RenamedState");
    }

    [AvaloniaFact]
    public async Task UC02_NameChangingMonitor_CreatesIncrementingNames()
    {
        var monitor = new NameChangingMonitor();
        var name1 = monitor.CreateNextDefaultName();
        var name2 = monitor.CreateNextDefaultName();
        var name3 = monitor.CreateNextDefaultName();

        name1.Should().Be("New state 1");
        name2.Should().Be("New state 2");
        name3.Should().Be("New state 3");
    }

    [AvaloniaFact]
    public async Task UC02_NameChangingMonitor_RejectsDuplicateNames()
    {
        var monitor = new NameChangingMonitor();
        monitor.TryRegister("Main").Should().BeTrue();
        monitor.TryRegister("Main").Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_RemovesState()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        // Remove the added state
        editorVm.CurrentMode = BlueprintEditorMode.RemoveState;
        editorVm.SelectedState = editorVm.States[2];
        editorVm.RemoveStateCommand.Execute(null);

        editorVm.States.Should().HaveCount(2); // Back to Main and Init
    }
}

public class BlueprintToVisualizationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC11_Blueprint_ConvertsToLismaText()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task UC11_Blueprint_WithTransitions_ConvertsCorrectly()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(4);

        editorVm.CurrentMode = BlueprintEditorMode.AddTransition;
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task UC11_Blueprint_ModelPersistsStateChanges()
    {
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();

        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        editorVm.GetBlueprintModel().States.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task UC11_EmptyBlueprint_ConvertsToMinimalLisma()
    {
        var model = BlueprintModel.Empty;
        var lisma = BlueprintToLismaConverter.ConvertToLisma(model);
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task UC11_BlueprintSimulation_FullPipeline()
    {
        // Create blueprint project
        Window.ClickMenuItem("MenuNewBlueprint");
        Window.Flush();
        ViewModel.Projects.Should().HaveCount(1);

        // Add a state
        var blueprintProject = Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        editorVm!.AddStateCommand.Execute(null);

        // Configure simulation parameters
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;

        // Mock server
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "blueprint-model" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        // Run simulation (this should convert blueprint to LISMA and simulate)
        Window.ClickMenuItem("MenuRun");
        Window.Flush();

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }
}
