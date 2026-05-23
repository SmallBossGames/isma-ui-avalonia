namespace ISMA.Domain.Models;

public sealed class SimulationParameters
{
    public CauchyInitials CauchyInitials { get; set; } = new();
    public IntegrationMethodParameters IntegrationMethod { get; set; } = new();
    public EventDetectionParameters EventDetection { get; set; } = new();
    public ResultSavingParameters ResultSaving { get; set; } = new();
    public ResultProcessingParameters ResultProcessing { get; set; } = new();
}
