using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for state removal cascade behavior.
/// Verifies that removing a state also removes associated transactions and loops.
/// </summary>
public class BlueprintCascadeTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task RemoveState_RemovesAssociatedTransactions()
    {
        // Create blueprint project and add a state with a transaction
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add a user state
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3); // Main, Init, New state 1

        // Add a transition from the new state to another state
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(4); // Main, Init, New state 1, New state 2

        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify transaction was created
        editorVm.Transactions.Should().HaveCount(1);

        // Remove the source state (New state 1)
        editorVm.Mode = new EditorMode.RemoveState();
        editorVm.SelectedState = editorVm.States[2];
        editorVm.RemoveStateCommand.Execute(null);

        // Verify state and its transaction were removed
        // Removing "New state 1" leaves Main, Init, and "New state 2"
        editorVm.States.Should().HaveCount(3);
        editorVm.Transactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task RemoveState_RemovesAssociatedLoops()
    {
        // Create blueprint project, add a state with a loop
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add a user state
        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        // Add a loop on the state
        var loopModel = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = editorVm.States[2].Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        editorVm.AddLoop(loopModel);
        editorVm.LoopTransactions.Should().HaveCount(1);

        // Remove the state
        editorVm.Mode = new EditorMode.RemoveState();
        editorVm.SelectedState = editorVm.States[2];
        editorVm.RemoveStateCommand.Execute(null);

        // Verify state and its loop were removed
        editorVm.States.Should().HaveCount(2); // Main, Init only
        editorVm.LoopTransactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task RemoveState_WithMultipleTransactions_RemovesAll()
    {
        // Create blueprint project, add a state with multiple outgoing transactions
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        // Add 3 user states
        editorVm!.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(5); // Main, Init, State1, State2, State3

        // Create 2 transactions from State1 to State2 and State3
        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[3];
        editorVm.AddTransitionCommand.Execute(null);

        editorVm.Mode = new EditorMode.AddTransition(new List<BlueprintStateViewModel>());
        editorVm.SetTransitionSource(editorVm.States[2]);
        editorVm.SelectedState = editorVm.States[4];
        editorVm.AddTransitionCommand.Execute(null);

        // Verify both transactions were created
        editorVm.Transactions.Should().HaveCount(2);

        // Remove State1
        editorVm.Mode = new EditorMode.RemoveState();
        editorVm.SelectedState = editorVm.States[2];
        editorVm.RemoveStateCommand.Execute(null);

        // Verify State1 and both its transactions were removed
        // Remaining: Main, Init, State2, State3 = 4 states
        editorVm.States.Should().HaveCount(4);
        editorVm.Transactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task RemoveMainState_IsNoOp()
    {
        // Try to remove Main state — should be prevented
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        int initialCount = editorVm!.States.Count;
        editorVm.States.Should().Contain(s => s.IsMain);

        // Try to remove Main state
        editorVm.Mode = new EditorMode.RemoveState();
        var mainState = editorVm.States.First(s => s.IsMain);
        editorVm.SelectedState = mainState;
        editorVm.RemoveStateCommand.Execute(null);

        // Main state should still exist — the RemoveState command checks IsMain/IsInit
        // Actually looking at the code, RemoveState removes by stateName and the model
        // doesn't protect Main/Init from being removed from the collection, but
        // the ReloadViews always re-adds them. Let's verify the actual behavior.
        var mainStateAfter = editorVm.States.FirstOrDefault(s => s.IsMain);
        mainStateAfter.Should().NotBeNull("Main state should not be removable");
        editorVm.States.Should().HaveCount(initialCount);
    }

    [AvaloniaFact]
    public async Task RemoveInitState_IsNoOp()
    {
        // Try to remove Init state — should be prevented
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;

        int initialCount = editorVm!.States.Count;
        editorVm.States.Should().Contain(s => s.IsInit);

        // Try to remove Init state
        editorVm.Mode = new EditorMode.RemoveState();
        var initState = editorVm.States.First(s => s.IsInit);
        editorVm.SelectedState = initState;
        editorVm.RemoveStateCommand.Execute(null);

        var initStateAfter = editorVm.States.FirstOrDefault(s => s.IsInit);
        initStateAfter.Should().NotBeNull("Init state should not be removable");
        editorVm.States.Should().HaveCount(initialCount);
    }
}
