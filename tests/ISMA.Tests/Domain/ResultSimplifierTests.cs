using FluentAssertions;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;
using Xunit;

namespace ISMA.Tests.Domain;

/// <summary>
/// Unit tests for ResultSimplifier algorithms.
/// </summary>
public class ResultSimplifierTests
{
    [Fact]
    public void DouglasPeucker_ZeroTolerance_ReturnsAllPoints()
    {
        var points = CreatePoints(10);
        var result = ResultSimplifier.DouglasPeucker(points, 0.0).ToList();
        result.Should().HaveCount(10);
    }

    [Fact]
    public void DouglasPeucker_LargeTolerance_ReturnsMinimalPoints()
    {
        var points = CreatePoints(100);
        var result = ResultSimplifier.DouglasPeucker(points, 1000.0).ToList();
        result.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public void DouglasPeucker_PreservesEndpoints()
    {
        var points = CreatePoints(10);
        var result = ResultSimplifier.DouglasPeucker(points, 0.1).ToList();
        result.First().X.Should().Be(0.0);
        result.Last().X.Should().Be(9.0);
    }

    [Fact]
    public void DouglasPeucker_ReducesPointCount()
    {
        var points = Enumerable.Range(0, 20).Select(i => new SimulationPoint(i, new[] { i * 2.0 }, new List<double[]> { Array.Empty<double>() })).ToList();
        var result = ResultSimplifier.DouglasPeucker(points, 0.5).ToList();
        result.Should().HaveCountLessThan(20);
    }

    [Fact]
    public void RadialDistance_ZeroTolerance_ReturnsAllPoints()
    {
        var points = CreatePoints(10);
        var result = ResultSimplifier.RadialDistance(points, 0.0).ToList();
        result.Should().HaveCount(10);
    }

    [Fact]
    public void RadialDistance_LargeTolerance_ReturnsMinimalPoints()
    {
        var points = CreatePoints(100);
        var result = ResultSimplifier.RadialDistance(points, 1000.0).ToList();
        result.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public void RadialDistance_PreservesEndpoints()
    {
        var points = CreatePoints(10);
        var result = ResultSimplifier.RadialDistance(points, 0.1).ToList();
        result.First().X.Should().Be(0.0);
        result.Last().X.Should().Be(9.0);
    }

    [Fact]
    public void RadialDistance_EmptyInput_ReturnsEmpty()
    {
        var result = ResultSimplifier.RadialDistance(Enumerable.Empty<SimulationPoint>(), 0.1).ToList();
        result.Should().BeEmpty();
    }

    [Fact]
    public void DouglasPeucker_EmptyInput_ReturnsEmpty()
    {
        var result = ResultSimplifier.DouglasPeucker(Enumerable.Empty<SimulationPoint>(), 0.1).ToList();
        result.Should().BeEmpty();
    }

    [Fact]
    public void DouglasPeucker_SinglePoint_ReturnsSamePoint()
    {
        var points = new[] { new SimulationPoint(5.0, new[] { 10.0 }, new List<double[]> { Array.Empty<double>() }) };
        var result = ResultSimplifier.DouglasPeucker(points, 0.1).ToList();
        result.Should().HaveCount(1);
        result.First().X.Should().Be(5.0);
    }

    private static List<SimulationPoint> CreatePoints(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new SimulationPoint(i, new[] { Math.Sin(i * 0.5) * 10.0 }, new List<double[]> { Array.Empty<double>() }))
            .ToList();
    }
}
