global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.App.ViewModels;

namespace ISMA.Tests.ViewModels.Settings;

public class SettingsPanelTests
{
    private static SimulationParametersViewModel CreateParameters() => new();

    [Fact]
    public void SimulationParameters_Has_CauchyInitials()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.Should().NotBeNull();
    }

    [Fact]
    public void SimulationParameters_Has_IntegrationMethod()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.Should().NotBeNull();
    }

    [Fact]
    public void SimulationParameters_Has_EventDetection()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.Should().NotBeNull();
    }

    [Fact]
    public void SimulationParameters_Has_ResultSaving()
    {
        var parameters = CreateParameters();
        parameters.ResultSaving.Should().NotBeNull();
    }

    [Fact]
    public void CauchyInitials_Has_Default_StartTime()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime.Should().Be(0.0);
    }

    [Fact]
    public void CauchyInitials_Has_Default_EndTime()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.EndTime.Should().Be(10.0);
    }

    [Fact]
    public void CauchyInitials_Has_Default_InitialStep()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.InitialStep.Should().Be(0.1);
    }

    [Fact]
    public void CauchyInitials_Can_Update_StartTime()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime = 1.0;
        parameters.CauchyInitials.StartTime.Should().Be(1.0);
    }

    [Fact]
    public void CauchyInitials_Can_Update_EndTime()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.EndTime = 100.0;
        parameters.CauchyInitials.EndTime.Should().Be(100.0);
    }

    [Fact]
    public void CauchyInitials_Can_Update_InitialStep()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.InitialStep = 0.001;
        parameters.CauchyInitials.InitialStep.Should().Be(0.001);
    }

    [Fact]
    public void MethodSettings_Has_Default_Accuracy()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.Accuracy.Should().Be(0.1);
    }

    [Fact]
    public void MethodSettings_Can_Update_Selected_Method()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.SelectedMethod = "Rk4";
        parameters.IntegrationMethod.SelectedMethod.Should().Be("Rk4");
    }

    [Fact]
    public void MethodSettings_Can_Update_Accuracy()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.Accuracy = 0.00001;
        parameters.IntegrationMethod.Accuracy.Should().Be(0.00001);
    }

    [Fact]
    public void EventDetection_Has_Default_Is_Step_Limit_In_Use()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.IsStepLimitInUse.Should().BeFalse();
    }

    [Fact]
    public void EventDetection_Has_Default_Gamma()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.Gamma.Should().Be(0.8);
    }

    [Fact]
    public void EventDetection_Has_Default_Low_Border()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.LowBorder.Should().Be(0.001);
    }

    [Fact]
    public void EventDetection_Can_Enable_Step_Limit()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.IsStepLimitInUse = true;
        parameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
    }

    [Fact]
    public void EventDetection_Can_Update_Gamma()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.Gamma = 0.01;
        parameters.EventDetection.Gamma.Should().Be(0.01);
    }

    [Fact]
    public void EventDetection_Can_Update_Low_Border()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.LowBorder = -1.0;
        parameters.EventDetection.LowBorder.Should().Be(-1.0);
    }

    [Fact]
    public void ResultSaving_Has_Default_Saving_Target()
    {
        var parameters = CreateParameters();
        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [Fact]
    public void ResultSaving_Can_Update_Saving_Target()
    {
        var parameters = CreateParameters();
        parameters.ResultSaving.SavingTarget = SaveTarget.File;
        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }

    [Fact]
    public void SimulationParameters_Can_Snapshot()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.EndTime = 50.0;
        parameters.IntegrationMethod.Accuracy = 0.00001;

        var snapshot = parameters.Snapshot();
        snapshot.Should().NotBeNull();
        snapshot.CauchyInitials.EndTime.Should().Be(50.0);
        snapshot.IntegrationMethod.Accuracy.Should().Be(0.00001);
    }

    [Fact]
    public void SimulationParameters_Can_Commit()
    {
        var parameters = CreateParameters();
        var model = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials { StartTime = 1.0, EndTime = 100.0, InitialStep = 0.001 },
            IntegrationMethod = new IntegrationMethodParameters { SelectedMethod = "Rk4", Accuracy = 0.00001 },
            EventDetection = new EventDetectionParameters { Gamma = 0.01 },
            ResultSaving = new ResultSavingParameters { SavingTarget = SaveTarget.File },
        };

        parameters.Commit(model);
        parameters.CauchyInitials.StartTime.Should().Be(1.0);
        parameters.CauchyInitials.EndTime.Should().Be(100.0);
        parameters.IntegrationMethod.SelectedMethod.Should().Be("Rk4");
    }

    [Fact]
    public void TasksPopOver_Has_Empty_InProgress_Initially()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        tasksPopOver.InProgress.Should().BeEmpty();
    }

    [Fact]
    public void TasksPopOver_Has_Empty_Completed_Initially()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        tasksPopOver.Completed.Should().BeEmpty();
    }

    [Fact]
    public void TasksPopOver_Can_Add_InProgress()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        var simulation = new InProgressSimulationViewModel { Id = 1, ModelName = "Test Simulation" };
        tasksPopOver.AddInProgress(simulation);
        tasksPopOver.InProgress.Should().HaveCount(1);
        tasksPopOver.InProgressCount.Should().Be(1);
    }

    [Fact]
    public void TasksPopOver_Can_Remove_InProgress()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        var simulation = new InProgressSimulationViewModel { Id = 1, ModelName = "Test Simulation" };
        tasksPopOver.AddInProgress(simulation);
        tasksPopOver.InProgress.Should().HaveCount(1);

        tasksPopOver.RemoveInProgress(simulation);
        tasksPopOver.InProgress.Should().BeEmpty();
        tasksPopOver.InProgressCount.Should().Be(0);
    }

    [Fact]
    public void TasksPopOver_Can_Clear_InProgress()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        tasksPopOver.AddInProgress(new InProgressSimulationViewModel { Id = 1 });
        tasksPopOver.AddInProgress(new InProgressSimulationViewModel { Id = 2 });
        tasksPopOver.InProgress.Should().HaveCount(2);

        tasksPopOver.ClearInProgress();
        tasksPopOver.InProgress.Should().BeEmpty();
    }

    [Fact]
    public void TasksPopOver_Can_Add_Completed()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        var completed = new CompletedSimulation { Id = 1, ModelName = "Completed Sim" };
        tasksPopOver.AddCompleted(completed);
        tasksPopOver.Completed.Should().HaveCount(1);
        tasksPopOver.CompletedCount.Should().Be(1);
    }

    [Fact]
    public void TasksPopOver_Can_Remove_Completed()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        var completed = new CompletedSimulation { Id = 1, ModelName = "Completed Sim" };
        tasksPopOver.AddCompleted(completed);
        tasksPopOver.Completed.Should().HaveCount(1);

        var vm = tasksPopOver.Completed[0];
        tasksPopOver.RemoveCompleted(vm);
        tasksPopOver.Completed.Should().BeEmpty();
        tasksPopOver.CompletedCount.Should().Be(0);
    }

    [Fact]
    public void TasksPopOver_Can_Clear_Completed()
    {
        var tasksPopOver = new TasksPopOverViewModel();
        tasksPopOver.AddCompleted(new CompletedSimulation { Id = 1 });
        tasksPopOver.AddCompleted(new CompletedSimulation { Id = 2 });
        tasksPopOver.Completed.Should().HaveCount(2);

        tasksPopOver.ClearCompleted();
        tasksPopOver.Completed.Should().BeEmpty();
    }

    [Fact]
    public void InProgressSimulationViewModel_Can_Be_Created()
    {
        var vm = new InProgressSimulationViewModel();
        vm.Should().NotBeNull();
        vm.Id.Should().Be(0);
        vm.ModelName.Should().BeEmpty();
    }

    [Fact]
    public void InProgressSimulationViewModel_Can_Set_Id()
    {
        var vm = new InProgressSimulationViewModel();
        vm.Id = 42;
        vm.Id.Should().Be(42);
    }

    [Fact]
    public void InProgressSimulationViewModel_Can_Set_Model_Name()
    {
        var vm = new InProgressSimulationViewModel();
        vm.ModelName = "Test Simulation";
        vm.ModelName.Should().Be("Test Simulation");
    }

    [Fact]
    public void InProgressSimulationViewModel_Can_Set_Progress()
    {
        var vm = new InProgressSimulationViewModel();
        vm.Progress = 50;
        vm.Progress.Should().Be(50);
    }

    [Fact]
    public void InProgressSimulationViewModel_Can_Set_Can_Abort()
    {
        var vm = new InProgressSimulationViewModel();
        vm.CanAbort = true;
        vm.CanAbort.Should().BeTrue();
    }

    [Fact]
    public void CompletedSimulationViewModel_Can_Be_Created()
    {
        var completed = new CompletedSimulation { Id = 1, ModelName = "Completed Sim" };
        var vm = new CompletedSimulationViewModel(completed);
        vm.Should().NotBeNull();
        vm.Id.Should().Be(1);
        vm.ModelName.Should().Be("Completed Sim");
    }

    [Fact]
    public void CompletedSimulationViewModel_Has_Cached_File()
    {
        var completed = new CompletedSimulation { Id = 1, CachedFile = "/tmp/result.bin" };
        var vm = new CompletedSimulationViewModel(completed);
        vm.CachedFile.Should().Be("/tmp/result.bin");
    }

    [Fact]
    public void CompletedSimulationViewModel_Has_Parameters()
    {
        var completed = new CompletedSimulation { Id = 1 };
        var vm = new CompletedSimulationViewModel(completed);
        vm.Parameters.Should().NotBeNull();
    }
}
