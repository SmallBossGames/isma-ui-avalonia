using Xunit;
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
    public void FragmentNameByIndex_ValidIndex_ReturnsFragmentName()
    {
        var region = new CodeRegion("TestRegion", 0, 10);

        region.FragmentNameByIndex(0).Should().Be("Fragment_0");
        region.FragmentNameByIndex(1).Should().Be("Fragment_1");
        region.FragmentNameByIndex(5).Should().Be("Fragment_5");
    }

    [Fact]
    public void FragmentNameByIndex_InvalidIndex_ReturnsFragmentName()
    {
        var region = new CodeRegion("TestRegion", 0, 10);

        region.FragmentNameByIndex(-1).Should().Be("Fragment_-1");
    }

    [Fact]
    public void FragmentNameByIndex_NegativeIndex_ReturnsFragmentName()
    {
        var region = new CodeRegion("TestRegion", 0, 10);

        region.FragmentNameByIndex(-1).Should().Be("Fragment_-1");
        region.FragmentNameByIndex(-100).Should().Be("Fragment_-100");
    }

    [Fact]
    public void FragmentNameByIndex_ZeroIndex_ReturnsFragmentZero()
    {
        var region = new CodeRegion("TestRegion", 0, 10);

        region.FragmentNameByIndex(0).Should().Be("Fragment_0");
    }

    [Fact]
    public void FragmentNameByIndex_LargeIndex_ReturnsFragmentName()
    {
        var region = new CodeRegion("TestRegion", 0, 10);

        region.FragmentNameByIndex(999).Should().Be("Fragment_999");
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
