using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.Domain.Conversion;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.Tests.Integration;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.Tests.Integration;

public class NameUniquenessIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task NameChangingMonitor_Can_Register_Name()
    {
        var monitor = new NameChangingMonitor();
        monitor.TryRegister("Main").Should().BeTrue();
        monitor.TryRegister("Init").Should().BeTrue();
        monitor.TryRegister("State1").Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task NameChangingMonitor_Rejects_Duplicate_Name()
    {
        var monitor = new NameChangingMonitor();
        monitor.TryRegister("Main").Should().BeTrue();
        monitor.TryRegister("Main").Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task NameChangingMonitor_Can_Unregister_Name()
    {
        var monitor = new NameChangingMonitor();
        monitor.TryRegister("Main").Should().BeTrue();
        monitor.TryUnregister("Main").Should().BeTrue();
        monitor.TryRegister("Main").Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task NameChangingMonitor_Creates_Incrementing_Names()
    {
        var monitor = new NameChangingMonitor();
        var name1 = monitor.CreateNextDefaultName();
        var name2 = monitor.CreateNextDefaultName();
        var name3 = monitor.CreateNextDefaultName();

        name1.Should().Be("New state 1");
        name2.Should().Be("New state 2");
        name3.Should().Be("New state 3");
    }

    [AvaloniaFact]
    public async Task NameChangingMonitor_Updates_Counter_From_Existing_Name()
    {
        var monitor = new NameChangingMonitor();
        monitor.TryRegister("New state 5").Should().BeTrue();
        var nextName = monitor.CreateNextDefaultName();
        nextName.Should().Be("New state 6");
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Rejects_Duplicate_State_Name()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(3);

        var newStateName = vm.States[2].Name;
        vm.AddStateWithName(newStateName, 200, 200);
        vm.States.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Allows_Unique_State_Name()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(3);

        vm.AddStateWithName("UniqueState", 200, 200);
        vm.States.Should().HaveCount(4);
        vm.States[3].Name.Should().Be("UniqueState");
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Allows_State_Name_Change_To_Unique_Name()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(3);

        var oldName = vm.States[2].Name;
        vm.UpdateStateName(vm.States[2], "NewName");
        vm.States.Should().HaveCount(3);
        vm.States.First(s => s.Name == "NewName").Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Rejects_State_Name_Change_To_Duplicate()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(4);

        var existingName = vm.States[2].Name;
        vm.UpdateStateName(vm.States[3], existingName);
        vm.States.Should().HaveCount(4);
        vm.States.Count(s => s.Name == existingName).Should().Be(1);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Preserves_Transaction_References_After_Name_Change()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.CurrentMode = BlueprintEditorMode.AddTransition;
        vm.SelectedState = vm.States[2];
        vm.AddTransitionCommand.Execute(null);

        vm.Transactions.Should().HaveCount(1);
        var oldTxStartState = vm.Transactions[0].StartState.Name;

        vm.UpdateStateName(vm.States[2], "RenamedState");
        vm.Transactions.Should().HaveCount(1);
        vm.Transactions[0].StartState.Name.Should().Be("RenamedState");
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Preserves_Loop_Transaction_References_After_Name_Change()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(3);

        vm.RemoveLoopCommand.Execute(null);

        vm.States.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task BlueprintEditor_Name_Monitor_Clears_On_Dispose()
    {
        var vm = new BlueprintEditorViewModel();
        vm.AddStateCommand.Execute(null);
        vm.States.Should().HaveCount(3);

        vm.Dispose();
        vm.States.Should().BeEmpty();
    }
}

public class ModelValidationIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task Verify_Calls_Server_Validation()
    {
        var compilationErrors = new[]
        {
            new CompilationError { Row = 1, Column = 5, Message = "Syntax error" }
        };

        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = compilationErrors.ToImmutableArray()
        });

        var projectService = Services.GetRequiredService<ProjectService>();
        var project = await projectService.CreateNewAsync();
        ViewModel.ActiveProject = project;

        await ViewModel.VerifyCommand.ExecuteAsync(null);

        MockServer.ValidateCalled.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task Verify_Does_Not_Throw_On_Success()
    {
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray<CompilationError>.Empty
        });

        var projectService = Services.GetRequiredService<ProjectService>();
        var project = await projectService.CreateNewAsync();
        ViewModel.ActiveProject = project;

        Action verify = () => ViewModel.VerifyCommand.Execute(null);
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_Does_Not_Throw_With_No_Active_Project()
    {
        ViewModel.ActiveProject = null;

        Action verify = () => ViewModel.VerifyCommand.Execute(null);
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Verify_Does_Not_Affect_Blueprint_Project()
    {
        var projectService = Services.GetRequiredService<ProjectService>();
        var blueprintProject = await projectService.CreateNewBlueprintAsync();
        ViewModel.ActiveProject = blueprintProject;

        Action verify = () => ViewModel.VerifyCommand.Execute(null);
        verify.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task ErrorList_Populates_Correctly()
    {
        var errors = new[]
        {
            new ErrorInfo { Row = 1, Position = 5, FragmentName = "Main", Message = "Error 1" },
            new ErrorInfo { Row = 2, Position = 10, FragmentName = "Main", Message = "Error 2" }
        };

        ViewModel.ErrorList.PutErrorList(errors);

        ViewModel.ErrorList.Errors.Should().HaveCount(2);
        ViewModel.ErrorList.ErrorCount.Should().Be(2);
    }

    [AvaloniaFact]
    public async Task ErrorList_Clears_Correctly()
    {
        ViewModel.ErrorList.PutErrorList(new[]
        {
            new ErrorInfo { Row = 1, Position = 1, FragmentName = "Main", Message = "Error" }
        });

        ViewModel.ErrorList.ClearErrors();
        ViewModel.ErrorList.Errors.Should().BeEmpty();
        ViewModel.ErrorList.ErrorCount.Should().Be(0);
    }
}

public class SimulationEndToEndIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task FullSimulation_Flow_Completes_Successfully()
    {
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "model123" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => AsyncEnumerable.One(new SimulationProgress
        {
            StartTime = 0,
            EndTime = 10,
            CurrentTime = 10
        });
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = "/tmp/result.bin",
            ColumnNames = ImmutableArray.Create("time", "y")
        });

        var projectService = Services.GetRequiredService<ProjectService>();
        var project = await projectService.CreateNewAsync();
        ViewModel.ActiveProject = project;

        await ViewModel.RunCommand.ExecuteAsync(null);

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Simulation complete");
        var resultService = Services.GetRequiredService<ISimulationResultService>();
        resultService.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task Simulation_Fails_On_Compile_Errors()
    {
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult
        {
            Errors = ImmutableArray.Create(new CompilationError { Row = 1, Column = 1, Message = "Error" })
        });

        var projectService = Services.GetRequiredService<ProjectService>();
        var project = await projectService.CreateNewAsync();
        ViewModel.ActiveProject = project;

        await ViewModel.RunCommand.ExecuteAsync(null);

        ViewModel.SimulationService.IsRunning.Should().BeFalse();
        ViewModel.SimulationService.StatusText.Should().Be("Compilation failed");
    }

    [AvaloniaFact]
    public async Task Simulation_Does_Not_Run_When_Already_Running()
    {
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "model123" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        var projectService = Services.GetRequiredService<ProjectService>();
        var project = await projectService.CreateNewAsync();
        ViewModel.ActiveProject = project;

        ViewModel.SimulationService.IsRunning = true;

        await ViewModel.RunCommand.ExecuteAsync(null);

        ViewModel.SimulationService.IsRunning.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task Simulation_Does_Not_Run_With_No_Active_Project()
    {
        ViewModel.ActiveProject = null;

        Action run = () => ViewModel.RunCommand.Execute(null);
        run.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Simulation_Updates_Progress()
    {
        MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "model123" });
        MockServer.RunHandler = _ => Task.FromResult(1L);
        MockServer.MonitorHandler = id => AsyncEnumerable.One(new SimulationProgress
        {
            StartTime = 0,
            EndTime = 10,
            CurrentTime = 5
        });
        MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/result.bin" });

        var projectService = Services.GetRequiredService<ProjectService>();
        var project = await projectService.CreateNewAsync();
        ViewModel.ActiveProject = project;

        await ViewModel.RunCommand.ExecuteAsync(null);

        ViewModel.SimulationService.TrackingTasks.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task Simulation_Parameters_Can_Be_Modified()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 1.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 20.0;

        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(20.0);
    }
}

public class MultiProjectWorkflowIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task Can_Create_Multiple_Text_Projects()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewTextCommand.ExecuteAsync(null);

        ViewModel.Projects.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task Can_Create_Multiple_Blueprint_Projects()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);

        ViewModel.Projects.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public async Task Can_Create_Mixed_Project_Types()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        await ViewModel.NewTextCommand.ExecuteAsync(null);

        ViewModel.Projects.Should().HaveCount(3);
    }

    [AvaloniaFact]
    public async Task Can_Close_Single_Project()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(1);

        await ViewModel.CloseCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task Can_Close_All_Projects()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().HaveCount(2);

        await ViewModel.CloseAllCommand.ExecuteAsync(null);
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task ActiveProject_Updates_On_Project_Selection()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        await ViewModel.NewTextCommand.ExecuteAsync(null);

        ViewModel.Projects.Should().HaveCount(2);
        ViewModel.ActiveProject.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task Project_Name_Changes_Are_Tracked()
    {
        await ViewModel.NewTextCommand.ExecuteAsync(null);
        var projectName = ViewModel.Projects[0].Name;
        projectName.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task Blueprint_Project_Has_Initial_States()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        var model = blueprintProject!.GetBlueprintModel();
        model.Main.Should().NotBeNull();
        model.Init.Should().NotBeNull();
    }
}

public class BlueprintToLismaConversionIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task Blueprint_Converts_To_Lisma_Text()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        var lisma = blueprintProject!.ConvertToLisma();
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task Blueprint_With_Transitions_Converts_Correctly()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        editorVm.Should().NotBeNull();

        editorVm!.AddStateCommand.Execute(null);
        editorVm.States.Should().HaveCount(3);

        editorVm.CurrentMode = BlueprintEditorMode.AddTransition;
        editorVm.SelectedState = editorVm.States[2];
        editorVm.AddTransitionCommand.Execute(null);

        var lisma = blueprintProject.ConvertToLisma();
        lisma.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task Empty_Blueprint_Converts_To_Minimal_Lisma()
    {
        var model = BlueprintModel.Empty;
        var lisma = BlueprintToLismaConverter.ConvertToLisma(model);
        lisma.Should().NotBeNull();
        lisma.FullText.Should().NotBeNullOrEmpty();
    }

    [AvaloniaFact]
    public async Task Blueprint_Model_Persists_State_Changes()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);
        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        var editorVm = blueprintProject!.EditorContent as BlueprintEditorViewModel;
        editorVm!.AddStateCommand.Execute(null);

        editorVm.GetBlueprintModel().States.Should().HaveCount(1);
    }
}

public class ClipboardPropagationIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task Cut_Command_Does_Not_Throw_With_No_Active_Project()
    {
        ViewModel.ActiveProject = null;

        Action cut = () => ViewModel.CutCommand.Execute(null);
        cut.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Copy_Command_Does_Not_Throw_With_No_Active_Project()
    {
        ViewModel.ActiveProject = null;

        Action copy = () => ViewModel.CopyCommand.Execute(null);
        copy.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Paste_Command_Does_Not_Throw_With_No_Active_Project()
    {
        ViewModel.ActiveProject = null;

        Action paste = () => ViewModel.PasteCommand.Execute(null);
        paste.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Cut_Command_Does_Not_Throw_With_Blueprint_Project()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);

        Action cut = () => ViewModel.CutCommand.Execute(null);
        cut.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Copy_Command_Does_Not_Throw_With_Blueprint_Project()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);

        Action copy = () => ViewModel.CopyCommand.Execute(null);
        copy.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task Paste_Command_Does_Not_Throw_With_Blueprint_Project()
    {
        await ViewModel.NewBlueprintCommand.ExecuteAsync(null);

        Action paste = () => ViewModel.PasteCommand.Execute(null);
        paste.Should().NotThrow();
    }
}

public class LoadSettingsIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task StoreSettings_Captures_Current_Parameters()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 5.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 20.0;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.05;

        ViewModel.StoreSettingsCommand.Execute(null);

        var snapshot = ViewModel.SimulationParameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
        snapshot.CauchyInitials.EndTime.Should().Be(20.0);
        snapshot.CauchyInitials.InitialStep.Should().Be(0.05);
    }

    [AvaloniaFact]
    public async Task LoadSettings_Does_Not_Throw()
    {
        Action load = () => ViewModel.LoadSettingsCommand.Execute(null);
        load.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Can_Be_Modified()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 1.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 100.0;
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.001;
        ViewModel.SimulationParameters.EventDetection.Gamma = 0.5;
        ViewModel.SimulationParameters.EventDetection.LowBorder = 0.01;

        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(100.0);
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.001);
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.5);
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.01);
    }

    [AvaloniaFact]
    public async Task Snapshot_Reflects_Current_Parameters()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 10.0;

        var snapshot = ViewModel.SimulationParameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(10.0);
    }

    [AvaloniaFact]
    public async Task ResultSaving_Defaults_To_Memory()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [AvaloniaFact]
    public async Task ResultSaving_Can_Be_Changed_To_File()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget = SaveTarget.File;
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }
}
