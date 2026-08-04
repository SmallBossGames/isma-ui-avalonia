namespace ISMA.BlueprintEditor.Models;

public record CodeRegion(string Name, int StartLine, int EndLine);

public record LismaTextModel(string FullText, List<CodeRegion> Regions)
{
    public LismaTextModel(string fullText) : this(fullText, new List<CodeRegion>())
    {
    }

    public string? FragmentNameByIndex(int lineNumber)
    {
        foreach (var region in Regions)
        {
            if (lineNumber >= region.StartLine && lineNumber <= region.EndLine)
            {
                return region.Name;
            }
        }
        return null;
    }
}
