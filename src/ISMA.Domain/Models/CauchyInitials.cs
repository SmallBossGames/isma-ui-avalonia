namespace ISMA.Domain.Models;

public sealed class CauchyInitials
{
    public double StartTime { get; set; } = 0.0;
    public double EndTime { get; set; } = 10.0;
    public double InitialStep { get; set; } = 0.1;
}
