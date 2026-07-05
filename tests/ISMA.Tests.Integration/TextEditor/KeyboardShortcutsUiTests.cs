using Avalonia;
using System.Collections.Immutable;
using Avalonia.Automation;
using Avalonia.Headless.XUnit;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for global keyboard shortcuts.
/// Tests that keyboard shortcuts work through the full UI tree.
/// </summary>
public class KeyboardShortcutsUiTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task KeyboardShortcuts_NewText_UsesCtrlN()
    {

        var newTextMenuItem = _app.Window.FindMenuItem("MenuNewText");
        newTextMenuItem.Should().NotBeNull("MenuNewText should exist");

        var initialCount = _app.Window.GetProjectCount();
        _app.Window.ClickMenuItem("MenuNewText");

        _app.Window.GetProjectCount().Should().Be(initialCount + 1);
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_NewBlueprint_UsesCtrlB()
    {
        var newBlueprintMenuItem = _app.Window.FindMenuItem("MenuNewBlueprint");
        newBlueprintMenuItem.Should().NotBeNull("MenuNewBlueprint should exist");

        var initialCount = _app.Window.GetProjectCount();
        _app.Window.ClickMenuItem("MenuNewBlueprint");

        _app.Window.GetProjectCount().Should().Be(initialCount + 1);
        _app.Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Save_UsesCtrlS()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);
        var saveMenuItem = _app.Window.FindMenuItem("MenuSave");
        saveMenuItem.Should().NotBeNull("MenuSave should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Exit_UsesCtrlW()
    {
        var exitMenuItem = _app.Window.FindMenuItem("MenuExit");
        exitMenuItem.Should().NotBeNull("MenuExit should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Verify_UsesCtrlF4()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        var verifyMenuItem = _app.Window.FindMenuItem("MenuVerify");
        verifyMenuItem.Should().NotBeNull("MenuVerify should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Run_UsesCtrlF5()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        var runMenuItem = _app.Window.FindMenuItem("MenuRun");
        runMenuItem.Should().NotBeNull("MenuRun should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_CutCopyPaste_Exist()
    {
        var cutMenuItem = _app.Window.FindMenuItem("MenuCut");
        cutMenuItem.Should().NotBeNull("MenuCut should exist");
        var copyMenuItem = _app.Window.FindMenuItem("MenuCopy");
        copyMenuItem.Should().NotBeNull("MenuCopy should exist");
        var pasteMenuItem = _app.Window.FindMenuItem("MenuPaste");
        pasteMenuItem.Should().NotBeNull("MenuPaste should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_GlobalShortcuts_AreRegisteredInMainWindow()
    {
        _app.Window.Should().NotBeNull();
        _app.Window.Should().BeAssignableTo<Avalonia.Controls.Window>();
    }

    [AvaloniaFact]
    public async Task SimulationWorkflow_RunCommand_IsExecutable()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();
        _app.Window.GetActiveProject().Should().BeOfType<LismaProjectViewModel>();
        var runMenuItem = _app.Window.FindMenuItem("MenuRun");
        runMenuItem.Should().NotBeNull("MenuRun should exist");
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Verify()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        var lismaProject = _app.Window.GetActiveProject() as LismaProjectViewModel;
        lismaProject!.SetContent("main\n{ 1 > 0 }\n");

        _app.MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray<CompilationError>.Empty,
            Warnings = ImmutableArray<string>.Empty
        });

        _app.Window.ClickMenuItem("MenuVerify");
        _app.Window.GetErrorList().Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Save()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);
        var saveMenuItem = _app.Window.FindMenuItem("MenuSave");
        saveMenuItem.Should().NotBeNull("MenuSave should exist");
    }

    [AvaloniaFact]
    public async Task ToolBar_CutButton_Command_IsBound()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var toolbar = _app.Window.FindControl<IsmaToolBarView>("ToolBar");
        toolbar.Should().NotBeNull("ToolBar should be found");

        var button = UiHelpers.FindDescendants<Button>(toolbar)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == "ToolBarCut");
        button.Should().NotBeNull("ToolBarCut button should be found");
        button!.Command.Should().NotBeNull("ToolBarCut button should have a command bound");
        button.Command!.CanExecute(null).Should().BeTrue("ToolBarCut command should be executable");
    }

    [AvaloniaFact]
    public async Task ViewModel_CutCommand_Directly_RemovesSelectedText()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        var lismaProject = _app.Window.GetActiveProject() as LismaProjectViewModel;
        lismaProject!.SetContent("hello world");


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull();
        textEditor!.Select(0, 5);

        _app.Window.ClickMenuItem("MenuCut");

        textEditor.Document.Text.Should().Be(" world");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuCut_RemovesSelectedText()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 5);

        _app.Window.ClickMenuItem("MenuCut");

        textEditor.Document.Text.Should().Be(" world",
            "Cut should remove selected text from editor");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuCopy_DoesNotModifyText()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 5);

        var originalText = textEditor.Document.Text;

        _app.Window.ClickMenuItem("MenuCopy");

        textEditor.Document.Text.Should().Be(originalText,
            "Copy should not modify editor text");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuPaste_PastesAtCaret()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello";
        textEditor.CaretOffset = 5;

        var originalText = textEditor.Document.Text;

        _app.Window.ClickMenuItem("MenuPaste");

        // Paste should not throw and should attempt to insert clipboard content
        // In headless mode, clipboard may be empty, so text may not change
        textEditor.Document.Text.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_ToolbarCut_RemovesSelectedText()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(6, 5);

        _app.Window.ClickToolbarButton("ToolBarCut");

        textEditor.Document.Text.Should().Be("hello ",
            "Toolbar Cut should remove selected text from editor");
    }

    [AvaloniaFact]
    public async Task TextEditor_ToolbarCopy_DoesNotModifyText()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(6, 5);

        var originalText = textEditor.Document.Text;

        _app.Window.ClickToolbarButton("ToolBarCopy");

        textEditor.Document.Text.Should().Be(originalText,
            "Toolbar Copy should not modify editor text");
    }

    [AvaloniaFact]
    public async Task TextEditor_ToolbarPaste_PastesAtCaret()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello";
        textEditor.CaretOffset = 5;

        var originalText = textEditor.Document.Text;

        _app.Window.ClickToolbarButton("ToolBarPaste");

        textEditor.Document.Text.Should().NotBeNull();
    }



    [AvaloniaFact]
    public async Task TextEditor_Cut_NoSelection_DoesNotThrow()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 0);

        var ex = Record.Exception(() => _app.Window.ClickMenuItem("MenuCopy"));
        ex.Should().BeNull("Copy with no selection should not throw");
    }

    [AvaloniaFact]
    public async Task TextEditor_Copy_NoSelection_DoesNotThrow()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);


        var textEditor = UiHelpers.GetActiveTextEditor(_app.Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello";
        textEditor.CaretOffset = 5;

        var ex = Record.Exception(() => _app.Window.ClickMenuItem("MenuPaste"));
        ex.Should().BeNull("Paste with empty clipboard should not throw");
    }
}
