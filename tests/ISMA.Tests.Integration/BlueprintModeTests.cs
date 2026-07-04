using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for the EditorMode sealed class hierarchy.
/// Verifies mode initialization, state carrying, reset behavior, and mutual exclusivity.
/// </summary>
public class BlueprintModeTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task InitialMode_IsDefault()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [AvaloniaFact]
    public async Task AddTransitionMode_CarriesSelectedStates()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add some states
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(4);

        // Create AddTransition mode with selected states
        var selectedStates = new List<BlueprintStateViewModel> { editorVm.States[2] };
        var addMode = new EditorMode.AddTransition(selectedStates);
        editorVm.Mode = addMode;

        editorVm.Mode.Should().BeOfType<EditorMode.AddTransition>();
        var actualMode = (EditorMode.AddTransition)editorVm.Mode;
        actualMode.SelectedStates.Should().ContainSingle();
        actualMode.SelectedStates[0].Should().Be(editorVm.States[2]);
    }

    [AvaloniaFact]
    public async Task ResetMode_ReturnsToDefault()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Set to RemoveState mode
        editorVm!.Mode = new EditorMode.RemoveState();
        editorVm.Mode.Should().BeOfType<EditorMode.RemoveState>();

        // Reset
        editorVm.ResetEditorModeCommand.Execute(null);

        editorVm.Mode.Should().BeOfType<EditorMode.Default>();
    }

    [AvaloniaFact]
    public async Task ModeChange_UpdatesButtonContent()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Verify initial button content
        editorVm!.AddTransitionButtonContent.Should().Be("Add Transition");
        editorVm.RemoveStateButtonContent.Should().Be("Remove State");
        editorVm.RemoveTransitionButtonContent.Should().Be("Remove Transition");

        // Set AddTransition mode
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.AddTransitionButtonContent.Should().Be("Stop adding transaction");
        editorVm.RemoveStateButtonContent.Should().Be("Remove State");
        editorVm.RemoveTransitionButtonContent.Should().Be("Remove Transition");

        // Set RemoveState mode
        editorVm.Mode = new EditorMode.RemoveState();
        editorVm.AddTransitionButtonContent.Should().Be("Add Transition");
        editorVm.RemoveStateButtonContent.Should().Be("Stop remove state");
        editorVm.RemoveTransitionButtonContent.Should().Be("Remove Transition");

        // Set RemoveTransition mode
        editorVm.Mode = new EditorMode.RemoveTransition();
        editorVm.AddTransitionButtonContent.Should().Be("Add Transition");
        editorVm.RemoveStateButtonContent.Should().Be("Remove State");
        editorVm.RemoveTransitionButtonContent.Should().Be("Stop remove transition");
    }

    [AvaloniaFact]
    public async Task Modes_AreMutuallyExclusive()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Set RemoveState mode
        editorVm!.Mode = new EditorMode.RemoveState();
        editorVm.IsRemoveStateMode.Should().BeTrue();
        editorVm.IsAddTransitionMode.Should().BeFalse();
        editorVm.IsRemoveTransitionMode.Should().BeFalse();
        editorVm.IsDefaultMode.Should().BeFalse();

        // Set AddTransition mode
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.IsAddTransitionMode.Should().BeTrue();
        editorVm.IsRemoveStateMode.Should().BeFalse();
        editorVm.IsRemoveTransitionMode.Should().BeFalse();
        editorVm.IsDefaultMode.Should().BeFalse();

        // Set RemoveTransition mode
        editorVm.Mode = new EditorMode.RemoveTransition();
        editorVm.IsRemoveTransitionMode.Should().BeTrue();
        editorVm.IsAddTransitionMode.Should().BeFalse();
        editorVm.IsRemoveStateMode.Should().BeFalse();
        editorVm.IsDefaultMode.Should().BeFalse();

        // Reset to Default
        editorVm.ResetEditorModeCommand.Execute(null);
        editorVm.IsDefaultMode.Should().BeTrue();
        editorVm.IsAddTransitionMode.Should().BeFalse();
        editorVm.IsRemoveStateMode.Should().BeFalse();
        editorVm.IsRemoveTransitionMode.Should().BeFalse();
    }
}
