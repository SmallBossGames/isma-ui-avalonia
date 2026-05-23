namespace ISMA.Domain.Models;

public sealed class MetricData
{
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public long SimulationTime => EndTime - StartTime;
}
