using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class CodeRegion
{
    public string Name { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public CodeRegion(string name, int startLine, int endLine)
    {
        Name = name;
        StartLine = startLine;
        EndLine = endLine;
    }

    public string FragmentNameByIndex(int index)
    {
        return index >= 0 && index < 0 ? "Unknown" : $"Fragment_{index}";
    }
}

public sealed class LismaTextModel
{
    public string FullText { get; set; }
    public ImmutableArray<CodeRegion> Regions { get; set; }

    public static readonly CodeRegion DefaultFragment = new("Main", 0, 0);

    public LismaTextModel(string fullText, IEnumerable<CodeRegion> regions)
    {
        FullText = fullText;
        Regions = regions.ToImmutableArray();
    }
}
