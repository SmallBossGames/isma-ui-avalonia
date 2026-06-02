using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.Tests.Integration;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

public class SettingsPanelIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task SimulationParameters_Has_CauchyInitials()
    {
        ViewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Has_IntegrationMethod()
    {
        ViewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Has_EventDetection()
    {
        ViewModel.SimulationParameters.EventDetection.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Has_ResultSaving()
    {
        ViewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Has_ResultProcessing()
    {
        ViewModel.SimulationParameters.ResultProcessing.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task CauchyInitials_Has_Default_StartTime()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(0.0);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_Has_Default_EndTime()
    {
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(10.0);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_Has_Default_InitialStep()
    {
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.1);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_Can_Update_StartTime()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 1.0;
        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_Can_Update_EndTime()
    {
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 100.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(100.0);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_Can_Update_InitialStep()
    {
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.001;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.001);
    }

    [AvaloniaFact]
    public async Task MethodSettings_Has_Default_Selected_Method()
    {
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task MethodSettings_Has_Default_Accuracy()
    {
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.1);
    }

    [AvaloniaFact]
    public async Task MethodSettings_Can_Update_Selected_Method()
    {
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod = "Rk4";
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod.Should().Be("Rk4");
    }

    [AvaloniaFact]
    public async Task MethodSettings_Can_Update_Accuracy()
    {
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.00001;
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.00001);
    }

    

    [AvaloniaFact]
    public async Task EventDetection_Has_Default_Is_Step_Limit_In_Use()
    {
        ViewModel.SimulationParameters.EventDetection.IsStepLimitInUse.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task EventDetection_Has_Default_Gamma()
    {
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.8);
    }

    [AvaloniaFact]
    public async Task EventDetection_Has_Default_Low_Border()
    {
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.001);
    }

    [AvaloniaFact]
    public async Task EventDetection_Can_Enable_Step_Limit()
    {
        ViewModel.SimulationParameters.EventDetection.IsStepLimitInUse = true;
        ViewModel.SimulationParameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task EventDetection_Can_Update_Gamma()
    {
        ViewModel.SimulationParameters.EventDetection.Gamma = 0.01;
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.01);
    }

    [AvaloniaFact]
    public async Task EventDetection_Can_Update_Low_Border()
    {
        ViewModel.SimulationParameters.EventDetection.LowBorder = -1.0;
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(-1.0);
    }

    [AvaloniaFact]
    public async Task ResultSaving_Has_Default_Saving_Target()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [AvaloniaFact]
    public async Task ResultSaving_Can_Update_Saving_Target()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget = SaveTarget.File;
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }

    [AvaloniaFact]
    public async Task ResultProcessing_Has_Default_Is_Simplify_In_Use()
    {
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task ResultProcessing_Has_Default_Tolerance()
    {
        ViewModel.SimulationParameters.ResultProcessing.Tolerance.Should().Be(0.001);
    }

    [AvaloniaFact]
    public async Task ResultProcessing_Can_Enable_Simplify()
    {
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse = true;
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task ResultProcessing_Can_Update_Tolerance()
    {
        ViewModel.SimulationParameters.ResultProcessing.Tolerance = 0.00001;
        ViewModel.SimulationParameters.ResultProcessing.Tolerance.Should().Be(0.00001);
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Can_Snapshot()
    {
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 50.0;
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.00001;

        var snapshot = ViewModel.SimulationParameters.Snapshot();
        snapshot.Should().NotBeNull();
        snapshot.CauchyInitials.EndTime.Should().Be(50.0);
        snapshot.IntegrationMethod.Accuracy.Should().Be(0.00001);
    }

    [AvaloniaFact]
    public async Task SimulationParameters_Can_Commit()
    {
        var parameters = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials { StartTime = 1.0, EndTime = 100.0, InitialStep = 0.001 },
            IntegrationMethod = new IntegrationMethodParameters { SelectedMethod = "Rk4", Accuracy = 0.00001 },
            EventDetection = new EventDetectionParameters { IsStepLimitInUse = true, Gamma = 0.01 },
            ResultSaving = new ResultSavingParameters { SavingTarget = SaveTarget.File },
            ResultProcessing = new ResultProcessingParameters { IsSimplifyInUse = true, Tolerance = 0.00001 }
        };

        ViewModel.SimulationParameters.Commit(parameters);
        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(100.0);
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod.Should().Be("Rk4");
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Has_Empty_InProgress_Initially()
    {
        ViewModel.TasksPopOver.InProgress.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Has_Empty_Completed_Initially()
    {
        ViewModel.TasksPopOver.Completed.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Can_Add_InProgress()
    {
        var simulation = new InProgressSimulationViewModel { Id = 1, ModelName = "Test Simulation" };
        ViewModel.TasksPopOver.AddInProgress(simulation);
        ViewModel.TasksPopOver.InProgress.Should().HaveCount(1);
        ViewModel.TasksPopOver.InProgressCount.Should().Be(1);
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Can_Remove_InProgress()
    {
        var simulation = new InProgressSimulationViewModel { Id = 1, ModelName = "Test Simulation" };
        ViewModel.TasksPopOver.AddInProgress(simulation);
        ViewModel.TasksPopOver.InProgress.Should().HaveCount(1);

        ViewModel.TasksPopOver.RemoveInProgress(simulation);
        ViewModel.TasksPopOver.InProgress.Should().BeEmpty();
        ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Can_Clear_InProgress()
    {
        ViewModel.TasksPopOver.AddInProgress(new InProgressSimulationViewModel { Id = 1 });
        ViewModel.TasksPopOver.AddInProgress(new InProgressSimulationViewModel { Id = 2 });
        ViewModel.TasksPopOver.InProgress.Should().HaveCount(2);

        ViewModel.TasksPopOver.ClearInProgress();
        ViewModel.TasksPopOver.InProgress.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Can_Add_Completed()
    {
        var completed = new CompletedSimulation { Id = 1, ModelName = "Completed Sim" };
        ViewModel.TasksPopOver.AddCompleted(completed);
        ViewModel.TasksPopOver.Completed.Should().HaveCount(1);
        ViewModel.TasksPopOver.CompletedCount.Should().Be(1);
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Can_Remove_Completed()
    {
        var completed = new CompletedSimulation { Id = 1, ModelName = "Completed Sim" };
        ViewModel.TasksPopOver.AddCompleted(completed);
        ViewModel.TasksPopOver.Completed.Should().HaveCount(1);

        var vm = ViewModel.TasksPopOver.Completed[0];
        ViewModel.TasksPopOver.RemoveCompleted(vm);
        ViewModel.TasksPopOver.Completed.Should().BeEmpty();
        ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task TasksPopOver_Can_Clear_Completed()
    {
        ViewModel.TasksPopOver.AddCompleted(new CompletedSimulation { Id = 1 });
        ViewModel.TasksPopOver.AddCompleted(new CompletedSimulation { Id = 2 });
        ViewModel.TasksPopOver.Completed.Should().HaveCount(2);

        ViewModel.TasksPopOver.ClearCompleted();
        ViewModel.TasksPopOver.Completed.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task InProgressSimulationViewModel_Can_Be_Created()
    {
        var vm = new InProgressSimulationViewModel();
        vm.Should().NotBeNull();
        vm.Id.Should().Be(0);
        vm.ModelName.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task InProgressSimulationViewModel_Can_Set_Id()
    {
        var vm = new InProgressSimulationViewModel();
        vm.Id = 42;
        vm.Id.Should().Be(42);
    }

    [AvaloniaFact]
    public async Task InProgressSimulationViewModel_Can_Set_Model_Name()
    {
        var vm = new InProgressSimulationViewModel();
        vm.ModelName = "Test Simulation";
        vm.ModelName.Should().Be("Test Simulation");
    }

    [AvaloniaFact]
    public async Task InProgressSimulationViewModel_Can_Set_Progress()
    {
        var vm = new InProgressSimulationViewModel();
        vm.Progress = 50;
        vm.Progress.Should().Be(50);
    }

    [AvaloniaFact]
    public async Task InProgressSimulationViewModel_Can_Set_Can_Abort()
    {
        var vm = new InProgressSimulationViewModel();
        vm.CanAbort = true;
        vm.CanAbort.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task CompletedSimulationViewModel_Can_Be_Created()
    {
        var completed = new CompletedSimulation { Id = 1, ModelName = "Completed Sim" };
        var vm = new CompletedSimulationViewModel(completed);
        vm.Should().NotBeNull();
        vm.Id.Should().Be(1);
        vm.ModelName.Should().Be("Completed Sim");
    }

    [AvaloniaFact]
    public async Task CompletedSimulationViewModel_Has_Cached_File()
    {
        var completed = new CompletedSimulation { Id = 1, CachedFile = "/tmp/result.bin" };
        var vm = new CompletedSimulationViewModel(completed);
        vm.CachedFile.Should().Be("/tmp/result.bin");
    }

    [AvaloniaFact]
    public async Task CompletedSimulationViewModel_Has_Parameters()
    {
        var completed = new CompletedSimulation { Id = 1 };
        var vm = new CompletedSimulationViewModel(completed);
        vm.Parameters.Should().NotBeNull();
    }
}
