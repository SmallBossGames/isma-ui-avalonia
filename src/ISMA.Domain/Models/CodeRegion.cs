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
}
