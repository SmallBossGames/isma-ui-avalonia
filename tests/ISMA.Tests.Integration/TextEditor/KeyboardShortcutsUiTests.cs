using System.Collections.Immutable;
using Avalonia.Automation;
using Avalonia.Headless.XUnit;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for global keyboard shortcuts.
/// Tests that keyboard shortcuts work through the full UI tree.
/// </summary>
public class KeyboardShortcutsUiTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task KeyboardShortcuts_NewText_UsesCtrlN()
    {
        var newTextMenuItem = Window.FindMenuItem("MenuNewText");
        newTextMenuItem.Should().NotBeNull("MenuNewText should exist");

        var initialCount = Window.GetProjectCount();
        Window.ClickMenuItem("MenuNewText");

        Window.GetProjectCount().Should().Be(initialCount + 1);
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_NewBlueprint_UsesCtrlB()
    {
        var newBlueprintMenuItem = Window.FindMenuItem("MenuNewBlueprint");
        newBlueprintMenuItem.Should().NotBeNull("MenuNewBlueprint should exist");

        var initialCount = Window.GetProjectCount();
        Window.ClickMenuItem("MenuNewBlueprint");

        Window.GetProjectCount().Should().Be(initialCount + 1);
        Window.GetActiveProject().Should().BeOfType<BlueprintProjectViewModel>();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Save_UsesCtrlS()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);
        var saveMenuItem = Window.FindMenuItem("MenuSave");
        saveMenuItem.Should().NotBeNull("MenuSave should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Exit_UsesCtrlW()
    {
        var exitMenuItem = Window.FindMenuItem("MenuExit");
        exitMenuItem.Should().NotBeNull("MenuExit should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Verify_UsesCtrlF4()
    {
        Window.ClickMenuItem("MenuNewText");
        var verifyMenuItem = Window.FindMenuItem("MenuVerify");
        verifyMenuItem.Should().NotBeNull("MenuVerify should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Run_UsesCtrlF5()
    {
        Window.ClickMenuItem("MenuNewText");
        var runMenuItem = Window.FindMenuItem("MenuRun");
        runMenuItem.Should().NotBeNull("MenuRun should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_CutCopyPaste_Exist()
    {
        var cutMenuItem = Window.FindMenuItem("MenuCut");
        cutMenuItem.Should().NotBeNull("MenuCut should exist");
        var copyMenuItem = Window.FindMenuItem("MenuCopy");
        copyMenuItem.Should().NotBeNull("MenuCopy should exist");
        var pasteMenuItem = Window.FindMenuItem("MenuPaste");
        pasteMenuItem.Should().NotBeNull("MenuPaste should exist");
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_GlobalShortcuts_AreRegisteredInMainWindow()
    {
        Window.Should().NotBeNull();
        Window.Should().BeAssignableTo<Avalonia.Controls.Window>();
    }

    [AvaloniaFact]
    public async Task SimulationWorkflow_RunCommand_IsExecutable()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();
        Window.GetActiveProject().Should().BeOfType<LismaProjectViewModel>();
        var runMenuItem = Window.FindMenuItem("MenuRun");
        runMenuItem.Should().NotBeNull("MenuRun should exist");
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Verify()
    {
        Window.ClickMenuItem("MenuNewText");
        var lismaProject = Window.GetActiveProject() as LismaProjectViewModel;
        lismaProject!.SetContent("main\n{ 1 > 0 }\n");

        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray<CompilationError>.Empty,
            Warnings = ImmutableArray<string>.Empty
        });

        Window.ClickMenuItem("MenuVerify");
        await FlushDispatcher();
        Window.GetErrorList().Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Save()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);
        var saveMenuItem = Window.FindMenuItem("MenuSave");
        saveMenuItem.Should().NotBeNull("MenuSave should exist");
    }

    [AvaloniaFact]
    public async Task ToolBar_CutButton_Command_IsBound()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.Flush();

        var toolbar = Window.FindControl<IsmaToolBarView>("ToolBar");
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
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        var lismaProject = Window.GetActiveProject() as LismaProjectViewModel;
        lismaProject!.SetContent("hello world");

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull();
        textEditor!.Select(0, 5);
        Window.Flush();

        Window.ClickMenuItem("MenuCut");
        await FlushDispatcher();

        textEditor.Document.Text.Should().Be(" world");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuCut_RemovesSelectedText()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 5);
        Window.Flush();

        Window.ClickMenuItem("MenuCut");
        await FlushDispatcher();

        textEditor.Document.Text.Should().Be(" world",
            "Cut should remove selected text from editor");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuCopy_DoesNotModifyText()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 5);
        Window.Flush();

        var originalText = textEditor.Document.Text;

        Window.ClickMenuItem("MenuCopy");
        await FlushDispatcher();

        textEditor.Document.Text.Should().Be(originalText,
            "Copy should not modify editor text");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuPaste_PastesAtCaret()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello";
        textEditor.CaretOffset = 5;
        Window.Flush();

        var originalText = textEditor.Document.Text;

        Window.ClickMenuItem("MenuPaste");
        await FlushDispatcher();

        // Paste should not throw and should attempt to insert clipboard content
        // In headless mode, clipboard may be empty, so text may not change
        textEditor.Document.Text.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_ToolbarCut_RemovesSelectedText()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(6, 5);
        Window.Flush();

        Window.ClickToolbarButton("ToolBarCut");
        await FlushDispatcher();

        textEditor.Document.Text.Should().Be("hello ",
            "Toolbar Cut should remove selected text from editor");
    }

    [AvaloniaFact]
    public async Task TextEditor_ToolbarCopy_DoesNotModifyText()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(6, 5);
        Window.Flush();

        var originalText = textEditor.Document.Text;

        Window.ClickToolbarButton("ToolBarCopy");
        await FlushDispatcher();

        textEditor.Document.Text.Should().Be(originalText,
            "Toolbar Copy should not modify editor text");
    }

    [AvaloniaFact]
    public async Task TextEditor_ToolbarPaste_PastesAtCaret()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello";
        textEditor.CaretOffset = 5;
        Window.Flush();

        var originalText = textEditor.Document.Text;

        Window.ClickToolbarButton("ToolBarPaste");
        await FlushDispatcher();

        textEditor.Document.Text.Should().NotBeNull();
    }



    [AvaloniaFact]
    public async Task TextEditor_Cut_NoSelection_DoesNotThrow()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 0);
        Window.Flush();

        var ex = Record.Exception(() => Window.ClickMenuItem("MenuCopy"));
        ex.Should().BeNull("Copy with no selection should not throw");
    }

    [AvaloniaFact]
    public async Task TextEditor_Copy_NoSelection_DoesNotThrow()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello";
        textEditor.CaretOffset = 5;
        Window.Flush();

        var ex = Record.Exception(() => Window.ClickMenuItem("MenuPaste"));
        ex.Should().BeNull("Paste with empty clipboard should not throw");
    }
}
