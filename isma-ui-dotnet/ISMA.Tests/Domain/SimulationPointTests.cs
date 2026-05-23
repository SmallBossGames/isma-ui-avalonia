using Xunit;
using System.Collections.Immutable;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class SimulationPointTests
{
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0 }, Array.Empty<IEnumerable<double>>());
        var point2 = new SimulationPoint(1.0, new[] { 2.0 }, Array.Empty<IEnumerable<double>>());

        point1.Equals(point2).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameObject_ReturnsTrue()
    {
        var point = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });
        point.Equals(point).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var point = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });
        point.Equals((SimulationPoint?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentX_ReturnsFalse()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });
        var point2 = new SimulationPoint(2.0, new[] { 2.0 }, new[] { new[] { 3.0 } });

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentYForDe_ReturnsFalse()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0, 3.0 }, new[] { new[] { 4.0 } });
        var point2 = new SimulationPoint(1.0, new[] { 2.0, 4.0 }, new[] { new[] { 4.0 } });

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentRhs_ReturnsFalse()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0, 4.0 } });
        var point2 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0, 5.0 } });

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_ProducesSameHash()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0, 3.0 }, new[] { new[] { 4.0, 5.0 } });
        var point2 = new SimulationPoint(1.0, new[] { 2.0, 3.0 }, new[] { new[] { 4.0, 5.0 } });

        point1.GetHashCode().Should().Be(point2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ProducesDifferentHash()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });
        var point2 = new SimulationPoint(2.0, new[] { 2.0 }, new[] { new[] { 3.0 } });

        point1.GetHashCode().Should().NotBe(point2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentArrayLengths_ReturnsFalse()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0, 3.0 }, new[] { new[] { 4.0 } });
        var point2 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 4.0 } });

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentRhsArrayLengths_ReturnsFalse()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 }, new[] { 4.0 } });
        var point2 = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void Constructor_StoresValuesCorrectly()
    {
        var yValues = new[] { 1.0, 2.0, 3.0 };
        var rhsValues = new[] { new[] { 4.0, 5.0 }, new[] { 6.0, 7.0 } };

        var point = new SimulationPoint(0.5, yValues, rhsValues);

        point.X.Should().Be(0.5);
        point.YForDe.Should().BeEquivalentTo(yValues);
        point.Rhs.Should().HaveCount(2);
        point.Rhs[0].Should().BeEquivalentTo(new[] { 4.0, 5.0 });
        point.Rhs[1].Should().BeEquivalentTo(new[] { 6.0, 7.0 });
    }

    [Fact]
    public void Equals_ObjectEquals_WithSameType_ReturnsTrue()
    {
        var point1 = new SimulationPoint(1.0, new[] { 2.0 }, Array.Empty<IEnumerable<double>>());
        SimulationPoint? point2 = new SimulationPoint(1.0, new[] { 2.0 }, Array.Empty<IEnumerable<double>>());

        point1.Equals(point2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectEquals_WithNull_ReturnsFalse()
    {
        var point = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });
        object? obj = null;

        point.Equals(obj).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectEquals_WithWrongType_ReturnsFalse()
    {
        var point = new SimulationPoint(1.0, new[] { 2.0 }, new[] { new[] { 3.0 } });
        object obj = "not a simulation point";

        point.Equals(obj).Should().BeFalse();
    }
}
