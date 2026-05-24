using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration;

/// <summary>
/// End-to-end tests for simulation parameters and settings scenarios.
/// Tests parameter configuration, snapshot/commit, and persistence.
/// </summary>
public class SettingsTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task SettingsPanel_HasAllSections()
    {
        // Verify simulation parameters have all required sections
        ViewModel.SimulationParameters.Should().NotBeNull();
        ViewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
        ViewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
        ViewModel.SimulationParameters.EventDetection.Should().NotBeNull();
        ViewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
        ViewModel.SimulationParameters.ResultProcessing.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task CauchyInitials_DefaultValues_AreCorrect()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(0.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(10.0);
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.1);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_CanBeModified()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 1.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 20.0;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.05;

        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(1.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(20.0);
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.05);
    }

    [AvaloniaFact]
    public async Task IntegrationMethod_DefaultValues_AreCorrect()
    {
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.1);
        ViewModel.SimulationParameters.IntegrationMethod.Server.Should().Be("localhost");
        ViewModel.SimulationParameters.IntegrationMethod.Port.Should().Be(7890);
    }

    [AvaloniaFact]
    public async Task IntegrationMethod_CanBeModified()
    {
        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod = "RK4";
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy = 0.001;
        ViewModel.SimulationParameters.IntegrationMethod.Server = "127.0.0.1";
        ViewModel.SimulationParameters.IntegrationMethod.Port = 8000;

        ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod.Should().Be("RK4");
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.001);
        ViewModel.SimulationParameters.IntegrationMethod.Server.Should().Be("127.0.0.1");
        ViewModel.SimulationParameters.IntegrationMethod.Port.Should().Be(8000);
    }

    [AvaloniaFact]
    public async Task EventDetection_DefaultValues_AreCorrect()
    {
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.8);
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.001);
    }

    [AvaloniaFact]
    public async Task EventDetection_CanBeModified()
    {
        ViewModel.SimulationParameters.EventDetection.Gamma = 0.5;
        ViewModel.SimulationParameters.EventDetection.LowBorder = 0.01;
        ViewModel.SimulationParameters.EventDetection.IsEventDetectionInUse = true;
        ViewModel.SimulationParameters.EventDetection.IsStepLimitInUse = true;

        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.5);
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.01);
        ViewModel.SimulationParameters.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        ViewModel.SimulationParameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task ResultSaving_DefaultValue_IsMemory()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [AvaloniaFact]
    public async Task ResultSaving_CanBeChangedToFile()
    {
        ViewModel.SimulationParameters.ResultSaving.SavingTarget = SaveTarget.File;
        ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }

    [AvaloniaFact]
    public async Task ResultProcessing_DefaultValues_AreCorrect()
    {
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse.Should().BeFalse();
        ViewModel.SimulationParameters.ResultProcessing.SelectedSimplifyMethod.Should().Be("Radial-Distance");
    }

    [AvaloniaFact]
    public async Task ResultProcessing_CanBeModified()
    {
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse = true;
        ViewModel.SimulationParameters.ResultProcessing.SelectedSimplifyMethod = "Douglas-Peucker";
        ViewModel.SimulationParameters.ResultProcessing.Tolerance = 0.01;

        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        ViewModel.SimulationParameters.ResultProcessing.SelectedSimplifyMethod.Should().Be("Douglas-Peucker");
        ViewModel.SimulationParameters.ResultProcessing.Tolerance.Should().Be(0.01);
    }

    [AvaloniaFact]
    public async Task Snapshot_CapturesCurrentParameters()
    {
        // Modify parameters
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 5.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 15.0;

        // Take snapshot
        var snapshot = ViewModel.SimulationParameters.Snapshot();

        // Verify snapshot reflects current values
        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
        snapshot.CauchyInitials.EndTime.Should().Be(15.0);
    }

    [AvaloniaFact]
    public async Task StoreSettings_CapturesParameters()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 5.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 20.0;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.05;

        ViewModel.StoreSettingsCommand.Execute(null);

        // Verify parameters are captured
        var snapshot = ViewModel.SimulationParameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
        snapshot.CauchyInitials.EndTime.Should().Be(20.0);
        snapshot.CauchyInitials.InitialStep.Should().Be(0.05);
    }

    [AvaloniaFact]
    public async Task LoadSettings_DoesNotThrow()
    {
        Action load = () => ViewModel.LoadSettingsCommand.Execute(null);
        load.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task ShowSettings_TogglesCorrectly()
    {
        ViewModel.ShowSettings.Should().BeFalse();

        ViewModel.ShowSettings = true;
        ViewModel.ShowSettings.Should().BeTrue();

        ViewModel.ShowSettings = false;
        ViewModel.ShowSettings.Should().BeFalse();
    }
}
