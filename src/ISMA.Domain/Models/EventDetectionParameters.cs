namespace ISMA.Domain.Models;

public sealed class EventDetectionParameters
{
    public bool IsEventDetectionInUse { get; set; }
    public double Gamma { get; set; } = 0.8;
    public double LowBorder { get; set; } = 0.001;
}
