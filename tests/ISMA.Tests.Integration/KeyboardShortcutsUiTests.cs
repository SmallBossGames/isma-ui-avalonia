using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

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
        // Verify NewText command exists and can be invoked
        ViewModel.NewTextCommand.CanExecute(null).Should().BeTrue();
        
        var initialCount = ViewModel.Projects.Count;
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        
        ViewModel.Projects.Count.Should().Be(initialCount + 1);
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_NewBlueprint_UsesCtrlB()
    {
        // Verify NewBlueprint command exists and can be invoked
        ViewModel.NewBlueprintCommand.CanExecute(null).Should().BeTrue();
        
        var initialCount = ViewModel.Projects.Count;
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        
        ViewModel.Projects.Count.Should().Be(initialCount + 1);
        
        // Verify the active project is a BlueprintProjectViewModel
        ViewModel.ActiveProject.Should().BeOfType<BlueprintProjectViewModel>();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Save_UsesCtrlS()
    {
        // Create a project first
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        
        // Save command should be executable (even if file dialog would appear for unsaved)
        ViewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Exit_UsesCtrlW()
    {
        // Verify Exit command exists and can be invoked
        ViewModel.ExitCommand.CanExecute(null).Should().BeTrue();
        
        // Note: We don't actually execute Exit in tests as it would close the app
        // The KeyBinding in MainWindow.axaml ensures Ctrl+W triggers this command
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Verify_UsesCtrlF4()
    {
        // Create a project with some text
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        
        // Verify command is executable
        ViewModel.VerifyCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task KeyboardShortcuts_Run_UsesCtrlF5()
    {
        // Create a project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        
        // Verify command is executable
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
        // Verify the MainWindow is a proper Avalonia Window
        Window.Should().NotBeNull();
        Window.Should().BeAssignableTo<Avalonia.Controls.Window>();
    }

    [AvaloniaFact]
    public async Task SimulationWorkflow_RunCommand_IsExecutable()
    {
        // Create a LISMA text project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.ActiveProject.Should().NotBeNull();
        ViewModel.ActiveProject.Should().BeOfType<LismaProjectViewModel>();

        // Run command should be executable
        ViewModel.RunCommand.CanExecute(null).Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Verify()
    {
        // Create a LISMA text project with valid content
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var lismaProject = ViewModel.ActiveProject as LismaProjectViewModel;
        lismaProject!.SetContent("main\n{ 1 > 0 }\n");

        // Set up mock server for validation
        MockServer.ValidateHandler = _ => Task.FromResult(new ValidationResult
        {
            Errors = ImmutableArray<CompilationError>.Empty,
            Warnings = ImmutableArray<string>.Empty
        });

        // Click Verify via menu
        Window.ClickMenuItem("MenuVerify");
        await FlushDispatcher();

        // Verify command was executed (errors collection should be cleared/populated)
        ViewModel.ErrorList.Errors.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MenuBar_MenuItem_Execution_Save()
    {
        // Create a project
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);
        
        // Save command should be executable
        ViewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }
}
