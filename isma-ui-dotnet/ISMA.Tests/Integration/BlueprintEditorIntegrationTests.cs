using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;
using ISMA.Tests.Integration;
using ISMA.ViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.Tests.Integration;

public class BlueprintEditorIntegrationTests : IntegrationTestBase
{
   [AvaloniaFact]
    public async Task BlueprintEditor_Can_Add_State()
    {
        var vm = new BlueprintEditorViewModel();
        vm.States.Should().BeEmpty();

        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(1);
        vm.States[0].Name.Should().Be("State0");
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Add_Multiple_States()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.AddStateCommand.Execute(null);
        vm.AddStateCommand.Execute(null);

        vm.States.Should().HaveCount(3);
        vm.States[0].Name.Should().Be("State0");
        vm.States[1].Name.Should().Be("State1");
        vm.States[2].Name.Should().Be("State2");
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Has_Empty_Transactions_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.Transactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Has_Empty_LoopTransactions_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.LoopTransactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Has_Default_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.Default);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Has_No_Selected_State_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.SelectedState.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Has_No_Selected_Transaction_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.SelectedTransaction.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Is_Not_In_Add_Transition_Mode_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsAddTransitionMode.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Is_Not_In_Remove_State_Mode_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsRemoveStateMode.Should().BeFalse();
    }

   [AvaloniaFact]
    public async Task BlueprintEditor_Is_Not_In_Remove_Transition_Mode_Initially()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsRemoveTransitionMode.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Select_State()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);

        vm.SelectedState = vm.States[0];
        vm.SelectedState.Should().Be(vm.States[0]);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Deselect_State()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);

        vm.SelectedState = vm.States[0];
        vm.SelectedState.Should().NotBeNull();

        vm.SelectedState = null;
        vm.SelectedState.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Set_Add_Transition_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsAddTransitionMode = true;
        vm.IsAddTransitionMode.Should().BeTrue();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.AddTransition);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Exit_Add_Transition_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsAddTransitionMode = true;
        vm.IsAddTransitionMode.Should().BeTrue();

        vm.IsAddTransitionMode = false;
        vm.IsAddTransitionMode.Should().BeFalse();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.Default);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Set_Remove_State_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsRemoveStateMode = true;
        vm.IsRemoveStateMode.Should().BeTrue();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.RemoveState);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Exit_Remove_State_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsRemoveStateMode = true;
        vm.IsRemoveStateMode.Should().BeTrue();

        vm.IsRemoveStateMode = false;
        vm.IsRemoveStateMode.Should().BeFalse();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.Default);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Set_Remove_Transition_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsRemoveTransitionMode = true;
        vm.IsRemoveTransitionMode.Should().BeTrue();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.RemoveTransition);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Exit_Remove_Transition_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsRemoveTransitionMode = true;
        vm.IsRemoveTransitionMode.Should().BeTrue();

        vm.IsRemoveTransitionMode = false;
        vm.IsRemoveTransitionMode.Should().BeFalse();
        vm.CurrentMode.Should().Be(BlueprintEditorMode.Default);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Reset_Editor_Mode()
    {
        var vm = new BlueprintEditorViewModel();
        vm.IsAddTransitionMode = true;
        vm.IsRemoveStateMode = true;
        vm.IsRemoveTransitionMode = true;

        vm.ResetEditorModeCommand.Execute(null);

        vm.CurrentMode.Should().Be(BlueprintEditorMode.Default);
        vm.IsAddTransitionMode.Should().BeFalse();
        vm.IsRemoveStateMode.Should().BeFalse();
        vm.IsRemoveTransitionMode.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Get_Blueprint_Model()
    {
        var vm = new BlueprintEditorViewModel();
        var model = vm.GetBlueprintModel();
        model.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Set_Blueprint_Model()
    {
        var vm = new BlueprintEditorViewModel();
        var model = new BlueprintModel
        {
            Main = new BlueprintStateModel { Name = "Main", CanvasPositionX = 0, CanvasPositionY = 0 },
            Init = new BlueprintStateModel { Name = "Init", CanvasPositionX = 0, CanvasPositionY = 0 },
            States = ImmutableArray<BlueprintStateModel>.Empty,
            Transactions = ImmutableArray<BlueprintTransactionModel>.Empty,
            LoopTransactions = ImmutableArray<BlueprintLoopTransactionModel>.Empty
        };

        vm.SetBlueprintModel(model);
        vm.States.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Convert_To_Lisma()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States[0].Name = "InitialState";
        vm.States[0].IsInit = true;

        var project = new BlueprintProjectViewModel(
            Services.GetRequiredService<IProjectFileService>(),
            Services.GetRequiredService<ITextEditorFactory>());
        project.SetEditorViewModel(vm);

        var lisma = project.ConvertToLisma();
        lisma.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Can_Dispose()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(1);

        vm.Dispose();
        vm.States.Should().BeEmpty();
        vm.Transactions.Should().BeEmpty();
        vm.LoopTransactions.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintStateViewModel_Can_Be_Created()
    {
        var state = new BlueprintStateViewModel();
        state.Should().NotBeNull();
        state.Name.Should().BeEmpty();
        state.Text.Should().BeEmpty();
        state.CanvasPositionX.Should().Be(0);
        state.CanvasPositionY.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task BlueprintStateViewModel_Can_Set_Name()
    {
        var state = new BlueprintStateViewModel();
        state.Name = "TestState";
        state.Name.Should().Be("TestState");
    }

    [AvaloniaFact]
    public async Task BlueprintStateViewModel_Can_Set_Text()
    {
        var state = new BlueprintStateViewModel();
        state.Text = "State text";
        state.Text.Should().Be("State text");
    }

    [AvaloniaFact]
    public async Task BlueprintStateViewModel_Can_Set_Position()
    {
        var state = new BlueprintStateViewModel();
        state.CanvasPositionX = 100;
        state.CanvasPositionY = 200;
        state.CanvasPositionX.Should().Be(100);
        state.CanvasPositionY.Should().Be(200);
    }

    [AvaloniaFact]
    public async Task BlueprintStateViewModel_Can_Set_Fill_Color()
    {
        var state = new BlueprintStateViewModel();
        state.FillColorHex = "#FF0000";
        state.FillColorHex.Should().Be("#FF0000");
    }

    [AvaloniaFact]
    public async Task BlueprintStateViewModel_Can_Be_Selected()
    {
        var state = new BlueprintStateViewModel();
        state.IsSelected = true;
        state.IsSelected.Should().BeTrue();

        state.IsSelected = false;
        state.IsSelected.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task BlueprintTransactionViewModel_Can_Be_Created()
    {
        var tx = new BlueprintTransactionViewModel();
        tx.Should().NotBeNull();
        tx.Predicate.Should().BeEmpty();
        tx.Alias.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task BlueprintTransactionViewModel_Can_Set_Predicate()
    {
        var tx = new BlueprintTransactionViewModel();
        tx.Predicate = "x > 0";
        tx.Predicate.Should().Be("x > 0");
    }

    [AvaloniaFact]
    public async Task BlueprintTransactionViewModel_Can_Set_Alias()
    {
        var tx = new BlueprintTransactionViewModel();
        tx.Alias = "myAlias";
        tx.Alias.Should().Be("myAlias");
    }

    [AvaloniaFact]
    public async Task BlueprintTransactionViewModel_Can_Set_Start_State()
    {
        var tx = new BlueprintTransactionViewModel();
        var startState = new BlueprintStateViewModel { Name = "State1" };
        tx.StartState = startState;
        tx.StartState.Name.Should().Be("State1");
    }

    [AvaloniaFact]
    public async Task BlueprintTransactionViewModel_Can_Set_End_State()
    {
        var tx = new BlueprintTransactionViewModel();
        var endState = new BlueprintStateViewModel { Name = "State2" };
        tx.EndState = endState;
        tx.EndState.Name.Should().Be("State2");
    }

    [AvaloniaFact]
    public async Task BlueprintTransactionViewModel_Can_Be_Selected()
    {
        var tx = new BlueprintTransactionViewModel();
        tx.IsSelected = true;
        tx.IsSelected.Should().BeTrue();

        tx.IsSelected = false;
        tx.IsSelected.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task BlueprintProjectViewModel_Can_Be_Created()
    {
        var project = new BlueprintProjectViewModel(
            Services.GetRequiredService<IProjectFileService>(),
            Services.GetRequiredService<ITextEditorFactory>());

        project.Should().NotBeNull();
        project.Name.Should().Be("Untitled Blueprint");
    }

    [AvaloniaFact]
    public async Task BlueprintProjectViewModel_Can_Get_Blueprint_Model()
    {
        var project = new BlueprintProjectViewModel(
            Services.GetRequiredService<IProjectFileService>(),
            Services.GetRequiredService<ITextEditorFactory>());

        var model = project.GetBlueprintModel();
        model.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintProjectViewModel_Can_Set_Editor_View_Model()
    {
        var project = new BlueprintProjectViewModel(
            Services.GetRequiredService<IProjectFileService>(),
            Services.GetRequiredService<ITextEditorFactory>());

        var editorVm = new BlueprintEditorViewModel();
        project.SetEditorViewModel(editorVm);
        project.EditorContent.Should().Be(editorVm);
    }

    [AvaloniaFact]
    public async Task BlueprintProjectViewModel_Can_Dispose()
    {
        var project = new BlueprintProjectViewModel(
            Services.GetRequiredService<IProjectFileService>(),
            Services.GetRequiredService<ITextEditorFactory>());

        var editorVm = new BlueprintEditorViewModel();
        project.SetEditorViewModel(editorVm);
        project.Dispose();
    }

    [AvaloniaFact]
    public async Task BlueprintToLismaConverter_Can_Convert_Empty_Blueprint()
    {
        var model = BlueprintModel.Empty;
        var lisma = BlueprintToLismaConverter.ConvertToLisma(model);
        lisma.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintToLismaConverter_Can_Convert_Blueprint_With_States()
    {
        var model = new BlueprintModel
        {
            Main = new BlueprintStateModel { Name = "Main", CanvasPositionX = 0, CanvasPositionY = 0 },
            Init = new BlueprintStateModel { Name = "Init", CanvasPositionX = 0, CanvasPositionY = 0 },
            States = ImmutableArray.Create(
                new BlueprintStateModel { Name = "State1", CanvasPositionX = 100, CanvasPositionY = 100 },
                new BlueprintStateModel { Name = "State2", CanvasPositionX = 200, CanvasPositionY = 200 }),
            Transactions = ImmutableArray.Create(
                new BlueprintTransactionModel { StartStateName = "State1", EndStateName = "State2", Predicate = "x > 0" }),
            LoopTransactions = ImmutableArray<BlueprintLoopTransactionModel>.Empty
        };

        var lisma = BlueprintToLismaConverter.ConvertToLisma(model);
        lisma.Should().NotBeNull();
    }
}
