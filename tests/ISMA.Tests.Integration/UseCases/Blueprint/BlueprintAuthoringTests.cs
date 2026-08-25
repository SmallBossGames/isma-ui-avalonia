using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using FluentAssertions;
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
        model.Main.Name.Should().Be("Main");
        model.Init.Name.Should().Be("init");
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_CanAddStates()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(2);
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_CanAddTransitions()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(2);

        _app.Window.ClickAddTransitionToggle();

        _app.Window.ClickStateBoxOnCanvas("State 1");
        _app.Window.ClickStateBoxOnCanvas("State 2");

        _app.Window.GetArrowLineCount().Should().Be(1);

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        var model = editorVm!.GetBlueprintModel();
        model.Transactions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_RejectsDuplicateStateNames()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(1);

        var newState = _app.Window.GetStateBoxByName("State 1");
        newState.Should().NotBeNull();

        // Try to rename "State 1" to "Main" (duplicate of Main state)
        // The NameChangingMonitor should reject this
        var initialCount = _app.Window.GetStateBoxCount();

        // Note: Inline editing allows setting any name, but the ViewModel's
        // NameChangingMonitor rejects duplicates. Since we're using ViewModel-based
        // rename in headless mode, duplicate names should be rejected.
        _app.Window.RenameStateBoxByName("State 1", "Main");

        // State count should remain the same
        _app.Window.GetStateBoxCount().Should().Be(initialCount);
        // "State 1" should still exist (rename was rejected)
        _app.Window.GetStateBoxByName("State 1").Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_AllowsUniqueStateNames()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(1);

        _app.Window.RenameStateBoxByName("State 1", "UniqueState");

        _app.Window.GetStateBoxByName("UniqueState").Should().NotBeNull();

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(2);
        _app.Window.GetStateBoxByName("State 2").Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task UC02_BlueprintEditor_PreservesTransactionReferencesAfterRename()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddTransitionToggle();

        _app.Window.ClickStateBoxOnCanvas("State 1");
        _app.Window.ClickStateBoxOnCanvas("State 2");

        _app.Window.GetArrowLineCount().Should().Be(1);

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        var modelBefore = editorVm!.GetBlueprintModel();
        modelBefore.Transactions.Should().HaveCount(1);
        var txStartStateId = modelBefore.Transactions[0].StartStateId;

        _app.Window.RenameStateBoxByName("State 1", "RenamedState");

        var modelAfter = editorVm.GetBlueprintModel();
        modelAfter.Transactions.Should().HaveCount(1);
        modelAfter.Transactions[0].StartStateId.Should().Be(txStartStateId);
    }

    [AvaloniaFact]
    public async Task UC02_NameChangingMonitor_CreatesIncrementingNames()
    {
        var monitor = new NameChangingMonitor();
        var name1 = monitor.CreateNextDefaultName();
        var name2 = monitor.CreateNextDefaultName();
        var name3 = monitor.CreateNextDefaultName();

        name1.Should().Be("State 1");
        name2.Should().Be("State 2");
        name3.Should().Be("State 3");
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

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(1);

        _app.Window.ClickRemoveStateToggle();

        _app.Window.ClickStateBoxOnCanvas("State 1");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        var model = editorVm!.GetBlueprintModel();
        model.States.Should().BeEmpty(); // No user-added states after removing the only one
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
    }

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

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddStateButton();

        _app.Window.GetStateBoxCount().Should().Be(2);

        _app.Window.ClickAddTransitionToggle();

        _app.Window.ClickStateBoxOnCanvas("State 1");
        _app.Window.ClickStateBoxOnCanvas("State 2");

        _app.Window.GetArrowLineCount().Should().Be(1);

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task UC11_Blueprint_ModelPersistsStateChanges()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.ClickAddStateButton();

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        // Get the model from the editor (which tracks changes), not the project's initial model
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        var model = editorVm!.GetBlueprintModel();
        model.States.Should().HaveCount(1); // Only user-added states (Main and Init are separate properties)
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
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

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddStateButton();

        _app.Window.ClickAddTransitionToggle();

        _app.Window.ClickStateBoxOnCanvas("State 1");
        _app.Window.ClickStateBoxOnCanvas("State 2");

        _app.Window.GetArrowLineCount().Should().Be(1);

        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime = 0.0;
        _app.ViewModel.SimulationParameters.CauchyInitials.EndTime = 10.0;

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "blueprint-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new Domain.Dtos.CachedSimulationResult { File = "/tmp/result.bin" });

        _app.Window.ClickMenuItem("MenuRun");

        _app.Window.IsRunButtonEnabled().Should().BeTrue();
    }
}
