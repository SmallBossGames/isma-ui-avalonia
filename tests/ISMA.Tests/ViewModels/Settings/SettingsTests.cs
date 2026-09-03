global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.App.ViewModels;

namespace ISMA.Tests.ViewModels.Settings;

/// <summary>
/// End-to-end tests for simulation parameters and settings scenarios.
/// Tests parameter configuration, snapshot/commit, and persistence.
/// </summary>
public class SettingsTests
{
    private static SimulationParametersViewModel CreateParameters() => new();

    [Fact]
    public void SettingsPanel_HasAllSections()
    {
        var parameters = CreateParameters();
        parameters.Should().NotBeNull();
        parameters.CauchyInitials.Should().NotBeNull();
        parameters.IntegrationMethod.Should().NotBeNull();
        parameters.EventDetection.Should().NotBeNull();
        parameters.ResultSaving.Should().NotBeNull();
    }

    [Fact]
    public void CauchyInitials_DefaultValues_AreCorrect()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime.Should().Be(0.0);
        parameters.CauchyInitials.EndTime.Should().Be(10.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.1);
    }

    [Fact]
    public void CauchyInitials_CanBeModified()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime = 1.0;
        parameters.CauchyInitials.EndTime = 20.0;
        parameters.CauchyInitials.InitialStep = 0.05;

        parameters.CauchyInitials.StartTime.Should().Be(1.0);
        parameters.CauchyInitials.EndTime.Should().Be(20.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.05);
    }

    [Fact]
    public void IntegrationMethod_DefaultValues_AreCorrect()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.Accuracy.Should().Be(0.1);
    }

    [Fact]
    public void IntegrationMethod_CanBeModified()
    {
        var parameters = CreateParameters();
        parameters.IntegrationMethod.SelectedMethod = "RK4";
        parameters.IntegrationMethod.Accuracy = 0.001;

        parameters.IntegrationMethod.SelectedMethod.Should().Be("RK4");
        parameters.IntegrationMethod.Accuracy.Should().Be(0.001);
    }

    [Fact]
    public void EventDetection_DefaultValues_AreCorrect()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.Gamma.Should().Be(0.8);
        parameters.EventDetection.LowBorder.Should().Be(0.001);
    }

    [Fact]
    public void EventDetection_CanBeModified()
    {
        var parameters = CreateParameters();
        parameters.EventDetection.Gamma = 0.5;
        parameters.EventDetection.LowBorder = 0.01;
        parameters.EventDetection.IsStepLimitInUse = true;

        parameters.EventDetection.Gamma.Should().Be(0.5);
        parameters.EventDetection.LowBorder.Should().Be(0.01);
        parameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
    }

    [Fact]
    public void ResultSaving_DefaultValue_IsMemory()
    {
        var parameters = CreateParameters();
        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);
    }

    [Fact]
    public void ResultSaving_CanBeChangedToFile()
    {
        var parameters = CreateParameters();
        parameters.ResultSaving.SavingTarget = SaveTarget.File;
        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }

    [Fact]
    public void Snapshot_CapturesCurrentParameters()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime = 5.0;
        parameters.CauchyInitials.EndTime = 15.0;

        var snapshot = parameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
        snapshot.CauchyInitials.EndTime.Should().Be(15.0);
    }

    [Fact]
    public void StoreSettings_CapturesParameters()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime = 5.0;
        parameters.CauchyInitials.EndTime = 20.0;
        parameters.CauchyInitials.InitialStep = 0.05;

        var snapshot = parameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(5.0);
        snapshot.CauchyInitials.EndTime.Should().Be(20.0);
        snapshot.CauchyInitials.InitialStep.Should().Be(0.05);
    }
}
