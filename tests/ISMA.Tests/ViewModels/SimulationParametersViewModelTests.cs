global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels;

public class SimulationParametersViewModelTests
{
    [Fact]
    public void Snapshot_CapturesCurrentValues()
    {
        var viewModel = new SimulationParametersViewModel();

        viewModel.CauchyInitials.StartTime = 5.0;
        viewModel.CauchyInitials.EndTime = 50.0;
        viewModel.CauchyInitials.InitialStep = 0.5;
        viewModel.IntegrationMethod.SelectedMethod = "RK45";
        viewModel.IntegrationMethod.Accuracy = 0.001;
        viewModel.IntegrationMethod.IsAccuracyInUse = true;
        viewModel.IntegrationMethod.IsStableAllowedInUse = true;
        viewModel.IntegrationMethod.IsStableInUse = true;
        viewModel.IntegrationMethod.IsParallelInUse = true;
        viewModel.IntegrationMethod.Server = "myserver";
        viewModel.IntegrationMethod.Port = 9999;
        viewModel.EventDetection.IsEventDetectionInUse = true;
        viewModel.EventDetection.IsStepLimitInUse = true;
        viewModel.EventDetection.Gamma = 0.9;
        viewModel.EventDetection.LowBorder = 0.05;
        viewModel.ResultSaving.SavingTarget = SaveTarget.File;
        viewModel.ResultProcessing.IsSimplifyInUse = true;
        viewModel.ResultProcessing.SelectedSimplifyMethod = "Custom";
        viewModel.ResultProcessing.Tolerance = 0.02;

        var snapshot = viewModel.Snapshot();

        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
        snapshot.CauchyInitials.EndTime.Should().Be(50.0);
        snapshot.CauchyInitials.InitialStep.Should().Be(0.5);
        snapshot.IntegrationMethod.SelectedMethod.Should().Be("RK45");
        snapshot.IntegrationMethod.Accuracy.Should().Be(0.001);
        snapshot.IntegrationMethod.IsAccuracyInUse.Should().BeTrue();
        snapshot.IntegrationMethod.IsStableAllowedInUse.Should().BeTrue();
        snapshot.IntegrationMethod.IsStableInUse.Should().BeTrue();
        snapshot.IntegrationMethod.IsParallelInUse.Should().BeTrue();
        snapshot.IntegrationMethod.Server.Should().Be("myserver");
        snapshot.IntegrationMethod.Port.Should().Be(9999);
        snapshot.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        snapshot.EventDetection.IsStepLimitInUse.Should().BeTrue();
        snapshot.EventDetection.Gamma.Should().Be(0.9);
        snapshot.EventDetection.LowBorder.Should().Be(0.05);
        snapshot.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
        snapshot.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        snapshot.ResultProcessing.SelectedSimplifyMethod.Should().Be("Custom");
        snapshot.ResultProcessing.Tolerance.Should().Be(0.02);
    }

    [Fact]
    public void Snapshot_DefaultValues_CapturesDefaults()
    {
        var viewModel = new SimulationParametersViewModel();

        var snapshot = viewModel.Snapshot();

        snapshot.CauchyInitials.StartTime.Should().Be(0.0);
        snapshot.CauchyInitials.EndTime.Should().Be(10.0);
        snapshot.CauchyInitials.InitialStep.Should().Be(0.1);
        snapshot.IntegrationMethod.SelectedMethod.Should().Be("");
        snapshot.IntegrationMethod.Accuracy.Should().Be(0.1);
        snapshot.IntegrationMethod.IsAccuracyInUse.Should().BeFalse();
        snapshot.IntegrationMethod.Server.Should().Be("localhost");
        snapshot.IntegrationMethod.Port.Should().Be(7890);
        snapshot.EventDetection.IsEventDetectionInUse.Should().BeFalse();
        snapshot.EventDetection.Gamma.Should().Be(0.8);
        snapshot.EventDetection.LowBorder.Should().Be(0.001);
        snapshot.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
        snapshot.ResultProcessing.IsSimplifyInUse.Should().BeFalse();
        snapshot.ResultProcessing.SelectedSimplifyMethod.Should().Be("Radial-Distance");
        snapshot.ResultProcessing.Tolerance.Should().Be(0.001);
    }

    [Fact]
    public void Commit_AppliesValuesToViewModel()
    {
        var viewModel = new SimulationParametersViewModel();

        var parameters = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials
            {
                StartTime = 3.0,
                EndTime = 30.0,
                InitialStep = 0.3
            },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = "Euler",
                Accuracy = 0.05,
                IsAccuracyInUse = true,
                IsStableAllowedInUse = true,
                IsStableInUse = true,
                IsParallelInUse = true,
                Server = "commit-server",
                Port = 5555
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = true,
                IsStepLimitInUse = true,
                Gamma = 0.7,
                LowBorder = 0.02
            },
            ResultSaving = new ResultSavingParameters
            {
                SavingTarget = SaveTarget.File
            },
            ResultProcessing = new ResultProcessingParameters
            {
                IsSimplifyInUse = true,
                SelectedSimplifyMethod = "SimplifyMethod",
                Tolerance = 0.05
            }
        };

        viewModel.Commit(parameters);

        viewModel.CauchyInitials.StartTime.Should().Be(3.0);
        viewModel.CauchyInitials.EndTime.Should().Be(30.0);
        viewModel.CauchyInitials.InitialStep.Should().Be(0.3);
        viewModel.IntegrationMethod.SelectedMethod.Should().Be("Euler");
        viewModel.IntegrationMethod.Accuracy.Should().Be(0.05);
        viewModel.IntegrationMethod.IsAccuracyInUse.Should().BeTrue();
        viewModel.IntegrationMethod.IsStableAllowedInUse.Should().BeTrue();
        viewModel.IntegrationMethod.IsStableInUse.Should().BeTrue();
        viewModel.IntegrationMethod.IsParallelInUse.Should().BeTrue();
        viewModel.IntegrationMethod.Server.Should().Be("commit-server");
        viewModel.IntegrationMethod.Port.Should().Be(5555);
        viewModel.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        viewModel.EventDetection.IsStepLimitInUse.Should().BeTrue();
        viewModel.EventDetection.Gamma.Should().Be(0.7);
        viewModel.EventDetection.LowBorder.Should().Be(0.02);
        viewModel.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
        viewModel.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        viewModel.ResultProcessing.SelectedSimplifyMethod.Should().Be("SimplifyMethod");
        viewModel.ResultProcessing.Tolerance.Should().Be(0.05);
    }

    [Fact]
    public void Constructor_FromParameters_PopulatesAllProperties()
    {
        var parameters = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials
            {
                StartTime = 1.0,
                EndTime = 20.0,
                InitialStep = 0.2
            },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = "RK2",
                Accuracy = 0.01,
                IsAccuracyInUse = true,
                IsStableAllowedInUse = false,
                IsStableInUse = true,
                IsParallelInUse = false,
                Server = "constructor-server",
                Port = 4444
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = true,
                IsStepLimitInUse = false,
                Gamma = 0.6,
                LowBorder = 0.03
            },
            ResultSaving = new ResultSavingParameters
            {
                SavingTarget = SaveTarget.File
            },
            ResultProcessing = new ResultProcessingParameters
            {
                IsSimplifyInUse = true,
                SelectedSimplifyMethod = "ConstructorMethod",
                Tolerance = 0.03
            }
        };

        var viewModel = new SimulationParametersViewModel(parameters);

        viewModel.CauchyInitials.StartTime.Should().Be(1.0);
        viewModel.CauchyInitials.EndTime.Should().Be(20.0);
        viewModel.CauchyInitials.InitialStep.Should().Be(0.2);
        viewModel.IntegrationMethod.SelectedMethod.Should().Be("RK2");
        viewModel.IntegrationMethod.Accuracy.Should().Be(0.01);
        viewModel.IntegrationMethod.IsAccuracyInUse.Should().BeTrue();
        viewModel.IntegrationMethod.IsStableAllowedInUse.Should().BeFalse();
        viewModel.IntegrationMethod.IsStableInUse.Should().BeTrue();
        viewModel.IntegrationMethod.IsParallelInUse.Should().BeFalse();
        viewModel.IntegrationMethod.Server.Should().Be("constructor-server");
        viewModel.IntegrationMethod.Port.Should().Be(4444);
        viewModel.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        viewModel.EventDetection.IsStepLimitInUse.Should().BeFalse();
        viewModel.EventDetection.Gamma.Should().Be(0.6);
        viewModel.EventDetection.LowBorder.Should().Be(0.03);
        viewModel.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
        viewModel.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        viewModel.ResultProcessing.SelectedSimplifyMethod.Should().Be("ConstructorMethod");
        viewModel.ResultProcessing.Tolerance.Should().Be(0.03);
    }

    [Fact]
    public void SnapshotAndCommit_Consistency_CheckSnapshotThenCommit()
    {
        var viewModel = new SimulationParametersViewModel();

        viewModel.CauchyInitials.StartTime = 7.0;
        viewModel.CauchyInitials.EndTime = 70.0;

        var snapshot = viewModel.Snapshot();

        var newViewModel = new SimulationParametersViewModel();
        newViewModel.Commit(snapshot);

        newViewModel.CauchyInitials.StartTime.Should().Be(7.0);
        newViewModel.CauchyInitials.EndTime.Should().Be(70.0);
    }
}
