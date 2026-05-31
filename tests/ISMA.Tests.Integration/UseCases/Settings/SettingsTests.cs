using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration.UseCases.Settings;

/// <summary>
/// UC-06: Store and Load Simulation Parameters
/// UC-13: Configure Simulation Parameters
/// Tests settings panel, parameter configuration, and persistence.
/// </summary>
public class ConfigureSettingsTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC13_SettingsPanel_HasAllSections()
    {
        // Show settings panel
        ViewModel.ShowSettings = true;
        Window.Flush();

        // All 5 settings sections should be present
        ViewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
        ViewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
        ViewModel.SimulationParameters.EventDetection.Should().NotBeNull();
        ViewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
        ViewModel.SimulationParameters.ResultProcessing.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task UC13_CauchyInitials_DefaultValues()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(0.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(10.0);
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.1);
    }

    [AvaloniaFact]
    public async Task UC13_CauchyInitials_CanBeModified()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 5.0;
        ViewModel.SimulationParameters.CauchyInitials.EndTime = 50.0;
        ViewModel.SimulationParameters.CauchyInitials.InitialStep = 0.05;

        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(5.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(50.0);
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.05);
    }

    [AvaloniaFact]
    public async Task UC13_EventDetection_DefaultValues()
    {
        ViewModel.SimulationParameters.EventDetection.IsEventDetectionInUse.Should().BeFalse();
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.8);
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.001);
    }

    [AvaloniaFact]
    public async Task UC13_EventDetection_CanBeEnabled()
    {
        ViewModel.SimulationParameters.EventDetection.IsEventDetectionInUse = true;
        ViewModel.SimulationParameters.EventDetection.Gamma = 0.5;
        ViewModel.SimulationParameters.EventDetection.LowBorder = 0.01;

        ViewModel.SimulationParameters.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        ViewModel.SimulationParameters.EventDetection.Gamma.Should().Be(0.5);
        ViewModel.SimulationParameters.EventDetection.LowBorder.Should().Be(0.01);
    }

    [AvaloniaFact]
    public async Task UC13_ResultProcessing_DefaultValues()
    {
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse.Should().BeFalse();
        ViewModel.SimulationParameters.ResultProcessing.SelectedSimplifyMethod.Should().Be("Radial-Distance");
    }

    [AvaloniaFact]
    public async Task UC13_ResultProcessing_CanBeEnabled()
    {
        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse = true;
        ViewModel.SimulationParameters.ResultProcessing.Tolerance = 0.5;

        ViewModel.SimulationParameters.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        ViewModel.SimulationParameters.ResultProcessing.Tolerance.Should().Be(0.5);
    }

    [AvaloniaFact]
    public async Task UC13_IntegrationMethod_DefaultValues()
    {
        ViewModel.SimulationParameters.IntegrationMethod.Accuracy.Should().Be(0.1);
        ViewModel.SimulationParameters.IntegrationMethod.Server.Should().Be("localhost");
        ViewModel.SimulationParameters.IntegrationMethod.Port.Should().Be(7890);
    }
}

public class StoreLoadParametersTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task UC06_StoreSettings_CapturesCurrentParameters()
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
    public async Task UC06_LoadSettings_DoesNotThrow()
    {
        Action load = () => ViewModel.LoadSettingsCommand.Execute(null);
        load.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task UC06_Snapshot_ReflectsCurrentParameters()
    {
        ViewModel.SimulationParameters.CauchyInitials.StartTime = 10.0;

        var snapshot = ViewModel.SimulationParameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(10.0);
    }

    [AvaloniaFact]
    public async Task UC06_Commit_AppliesParametersToViewModel()
    {
        var model = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials { StartTime = 5.0, EndTime = 50.0, InitialStep = 0.05 }
        };

        ViewModel.SimulationParameters.Commit(model);

        ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(5.0);
        ViewModel.SimulationParameters.CauchyInitials.EndTime.Should().Be(50.0);
        ViewModel.SimulationParameters.CauchyInitials.InitialStep.Should().Be(0.05);
    }
}
