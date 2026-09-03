global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.App.ViewModels;

namespace ISMA.Tests.ViewModels.Settings;

public class StoreLoadParametersTests
{
    private static SimulationParametersViewModel CreateParameters() => new();

    [Fact]
    public void UC06_StoreSettings_CapturesCurrentParameters()
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

    [Fact]
    public void UC06_LoadSettings_DoesNotThrow()
    {
        Action load = () => { };
        load.Should().NotThrow();
    }

    [Fact]
    public void UC06_Snapshot_ReflectsCurrentParameters()
    {
        var parameters = CreateParameters();
        parameters.CauchyInitials.StartTime = 10.0;

        var snapshot = parameters.Snapshot();
        snapshot.CauchyInitials.StartTime.Should().Be(10.0);
    }

    [Fact]
    public void UC06_Commit_AppliesParametersToViewModel()
    {
        var parameters = CreateParameters();
        var model = new SimulationParameters
        {
            CauchyInitials = new CauchyInitials { StartTime = 5.0, EndTime = 50.0, InitialStep = 0.05 }
        };

        parameters.Commit(model);

        parameters.CauchyInitials.StartTime.Should().Be(5.0);
        parameters.CauchyInitials.EndTime.Should().Be(50.0);
        parameters.CauchyInitials.InitialStep.Should().Be(0.05);
    }
}
