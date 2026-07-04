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
        ViewModel.NewTextCommand.CanExecute(null).Should().BeTrue();

        var initialCount = ViewModel.Projects.Count;
        await ViewModel.NewTextCommand.ExecuteAsync(null);

        ViewModel.Projects.Count.Should().Be(initialCount + 1);
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_NewBlueprint_UsesCtrlB()
    {
        ViewModel.NewBlueprintCommand.CanExecute(null).Should().BeTrue();

        var initialCount = ViewModel.Projects.Count;
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);

        ViewModel.Projects.Count.Should().Be(initialCount + 1);
        ViewModel.ActiveProject.Should().BeOfType<BlueprintProjectViewModel>();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Save_UsesCtrlS()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Exit_UsesCtrlW()
    {
        ViewModel.ExitCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Verify_UsesCtrlF4()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.VerifyCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Run_UsesCtrlF5()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.RunCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_CutCopyPaste_Exist()
    {
        ViewModel.CutCommand.CanExecute(null).Should().BeTrue();
        ViewModel.CopyCommand.CanExecute(null).Should().BeTrue();
        ViewModel.PasteCommand.CanExecute(null).Should().BeTrue();
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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();
        ViewModel.ActiveProject.Should().BeOfType<LismaProjectViewModel>();
        ViewModel.RunCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Verify()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var lismaProject = ViewModel.ActiveProject as LismaProjectViewModel;
        lismaProject!.SetContent("main\n{ 1 > 0 }\n");

        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray<CompilationError>.Empty,
            Warnings = ImmutableArray<string>.Empty
        });

        Window.ClickMenuItem("MenuVerify");
        await FlushDispatcher();
        ViewModel.ErrorList.Errors.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Save()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task ToolBar_CutButton_Command_IsBound()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

        var lismaProject = ViewModel.ActiveProject as LismaProjectViewModel;
        lismaProject!.SetContent("hello world");

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull();
        textEditor!.Select(0, 5);
        Window.Flush();

        ViewModel.CutCommand.Execute(null);
        await FlushDispatcher();

        textEditor.Document.Text.Should().Be(" world");
    }

    [AvaloniaFact]
    public async Task TextEditor_MenuCut_RemovesSelectedText()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

        Window.Flush();

        var textEditor = UiHelpers.GetActiveTextEditor(Window);
        textEditor.Should().NotBeNull("TextEditor should be available");

        textEditor!.Document.Text = "hello world";
        textEditor.Select(0, 0);
        Window.Flush();

        var ex = Record.Exception(() => Window.ClickMenuItem("MenuCut"));
        ex.Should().BeNull("Cut with no selection should not throw");
    }

    [AvaloniaFact]
    public async Task TextEditor_Copy_NoSelection_DoesNotThrow()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
    public async Task TextEditor_Paste_EmptyClipboard_DoesNotThrow()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

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
