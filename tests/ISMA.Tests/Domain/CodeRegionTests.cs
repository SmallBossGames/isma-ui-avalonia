global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class CodeRegionTests
{
    [Fact]
    public void LineRangeTracking_CorrectlyStoresStartAndEndLines()
    {
        var region = new CodeRegion("Main", 0, 5);

        region.Name.Should().Be("Main");
        region.StartLine.Should().Be(0);
        region.EndLine.Should().Be(5);
    }

    [Fact]
    public void LineRangeTracking_EndLineEqualsStartLine_IsValidSingleLineRegion()
    {
        var region = new CodeRegion("SingleLine", 10, 10);

        region.StartLine.Should().Be(10);
        region.EndLine.Should().Be(10);
    }

    [Fact]
    public void LineRangeTracking_LargeLineNumbers_AreStoredCorrectly()
    {
        var region = new CodeRegion("LargeRegion", 1000, 2000);

        region.StartLine.Should().Be(1000);
        region.EndLine.Should().Be(2000);
    }

    [Fact]
    public void FragmentNameByLine_LineInsideRegion_ReturnsRegionName()
    {
        var model = new LismaTextModel("text", new[]
        {
            new CodeRegion("Main", 1, 3),
            new CodeRegion("State A", 5, 8)
        });

        model.FragmentNameByLine(1).Should().Be("Main");
        model.FragmentNameByLine(3).Should().Be("Main");
        model.FragmentNameByLine(5).Should().Be("State A");
        model.FragmentNameByLine(8).Should().Be("State A");
    }

    [Fact]
    public void FragmentNameByLine_LineOutsideAllRegions_ReturnsDefaultFragment()
    {
        var model = new LismaTextModel("text", new[]
        {
            new CodeRegion("Main", 1, 3)
        });

        model.FragmentNameByLine(0).Should().Be("Main");
        model.FragmentNameByLine(4).Should().Be("Main");
        model.FragmentNameByLine(-1).Should().Be("Main");
    }

    [Fact]
    public void FragmentNameByLine_NoRegions_ReturnsDefaultFragment()
    {
        var model = new LismaTextModel("text", Array.Empty<CodeRegion>());

        model.FragmentNameByLine(1).Should().Be("Main");
    }

    [Fact]
    public void Constructor_NameCanBeAnyString()
    {
        var region1 = new CodeRegion("", 0, 0);
        var region2 = new CodeRegion("a".PadLeft(1000, 'a'), 0, 0);

        region1.Name.Should().Be("");
        region2.Name.Should().HaveLength(1000);
    }
}
