global using global::Xunit;
using System.Text.Json;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

/// <summary>
/// Verifies the simulation parameters file schema matches the original Kotlin
/// <c>SimulationParametersModel</c> kotlinx.serialization output.
/// </summary>
public class SimulationParametersFileModelTests
{
    private static JsonSerializerOptions Options => SimulationParametersFileModel.JsonOptions;

    [Fact]
    public void Serialize_UsesOriginalPropertyNames()
    {
        var parameters = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials { StartTime = 0.0, EndTime = 10.0, InitialStep = 0.1 },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = "RK45",
                Accuracy = 0.1,
                IsAccuracyInUse = true,
                IsStableAllowedInUse = true,
                IsStableInUse = false,
                IsParallelInUse = false,
                Server = "localhost",
                Port = 7890
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = true,
                IsStepLimitInUse = false,
                Gamma = 0.8,
                LowBorder = 0.001
            },
            ResultSaving = new ResultSavingParameters { SavingTarget = SaveTarget.Memory }
        };

        var json = JsonSerializer.Serialize(SimulationParametersFileModel.From(parameters), Options);

        json.Should().Contain("\"cauchyInitials\"");
        json.Should().Contain("\"eventDetectionParameters\"");
        json.Should().Contain("\"integrationMethodParameters\"");
        json.Should().Contain("\"resultSavingParameters\"");
        json.Should().Contain("\"startTime\":0.0");
        json.Should().Contain("\"initialStep\":0.1");
        json.Should().Contain("\"selectedMethod\":\"RK45\"");
        json.Should().Contain("\"isAccuracyInUse\":true");
        json.Should().Contain("\"isStableAllowedInUse\":true");
        json.Should().Contain("\"isParallelInUse\":false");
        json.Should().Contain("\"server\":\"localhost\"");
        json.Should().Contain("\"port\":7890");
        json.Should().Contain("\"isEventDetectionInUse\":true");
        json.Should().Contain("\"isStepLimitInUse\":false");
        json.Should().Contain("\"gamma\":0.8");
        json.Should().Contain("\"lowBorder\":0.001");
        json.Should().Contain("\"savingTarget\":\"MEMORY\"");
    }

    [Fact]
    public void Deserialize_OriginalFile_PopulatesModel()
    {
        const string originalJson = """
            {"cauchyInitials":{"startTime":0.0,"endTime":5.5,"initialStep":0.05},"eventDetectionParameters":{"isEventDetectionInUse":false,"isStepLimitInUse":true,"gamma":0.8,"lowBorder":0.001},"integrationMethodParameters":{"selectedMethod":"Euler","accuracy":0.1,"isAccuracyInUse":false,"isStableAllowedInUse":true,"isStableInUse":true,"isParallelInUse":false,"server":"localhost","port":7890},"resultSavingParameters":{"savingTarget":"FILE"}}
            """;

        var fileModel = JsonSerializer.Deserialize<SimulationParametersFileModel>(originalJson, Options);
        var parameters = fileModel!.ToModel();

        parameters.CauchyInitials.StartTime.Should().Be(0.0);
        parameters.CauchyInitials.EndTime.Should().Be(5.5);
        parameters.CauchyInitials.InitialStep.Should().Be(0.05);
        parameters.IntegrationMethod.SelectedMethod.Should().Be("Euler");
        parameters.IntegrationMethod.IsStableAllowedInUse.Should().BeTrue();
        parameters.IntegrationMethod.IsStableInUse.Should().BeTrue();
        parameters.IntegrationMethod.Server.Should().Be("localhost");
        parameters.IntegrationMethod.Port.Should().Be(7890);
        parameters.EventDetection.IsEventDetectionInUse.Should().BeFalse();
        parameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
        parameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }

    [Fact]
    public void RoundTrip_PreservesAllValues()
    {
        var parameters = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials { StartTime = 1.0, EndTime = 99.0, InitialStep = 0.25 },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = "RK23",
                Accuracy = 0.001,
                IsAccuracyInUse = true,
                IsStableAllowedInUse = false,
                IsStableInUse = true,
                IsParallelInUse = true,
                Server = "remote",
                Port = 9999
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = true,
                IsStepLimitInUse = true,
                Gamma = 0.5,
                LowBorder = 0.01
            },
            ResultSaving = new ResultSavingParameters { SavingTarget = SaveTarget.File }
        };

        var json = JsonSerializer.Serialize(SimulationParametersFileModel.From(parameters), Options);
        var restored = JsonSerializer.Deserialize<SimulationParametersFileModel>(json, Options)!.ToModel();

        restored.CauchyInitials.Should().BeEquivalentTo(parameters.CauchyInitials);
        restored.IntegrationMethod.Should().BeEquivalentTo(parameters.IntegrationMethod);
        restored.EventDetection.Should().BeEquivalentTo(parameters.EventDetection);
        restored.ResultSaving.Should().BeEquivalentTo(parameters.ResultSaving);
    }
}
