global using global::Xunit;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.App.ViewModels;
using ISMA.BlueprintEditor.Controls;
using ISMA.BlueprintEditor.Views;
using ISMA.TextEditor;

namespace ISMA.Tests.Integration.BlueprintEditor;

/// <summary>
/// Honest UI interaction tests for the blueprint editor (design doc §6):
/// real pointer events (MouseDown/MouseMove/MouseUp) and real key input,
/// assertions on the visual tree (realized controls, rendered positions,
/// realized text). No ViewModel fallbacks, no direct command execution,
/// no Raise* hooks. The native file picker cannot be driven headless, so
/// the save/reload round-trip uses the project service for the file part
/// (same approach as OpenProjectFileTests).
/// </summary>
public class BlueprintEditorInteractionTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;

    // ------------------------------------------------------------------
    // Visual-tree access
    // ------------------------------------------------------------------

    private StateBox[] GetStateBoxes() => UiHelpers.FindDescendants<StateBox>(_app.Window).ToArray();

    private TransactionArrow[] GetArrows() => UiHelpers.FindDescendants<TransactionArrow>(_app.Window).ToArray();

    private LoopTransactionArrow[] GetLoopArrows() => UiHelpers.FindDescendants<LoopTransactionArrow>(_app.Window).ToArray();

    private EditArrowPopOver[] GetPopovers() => UiHelpers.FindDescendants<EditArrowPopOver>(_app.Window).ToArray();

    private BlueprintCanvas GetCanvas()
    {
        var canvas = _app.Window.GetSelfAndVisualDescendants()
            .OfType<BlueprintCanvas>()
            .FirstOrDefault(c => c.Name == "RootCanvas");
        canvas.Should().NotBeNull("RootCanvas of the diagram");
        return canvas!;
    }

    private StateBox BoxWithName(string name)
    {
        var box = GetStateBoxes().FirstOrDefault(b => NameLabel(b)?.Text == name);
        box.Should().NotBeNull($"StateBox with name label '{name}'");
        return box!;
    }

    private static TextBlock? NameLabel(StateBox box) =>
        box.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();

    private static TextBox? NameEditor(StateBox box) =>
        box.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();

    // ------------------------------------------------------------------
    // Real input
    // ------------------------------------------------------------------

    private void ClickAt(Point point)
    {
        _app.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        _app.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
    }

    private void ClickControl(Control control) =>
        ClickAt(InWindow(control, new Point(control.Bounds.Width / 2, control.Bounds.Height / 2)));

    private Point InWindow(Control control, Point local)
    {
        var p = control.TranslatePoint(local, _app.Window);
        p.Should().NotBeNull($"translate {control.GetType().Name} to window");
        return p.Value;
    }

    private static Point InCanvas(Control control, Visual canvas)
    {
        var p = control.TranslatePoint(new Point(0, 0), canvas);
        p.Should().NotBeNull($"translate {control.GetType().Name} to canvas");
        return p.Value;
    }

    /// <summary>Waits out the 200 ms single/double click disambiguation window.</summary>
    private async Task WaitClickDisambiguation() => await Task.Delay(350);

    private void DragControl(Control control, Vector delta)
    {
        var start = InWindow(control, new Point(control.Bounds.Width / 2, control.Bounds.Height / 2));

        _app.Window.MouseDown(start, MouseButton.Left, RawInputModifiers.None);
        for (var i = 1; i <= 5; i++)
        {
            var p = start + new Vector(delta.X * i / 5, delta.Y * i / 5);
            _app.Window.MouseMove(p, RawInputModifiers.LeftMouseButton);
        }

        _app.Window.MouseUp(start + delta, MouseButton.Left, RawInputModifiers.None);
    }

    private void ClickToolbarButton(string content)
    {
        var button = _app.Window.GetSelfAndVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.Content as string == content);
        button.Should().NotBeNull($"toolbar button '{content}'");
        ClickControl(button!);
    }
    /// <summary>Creates a new blueprint project and waits until the editor is realized in the visual tree.</summary>
    private async Task NewBlueprintProject()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        for (var i = 0; i < 50; i++)
        {
            if (UiHelpers.FindDescendants<IsmaBlueprintEditor>(_app.Window).FirstOrDefault() is { } editor)
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new InvalidOperationException("blueprint editor did not materialize in the visual tree");
    }


    /// <summary>Builds the standard A -> B arrow scenario: two states, B dragged right, transition added.</summary>
    private async Task<(StateBox A, StateBox B)> CreateConnectedStates()
    {
        await NewBlueprintProject();
        ClickToolbarButton("New state");
        ClickToolbarButton("New state");
        await Task.Delay(100);

        var a = BoxWithName("State 1");
        var b = BoxWithName("State 2");
        DragControl(b, new Vector(200, 0)); // un-overlap the boxes (both spawn at 10,200)
        await Task.Delay(100);

        ClickToolbarButton("New transition");
        ClickControl(a);
        await WaitClickDisambiguation();
        ClickControl(b);
        await WaitClickDisambiguation();

        GetArrows().Should().HaveCount(1, "the transition should render an arrow");
        return (a, b);
    }

    private static Point CenterInCanvas(Control control, BlueprintCanvas canvas)
    {
        var p = control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), canvas);
        p.Should().NotBeNull();
        return p.Value;
    }

    // ------------------------------------------------------------------
    // Tests
    // ------------------------------------------------------------------

    [AvaloniaFact]
    public async Task NewStateButton_AddsStateRenderedAtExpectedPosition()
    {
        await NewBlueprintProject();
        var canvas = GetCanvas();
        GetStateBoxes().Should().HaveCount(2, "Main and init exist on creation");

        ClickToolbarButton("New state");
        await Task.Delay(100);

        GetStateBoxes().Should().HaveCount(3);

        var box = BoxWithName("State 1");
        var topLeft = InCanvas(box, canvas);
        topLeft.X.Should().BeApproximately(10, 1, "default state X");
        topLeft.Y.Should().BeApproximately(200, 1, "default state Y");
        box.Bounds.Width.Should().BeApproximately(110, 1, "default state width");
        box.Bounds.Height.Should().BeApproximately(65, 1, "default state height");
    }

    [AvaloniaFact]
    public async Task DragStateBox_MovesBoxInCanvas()
    {
        await NewBlueprintProject();
        var canvas = GetCanvas();
        ClickToolbarButton("New state");
        await Task.Delay(100);

        var box = BoxWithName("State 1");
        var before = InCanvas(box, canvas);

        DragControl(box, new Vector(50, 30));
        await Task.Delay(100);

        var after = InCanvas(box, canvas);
        (after.X - before.X).Should().BeApproximately(50, 1, "drag delta X");
        (after.Y - before.Y).Should().BeApproximately(30, 1, "drag delta Y");
    }

    [AvaloniaFact]
    public async Task TwoStateClicksInAddTransitionMode_RenderArrow()
    {
        var (a, b) = await CreateConnectedStates();

        var canvas = GetCanvas();
        var arrow = GetArrows().Single();
        arrow.StartState.Should().BeSameAs(a.State, "arrow source");
        arrow.EndState.Should().BeSameAs(b.State, "arrow target");

        // The arrow line connects the state centers, offset perpendicularly (10 px).
        var ac = CenterInCanvas(a, canvas);
        var bc = CenterInCanvas(b, canvas);
        (bc.X - ac.X).Should().BeApproximately(200, 1, "states are separated");
    }

    [AvaloniaFact]
    public async Task SameStateTwiceInAddTransitionMode_CreatesLoopArrow()
    {
        await NewBlueprintProject();
        ClickToolbarButton("New state");
        await Task.Delay(100);

        var box = BoxWithName("State 1");

        ClickToolbarButton("New transition");
        ClickControl(box);
        // Wait beyond the double-click window so the second press is a distinct single click.
        await Task.Delay(600);
        ClickControl(box);
        await WaitClickDisambiguation();

        GetArrows().Should().BeEmpty("a same-state pair is a loop, not a transition");
        GetLoopArrows().Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task RemoveStateMode_RemovesClickedStateBox()
    {
        await NewBlueprintProject();
        ClickToolbarButton("New state");
        await Task.Delay(100);
        GetStateBoxes().Should().HaveCount(3);

        ClickToolbarButton("Remove state");
        ClickControl(BoxWithName("State 1"));
        await WaitClickDisambiguation();

        GetStateBoxes().Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task RemoveStateMode_DoesNotRemoveMainOrInit()
    {
        await NewBlueprintProject();

        ClickToolbarButton("Remove state");
        ClickControl(BoxWithName("Main"));
        await WaitClickDisambiguation();
        ClickControl(BoxWithName("init"));
        await WaitClickDisambiguation();

        GetStateBoxes().Should().HaveCount(2, "Main and init are protected");
    }

    [AvaloniaFact]
    public async Task RemoveTransitionMode_BodyClickRemovesArrow()
    {
        var (a, b) = await CreateConnectedStates();
        var canvas = GetCanvas();

        ClickToolbarButton("Remove transition");

        // Click the line body 40 px from the arrowhead (arrowhead hit tolerance is 8 px).
        var ac = CenterInCanvas(a, canvas);
        var bc = CenterInCanvas(b, canvas);
        var mid = new Point((ac.X + bc.X) / 2, (ac.Y + bc.Y) / 2);
        ClickAt(InWindow(canvas, new Point(mid.X - 40, mid.Y - 10)));
        await Task.Delay(100);

        GetArrows().Should().BeEmpty();
        GetStateBoxes().Should().HaveCount(4, "removing a transition keeps the states");
    }

    [AvaloniaFact]
    public async Task ArrowheadClick_ShowsPopover_WhichClosesOnPointerExit()
    {
        var (a, b) = await CreateConnectedStates();
        var canvas = GetCanvas();

        var ac = CenterInCanvas(a, canvas);
        var bc = CenterInCanvas(b, canvas);
        var head = new Point((ac.X + bc.X) / 2, (ac.Y + bc.Y) / 2 - 10);
        ClickAt(InWindow(canvas, head));
        await Task.Delay(100);

        GetPopovers().Should().HaveCount(1, "arrowhead click opens the edit popover");

        // Move the pointer away from the popover -> PointerExited closes it.
        _app.Window.MouseMove(new Point(5, 5), RawInputModifiers.None);
        await Task.Delay(100);

        GetPopovers().Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task InlineRename_ViaRealClicksAndKeyInput_UpdatesNameLabel()
    {
        await NewBlueprintProject();
        ClickToolbarButton("New state");
        await Task.Delay(100);

        var box = BoxWithName("State 1");
        ClickControl(box); // single click on a user state starts name editing
        await WaitClickDisambiguation();

        var editor = NameEditor(box);
        editor.Should().NotBeNull();
        editor!.IsVisible.Should().BeTrue("name editor is shown in edit mode");
        NameLabel(box)!.IsVisible.Should().BeFalse();

        editor.Focus();
        editor.SelectAll();
        _app.Window.KeyTextInput("Renamed");
        _app.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        await Task.Delay(100);

        editor.IsVisible.Should().BeFalse("edit mode ends after commit");
        NameLabel(box)!.Text.Should().Be("Renamed");
    }

    [AvaloniaFact]
    public async Task DoubleClickState_OpensTextTabWithRealizedEditor()
    {
        await NewBlueprintProject();
        ClickToolbarButton("New state");
        await Task.Delay(100);

        var box = BoxWithName("State 1");
        var center = InWindow(box, new Point(box.Bounds.Width / 2, box.Bounds.Height / 2));
        ClickAt(center);
        ClickAt(center); // second press within the window -> double click
        await Task.Delay(100);

        var editorControl = UiHelpers.FindDescendants<IsmaBlueprintEditor>(_app.Window).Single();
        var tabs = editorControl.GetVisualDescendants().OfType<TabControl>().Single();
        tabs.Items.Should().HaveCount(2, "diagram tab + state body editor tab");

        UiHelpers.FindDescendants<IsmaTextEditor>(_app.Window).Should().HaveCount(1,
            "the state body tab contains a realized text editor");
    }

    [AvaloniaFact]
    public async Task SaveAndReload_RoundTripsUiEditedDiagramThroughIscm2()
    {
        var (a, b) = await CreateConnectedStates();
        var blueprint = (BlueprintProjectViewModel)_app.Window.GetActiveProject()!;
        GetStateBoxes().Should().HaveCount(4);

        var dir = Directory.CreateTempSubdirectory("isma-roundtrip-ui");
        try
        {
            var path = Path.Combine(dir.FullName, "ui-roundtrip.iscm2");
            blueprint.FilePath = path;
            (await blueprint.SaveAsync()).Should().BeTrue();

            var projectService = _app.GetRequiredService<ProjectService>();
            var reloaded = await projectService.OpenAsync(path);
            _app.ViewModel.SyncProjects();

            reloaded.Should().BeOfType<BlueprintProjectViewModel>();
            var model = ((BlueprintProjectViewModel)reloaded!).GetBlueprintModel();
            model.States.Should().HaveCount(2);
            model.States.Select(s => s.Name).Should().BeEquivalentTo("State 1", "State 2");
            model.Transactions.Should().HaveCount(1);
            model.Transactions[0].StartStateName.Should().Be(a.State!.Name);
            model.Transactions[0].EndStateName.Should().Be(b.State!.Name);
        }
        finally
        {
            dir.Delete(true);
        }
    }
}
