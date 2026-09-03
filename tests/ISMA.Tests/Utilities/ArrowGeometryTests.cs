using FluentAssertions;
using ISMA.BlueprintEditor.Utilities;

namespace ISMA.Tests.Utilities;

/// <summary>
/// Ported from the original ISMA Kotlin/JavaFX <c>ArrowGeometryTest</c>.
/// </summary>
public class ArrowGeometryTests
{
    private const double LineOffset = 10;
    private const double TextXOffset = 75;
    private const double TextYOffset = 50;

    [Fact]
    public void HorizontalArrowToTheRight()
    {
        var geo = ArrowGeometryCalculator.Calculate(0, 0, 100, 0, 0, 0);

        geo.LineStartX.Should().BeApproximately(0, 0.001);
        geo.LineStartY.Should().BeApproximately(-LineOffset, 0.001);
        geo.LineEndX.Should().BeApproximately(100, 0.001);
        geo.LineEndY.Should().BeApproximately(-LineOffset, 0.001);
        geo.ArrowheadRotation.Should().BeApproximately(-180, 0.001);
    }

    [Fact]
    public void HorizontalArrowToTheLeft()
    {
        var geo = ArrowGeometryCalculator.Calculate(100, 0, 0, 0, 0, 0);

        geo.LineStartX.Should().BeApproximately(100, 0.001);
        geo.LineStartY.Should().BeApproximately(LineOffset, 0.001);
        geo.LineEndX.Should().BeApproximately(0, 0.001);
        geo.LineEndY.Should().BeApproximately(LineOffset, 0.001);
        geo.ArrowheadRotation.Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void VerticalArrowDownward()
    {
        var geo = ArrowGeometryCalculator.Calculate(0, 0, 0, 100, 0, 0);

        geo.LineStartX.Should().BeApproximately(LineOffset, 0.001);
        geo.LineStartY.Should().BeApproximately(0, 0.001);
        geo.LineEndX.Should().BeApproximately(LineOffset, 0.001);
        geo.LineEndY.Should().BeApproximately(100, 0.001);
    }

    [Fact]
    public void VerticalArrowUpward()
    {
        var geo = ArrowGeometryCalculator.Calculate(0, 100, 0, 0, 0, 0);

        geo.LineStartX.Should().BeApproximately(-LineOffset, 0.001);
        geo.LineStartY.Should().BeApproximately(100, 0.001);
    }

    [Fact]
    public void DiagonalArrowBottomRight()
    {
        var geo = ArrowGeometryCalculator.Calculate(0, 0, 100, 100, 0, 0);

        geo.Should().NotBeNull();
        geo.LineEndX.Should().BeGreaterThan(geo.LineStartX);
        geo.LineEndY.Should().BeGreaterThan(geo.LineStartY);
    }

    [Fact]
    public void LayoutOffsetIsAppliedCorrectly()
    {
        var geo1 = ArrowGeometryCalculator.Calculate(50, 50, 150, 50, 0, 0);
        var geo2 = ArrowGeometryCalculator.Calculate(50, 50, 150, 50, 100, 0);

        (geo1.LineStartX - 100).Should().BeApproximately(geo2.LineStartX, 0.001);
        (geo1.LineEndX - 100).Should().BeApproximately(geo2.LineEndX, 0.001);
    }

    [Fact]
    public void LabelPositionIsPerpendicularToLineDirection()
    {
        var geo = ArrowGeometryCalculator.Calculate(0, 0, 100, 0, 0, 0);

        geo.LabelTextTranslateX.Should().BeApproximately(0, 0.001);
        geo.LabelTextTranslateY.Should().BeApproximately(-TextYOffset, 0.001);
    }

    [Fact]
    public void LabelPositionForVerticalDownwardArrow()
    {
        var geo = ArrowGeometryCalculator.Calculate(0, 0, 0, 100, 0, 0);

        geo.LabelTextTranslateX.Should().BeApproximately(TextXOffset, 0.001);
        geo.LabelTextTranslateY.Should().BeApproximately(0, 0.001);
    }
}
