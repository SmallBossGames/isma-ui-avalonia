global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels.Settings;

/// <summary>
/// UC-06: Store and Load Simulation Parameters
/// UC-13: Configure Simulation Parameters
/// Tests settings panel, parameter configuration, and persistence.
/// </summary>
public class ConfigureSettingsTests
{
    private static SimulationParametersViewModel CreateParameters() => new();

    [Fact]
    public void UC13_SettingsPanel_HasAllSections()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.Should().NotBeNull();
        parameters.IntegrationMethod.Should().NotBeNull();
        parameters.EventDetection.Should().NotBeNull();
        parameters.ResultSaving.Should().NotBeNull();
    }

    [Fact]
    public void UC13_CauchyInitials_DefaultValues()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime.Should().Be(0.0);
        parameters.CauchyInitials.EndTime.Should().Be(10.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.1);
    }

    [Fact]
    public void UC13_CauchyInitials_CanBeModified()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime = 5.0;
        parameters.CauchyInitials.EndTime = 50.0;
        parameters.CauchyInitials.InitialStep = 0.05;

        parameters.CauchyInitials.StartTime.Should().Be(5.0);
        parameters.CauchyInitials.EndTime.Should().Be(50.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.05);
    }

    [Fact]
    public void UC13_EventDetection_DefaultValues()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.IsStepLimitInUse.Should().BeFalse();
        parameters.EventDetection.Gamma.Should().Be(0.8);
        parameters.EventDetection.LowBorder.Should().Be(0.001);
    }

    [Fact]
    public void UC13_EventDetection_CanBeEnabled()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.IsStepLimitInUse = true;
        parameters.EventDetection.Gamma = 0.5;
        parameters.EventDetection.LowBorder = 0.01;

        parameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
        parameters.EventDetection.Gamma.Should().Be(0.5);
        parameters.EventDetection.LowBorder.Should().Be(0.01);
    }

    [Fact]
    public void UC13_IntegrationMethod_DefaultValues()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.Accuracy.Should().Be(0.1);
    }
}
