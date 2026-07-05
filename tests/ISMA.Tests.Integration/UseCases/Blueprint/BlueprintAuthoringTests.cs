using Avalonia;
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
public class AuthorBlueprintTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC02_NewBlueprint_CreatesInitialStatechart()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var model = blueprintProject!.GetBlueprintModel();
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
        model.Main.Name.Should().Be("main");
        model.Init.Name.Should().Be("init");
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_CanAddStates()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(4);
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_CanAddTransitions()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(4);

        editorVm!.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm!.SetTransitionSource(editorVm!.States[2]);
        editorVm!.SelectedState = editorVm!.States[3];
        editorVm!.AddTransitionCommand.Execute(null);

        editorVm!.Transactions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_RejectsDuplicateStateNames()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(3);

        var newStateName = editorVm!.States[2].Name;
        editorVm!.AddStateWithName(newStateName, 200, 200);
        editorVm!.States.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_AllowsUniqueStateNames()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(3);

        editorVm!.AddStateWithName("UniqueState", 200, 200);
        editorVm!.States.Should().HaveCount(4);
        editorVm!.States[3].Name.Should().Be("UniqueState");
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_PreservesTransactionReferencesAfterRename()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();
        editorVm!.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm!.SetTransitionSource(editorVm!.States[2]);
        editorVm!.SelectedState = editorVm!.States[3];
        editorVm!.AddTransitionCommand.Execute(null);

        editorVm!.Transactions.Should().HaveCount(1);
        var oldTxStartState = editorVm!.Transactions[0].StartState.Name;

        editorVm!.UpdateStateName(editorVm!.States[2], "RenamedState");
        editorVm!.Transactions.Should().HaveCount(1);
        editorVm!.Transactions[0].StartState.Name.Should().Be("RenamedState");
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
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(3);

        editorVm!.Mode = new EditorMode.RemoveState();
        editorVm!.SelectedState = editorVm!.States[2];
        editorVm!.RemoveStateCommand.Execute(null);

        editorVm!.States.Should().HaveCount(2);
    }
}

public class BlueprintToVisualizationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC11_Blueprint_ConvertsToLismaText()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task UC11_Blueprint_WithTransitions_ConvertsCorrectly()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        _app.Window.ClickAddStateButton();
        _app.Window.GetStateBoxCount().Should().Be(4);

        editorVm!.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm!.SetTransitionSource(editorVm!.States[2]);
        editorVm!.SelectedState = editorVm!.States[3];
        editorVm!.AddTransitionCommand.Execute(null);

        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task UC11_Blueprint_ModelPersistsStateChanges()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        _app.Window.ClickAddStateButton();
        editorVm!.GetBlueprintModel().States.Should().HaveCount(1);
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
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(1);

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        _app.Window.ClickAddStateButton();

        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "blueprint-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        _app.ViewModel.SimulationService.IsRunning.Should().BeFalse();
    }
}
