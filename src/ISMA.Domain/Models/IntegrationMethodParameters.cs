namespace ISMA.Domain.Models;

public sealed class IntegrationMethodParameters
{
    public string SelectedMethod { get; set; } = "";
    public double Accuracy { get; set; } = 0.1;
    public bool IsAccuracyInUse { get; set; }
    public bool IsStableInUse { get; set; }
}
