global using global::Xunit;
using System.Text.Json;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class SimulationParametersTests
{
    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesAllValues()
    {
        var original = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials
            {
                StartTime = 1.5,
                EndTime = 100.0,
                InitialStep = 0.05
            },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = "RK45",
                Accuracy = 0.001,
                IsAccuracyInUse = true,
                IsStableAllowedInUse = true,
                IsStableInUse = false,
                IsParallelInUse = true,
                Server = "remote-server",
                Port = 8080
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = true,
                IsStepLimitInUse = true,
                Gamma = 0.9,
                LowBorder = 0.01
            },
            ResultSaving = new ResultSavingParameters
            {
                SavingTarget = SaveTarget.File
            },
            ResultProcessing = new ResultProcessingParameters
            {
                IsSimplifyInUse = true,
                SelectedSimplifyMethod = "Chamberlain",
                Tolerance = 0.01
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<SimulationParameters>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.CauchyInitials.StartTime.Should().Be(1.5);
        deserialized.CauchyInitials.EndTime.Should().Be(100.0);
        deserialized.CauchyInitials.InitialStep.Should().Be(0.05);
        deserialized.IntegrationMethod.SelectedMethod.Should().Be("RK45");
        deserialized.IntegrationMethod.Accuracy.Should().Be(0.001);
        deserialized.IntegrationMethod.IsAccuracyInUse.Should().BeTrue();
        deserialized.IntegrationMethod.IsStableAllowedInUse.Should().BeTrue();
        deserialized.IntegrationMethod.IsStableInUse.Should().BeFalse();
        deserialized.IntegrationMethod.IsParallelInUse.Should().BeTrue();
        deserialized.IntegrationMethod.Server.Should().Be("remote-server");
        deserialized.IntegrationMethod.Port.Should().Be(8080);
        deserialized.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        deserialized.EventDetection.IsStepLimitInUse.Should().BeTrue();
        deserialized.EventDetection.Gamma.Should().Be(0.9);
        deserialized.EventDetection.LowBorder.Should().Be(0.01);
        deserialized.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
        deserialized.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        deserialized.ResultProcessing.SelectedSimplifyMethod.Should().Be("Chamberlain");
        deserialized.ResultProcessing.Tolerance.Should().Be(0.01);
    }

    [Fact]
    public void DefaultValues_MatchSpec_DefaultsAreCorrect()
    {
        var parameters = new SimulationParameters();

        parameters.CauchyInitials.StartTime.Should().Be(0.0);
        parameters.CauchyInitials.EndTime.Should().Be(10.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.1);

        parameters.IntegrationMethod.SelectedMethod.Should().Be("");
        parameters.IntegrationMethod.Accuracy.Should().Be(0.1);
        parameters.IntegrationMethod.IsAccuracyInUse.Should().BeFalse();
        parameters.IntegrationMethod.IsStableAllowedInUse.Should().BeFalse();
        parameters.IntegrationMethod.IsStableInUse.Should().BeFalse();
        parameters.IntegrationMethod.IsParallelInUse.Should().BeFalse();
        parameters.IntegrationMethod.Server.Should().Be("localhost");
        parameters.IntegrationMethod.Port.Should().Be(7890);

        parameters.EventDetection.IsEventDetectionInUse.Should().BeFalse();
        parameters.EventDetection.IsStepLimitInUse.Should().BeFalse();
        parameters.EventDetection.Gamma.Should().Be(0.8);
        parameters.EventDetection.LowBorder.Should().Be(0.001);

        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.Memory);

        parameters.ResultProcessing.IsSimplifyInUse.Should().BeFalse();
        parameters.ResultProcessing.SelectedSimplifyMethod.Should().Be("Radial-Distance");
        parameters.ResultProcessing.Tolerance.Should().Be(0.001);
    }

    [Fact]
    public void ModifyAfterCreate_VerifiesChanges_AreReflected()
    {
        var parameters = new SimulationParameters();

        parameters.CauchyInitials.StartTime.Should().Be(0.0);
        parameters.CauchyInitials.EndTime.Should().Be(10.0);

        parameters.CauchyInitials.StartTime = 5.0;
        parameters.CauchyInitials.EndTime = 50.0;
        parameters.CauchyInitials.InitialStep = 0.5;

        parameters.CauchyInitials.StartTime.Should().Be(5.0);
        parameters.CauchyInitials.EndTime.Should().Be(50.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.5);

        parameters.IntegrationMethod.SelectedMethod = "Euler";
        parameters.IntegrationMethod.Accuracy = 0.01;
        parameters.IntegrationMethod.Server = "test-server";
        parameters.IntegrationMethod.Port = 9999;

        parameters.IntegrationMethod.SelectedMethod.Should().Be("Euler");
        parameters.IntegrationMethod.Accuracy.Should().Be(0.01);
        parameters.IntegrationMethod.Server.Should().Be("test-server");
        parameters.IntegrationMethod.Port.Should().Be(9999);

        parameters.EventDetection.IsEventDetectionInUse = true;
        parameters.EventDetection.IsStepLimitInUse = true;
        parameters.EventDetection.Gamma = 0.5;
        parameters.EventDetection.LowBorder = 0.1;

        parameters.EventDetection.IsEventDetectionInUse.Should().BeTrue();
        parameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
        parameters.EventDetection.Gamma.Should().Be(0.5);
        parameters.EventDetection.LowBorder.Should().Be(0.1);

        parameters.ResultSaving.SavingTarget = SaveTarget.File;
        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);

        parameters.ResultProcessing.IsSimplifyInUse = true;
        parameters.ResultProcessing.SelectedSimplifyMethod = "Custom";
        parameters.ResultProcessing.Tolerance = 0.1;

        parameters.ResultProcessing.IsSimplifyInUse.Should().BeTrue();
        parameters.ResultProcessing.SelectedSimplifyMethod.Should().Be("Custom");
        parameters.ResultProcessing.Tolerance.Should().Be(0.1);
    }
}
