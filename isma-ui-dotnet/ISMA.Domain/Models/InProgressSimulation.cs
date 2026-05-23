namespace ISMA.Domain.Models;

public sealed class InProgressSimulation
{
    public int Id { get; set; }
    public string ModelName { get; set; } = "";
    public SimulationParameters Parameters { get; set; } = new();
    public double Progress { get; set; }
}
