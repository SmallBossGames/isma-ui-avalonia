using FluentAssertions;
using ISMA.BlueprintEditor.Utilities;

namespace ISMA.Tests.Utilities;

/// <summary>
/// Ported from the original ISMA Kotlin/JavaFX <c>NameChangingMonitorTest</c>.
/// </summary>
public class NameChangingMonitorTests
{
    private readonly NameChangingMonitor _monitor = new("New state");

    [Fact]
    public void FirstRegistrationSucceeds()
    {
        _monitor.TryRegister("New state 1").Should().BeTrue();
    }

    [Fact]
    public void UniqueNameRegistrationSucceeds()
    {
        _monitor.TryRegister("MyState").Should().BeTrue();
    }

    [Fact]
    public void DuplicateRegistrationFails()
    {
        _monitor.TryRegister("New state 1");
        _monitor.TryRegister("New state 1").Should().BeFalse();
    }

    [Fact]
    public void UnregisterAllowsReRegistration()
    {
        _monitor.TryRegister("New state 1");
        _monitor.TryUnregister("New state 1").Should().BeTrue();
        _monitor.TryRegister("New state 1").Should().BeTrue();
    }

    [Fact]
    public void UnregisterUnknownNameFails()
    {
        _monitor.TryUnregister("NonExistent").Should().BeFalse();
    }

    [Fact]
    public void CounterIncrementsOnHighNumberedName()
    {
        _monitor.TryRegister("New state 5");
        _monitor.CreateNextDefaultName().Should().Be("New state 6");
    }

    [Fact]
    public void CounterDoesNotDecreaseOnLowerName()
    {
        _monitor.TryRegister("New state 7");
        _monitor.TryRegister("New state 3");
        _monitor.CreateNextDefaultName().Should().Be("New state 8");
    }

    [Fact]
    public void NonDefaultNamesDoNotAffectCounter()
    {
        _monitor.TryRegister("MyState");
        _monitor.CreateNextDefaultName().Should().Be("New state 1");
    }

    [Fact]
    public void CounterPersistsAcrossUnregisterOfHighNumber()
    {
        _monitor.TryRegister("New state 10");
        _monitor.TryUnregister("New state 10");
        _monitor.CreateNextDefaultName().Should().Be("New state 11");
    }

    [Fact]
    public void DefaultNameRegexMatchesOnlyCorrectFormat()
    {
        _monitor.TryRegister("New state 42");
        _monitor.CreateNextDefaultName().Should().Be("New state 43");

        _monitor.TryRegister("New state");
        _monitor.TryRegister("New state abc");
        _monitor.TryRegister("Newstate 5");
        _monitor.CreateNextDefaultName().Should().Be("New state 43");
    }

    [Fact]
    public void MultipleStatesCanCoexist()
    {
        _monitor.TryRegister("State A").Should().BeTrue();
        _monitor.TryRegister("State B").Should().BeTrue();
        _monitor.TryRegister("State C").Should().BeTrue();
        _monitor.CreateNextDefaultName().Should().Be("New state 1");
    }

    [Fact]
    public void CounterRecoversFromGap()
    {
        _monitor.TryRegister("New state 1");
        _monitor.TryRegister("New state 3");
        _monitor.TryUnregister("New state 1");
        _monitor.CreateNextDefaultName().Should().Be("New state 4");
    }
}
