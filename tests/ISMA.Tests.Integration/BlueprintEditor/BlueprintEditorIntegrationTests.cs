using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// TDD Tasks 1-3: Blueprint editor integration tests.
/// Covers: toolbar buttons, canvas rendering, state interactions.
/// </summary>
public class BlueprintEditorIntegrationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;

    [AvaloniaFact]
    public async Task NewBlueprint_CreatesTabWithToolbar()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.GetProjectCount().Should().Be(1);

        var project = _app.Window.GetActiveProject();
        project.Should().BeOfType<BlueprintProjectViewModel>();
    }

    [AvaloniaFact]
    public async Task NewStateButton_Clicked_AddsState()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        var initialStateCount = editorVm!.States.Count;
        initialStateCount.Should().Be(0); // Main and Init are separate properties

        editorVm.AddStateCommand.Execute(null);

        editorVm.States.Count.Should().Be(1);
    }

    [AvaloniaFact]
    public async Task AddTransitionToggle_TogglesMode_AndChangesText()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        editorVm.SetAddTransitionModeCommand.Execute(null);

        editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>();
        editorVm.AddTransitionButtonContent.Should().Be("Stop adding transaction");
    }

    [AvaloniaFact]
    public async Task AddTransitionToggle_ClickedAgain_ResetsMode()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm.SetAddTransitionModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>();

        editorVm.SetAddTransitionModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
        editorVm.AddTransitionButtonContent.Should().Be("Add Transition");
    }

    [AvaloniaFact]
    public async Task RemoveStateToggle_TogglesMode_AndChangesText()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        editorVm.SetRemoveStateModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.RemoveState>();
        editorVm.RemoveStateButtonContent.Should().Be("Stop removing state");

        editorVm.SetRemoveStateModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
        editorVm.RemoveStateButtonContent.Should().Be("Remove State");
    }

    [AvaloniaFact]
    public async Task RemoveTransitionToggle_TogglesMode_AndChangesText()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm.Mode.Should().BeOfType<EditorMode.Default>();

        editorVm.SetRemoveTransitionModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.RemoveTransition>();
        editorVm.RemoveTransitionButtonContent.Should().Be("Stop removing transition");

        editorVm.SetRemoveTransitionModeCommand.Execute(null);
        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
        editorVm.RemoveTransitionButtonContent.Should().Be("Remove Transition");
    }

    [AvaloniaFact]
    public async Task Canvas_ShowsMainAndInitStates_OnNewBlueprint()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm!.States.Should().HaveCount(0);
    }

    [AvaloniaFact]
    public async Task Canvas_MainState_HasCorrectColor()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        var mainState = editorVm!.MainState;

        mainState.FillColor.Should().NotBeNull();
        mainState.FillColor.Should().Be("#90EE90");
    }

    [AvaloniaFact]
    public async Task Canvas_InitState_HasCorrectColor()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        var initState = editorVm!.InitState;

        initState.FillColor.Should().NotBeNull();
        initState.FillColor.Should().Be("#ADD8E6");
    }

    [AvaloniaFact]
    public async Task Canvas_MainState_HasCorrectHeight()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        var mainState = editorVm!.MainState;

        mainState.StateHeight.Should().Be(65);
    }

    [AvaloniaFact]
    public async Task Canvas_InitState_HasCorrectHeight()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        var initState = editorVm!.InitState;

        initState.StateHeight.Should().Be(65);
    }

    [AvaloniaFact]
    public async Task Canvas_StatesHaveCorrectNames()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        var mainState = editorVm!.MainState;
        var initState = editorVm.InitState;

        mainState.Name.Should().Be("Main");
        initState.Name.Should().Be("init");
    }

    [AvaloniaFact]
    public async Task StateDrag_RepositionsState()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm!.AddStateCommand.Execute(null);

        var userState = editorVm.States.First();
        var originalX = userState.CanvasPositionX;
        var originalY = userState.CanvasPositionY;

        userState.CanvasPositionX = originalX + 50;
        userState.CanvasPositionY = originalY + 30;

        userState.CanvasPositionX.Should().Be(originalX + 50);
        userState.CanvasPositionY.Should().Be(originalY + 30);
    }

    [AvaloniaFact]
    public async Task UserState_IsEditable()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        editorVm!.AddStateCommand.Execute(null);

        var userState = editorVm.States.First();

        userState.IsEditable.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task UserState_HasCorrectHeight()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm!.AddStateCommand.Execute(null);

        var userState = editorVm.States.First();
        userState.StateHeight.Should().Be(65);
    }

    [AvaloniaFact]
    public async Task UserState_HasCoralColor()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm!.AddStateCommand.Execute(null);

        var userState = editorVm.States.First();
        userState.FillColor.Should().Be("#F08080");
    }

    [AvaloniaFact]
    public async Task StateDoubleClicked_OpensTextEditorTab()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        var mainState = editorVm!.MainState;

        bool eventFired = false;
        BlueprintStateViewModel? eventState = null;
        editorVm.StateTextEditorRequested += state =>
        {
            eventFired = true;
            eventState = state;
        };

        editorVm.OpenStateTextEditor(mainState);

        eventFired.Should().BeTrue();
        eventState.Should().Be(mainState);
    }

    [AvaloniaFact]
    public async Task StateDoubleClicked_InitState_OpensTextEditorTab()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;
        var initState = editorVm!.InitState;

        bool eventFired = false;
        BlueprintStateViewModel? eventState = null;
        editorVm.StateTextEditorRequested += state =>
        {
            eventFired = true;
            eventState = state;
        };

        editorVm.OpenStateTextEditor(initState);

        eventFired.Should().BeTrue();
        eventState.Should().Be(initState);
    }

    [AvaloniaFact]
    public async Task StateDoubleClicked_UserState_OpensTextEditorTab()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        var blueprintProject = _app.Window.GetActiveProject() as BlueprintProjectViewModel;
        var editorVm = (BlueprintEditorViewModel)blueprintProject!.EditorContent!;

        editorVm!.AddStateCommand.Execute(null);

        var userState = editorVm.States.First();

        bool eventFired = false;
        BlueprintStateViewModel? eventState = null;
        editorVm.StateTextEditorRequested += state =>
        {
            eventFired = true;
            eventState = state;
        };

        editorVm.OpenStateTextEditor(userState);

        eventFired.Should().BeTrue();
        eventState.Should().Be(userState);
    }
}
