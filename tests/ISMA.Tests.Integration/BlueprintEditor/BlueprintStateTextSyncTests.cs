using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for state and loop text editor sync.
/// Verifies that text editor events fire correctly and ViewModel properties update.
/// </summary>
public class BlueprintStateTextSyncTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task OpenStateTextEditor_RaisesEvent()
    {
        // Create blueprint project and add a state
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        var userState = editorVm.States.First(s => !s.IsMain && !s.IsInit);

        // Subscribe to the StateTextEditorRequested event
        bool eventFired = false;
        BlueprintStateViewModel? eventState = null;
        editorVm.StateTextEditorRequested += state =>
        {
            eventFired = true;
            eventState = state;
        };

        // Open the text editor for the state
        editorVm.OpenStateTextEditor(userState);

        // Verify the event fired with the correct state
        eventFired.Should().BeTrue();
        eventState.Should().Be(userState);
        eventState!.Name.Should().Be(userState.Name);
    }

    [AvaloniaFact]
    public async Task OpenLoopTextEditor_RaisesEvent()
    {
        // Create blueprint project, add a state, add a loop
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        var userState = editorVm.States.First(s => !s.IsMain && !s.IsInit);

        // Add a loop on the state
        var loopModel = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = userState.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        editorVm.AddLoop(loopModel);
        editorVm.LoopTransactions.Should().HaveCount(1);

        // Subscribe to the LoopTextEditorRequested event
        bool eventFired = false;
        BlueprintLoopTransactionViewModel? eventLoop = null;
        editorVm.LoopTextEditorRequested += loop =>
        {
            eventFired = true;
            eventLoop = loop;
        };

        // Open the text editor for the loop
        editorVm.OpenLoopTextEditor(editorVm.LoopTransactions[0]);

        // Verify the event fired with the correct loop
        eventFired.Should().BeTrue();
        eventLoop.Should().Be(editorVm.LoopTransactions[0]);
    }

    [AvaloniaFact]
    public async Task UpdateStateText_UpdatesViewModel()
    {
        // Create blueprint project and add a state
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        var userState = editorVm.States.First(s => !s.IsMain && !s.IsInit);

        // Verify initial text is empty
        userState.Text.Should().BeNullOrEmpty();

        // Update the state text
        editorVm.UpdateStateText(userState, "This is a test state description");

        // Verify the ViewModel was updated
        userState.Text.Should().Be("This is a test state description");
    }

    [AvaloniaFact]
    public async Task UpdateLoopText_UpdatesViewModel()
    {
        // Create blueprint project, add a state, add a loop
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        editorVm!.AddStateCommand.Execute(null);
        var userState = editorVm.States.First(s => !s.IsMain && !s.IsInit);

        // Add a loop on the state
        var loopModel = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = userState.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        editorVm.AddLoop(loopModel);
        editorVm.LoopTransactions.Should().HaveCount(1);
        var loopVm = editorVm.LoopTransactions[0];

        // Verify initial text is empty
        loopVm.Text.Should().BeNullOrEmpty();

        // Update the loop text
        editorVm.UpdateLoopText(loopVm, "Loop condition: x > 0");

        // Verify the ViewModel was updated
        loopVm.Text.Should().Be("Loop condition: x > 0");
    }
}
