namespace ISMA.Domain.Models;

public sealed class ErrorInfo
{
    public int Row { get; set; }
    public int Position { get; set; }
    public string FragmentName { get; set; } = "";
    public string Message { get; set; } = "";
}
