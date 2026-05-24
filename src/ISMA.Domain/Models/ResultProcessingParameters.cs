namespace ISMA.Domain.Models;

public sealed class ResultProcessingParameters
{
    public bool IsSimplifyInUse { get; set; }
    public string SelectedSimplifyMethod { get; set; } = "Radial-Distance";
    public double Tolerance { get; set; } = 0.001;
}
