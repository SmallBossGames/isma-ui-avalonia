namespace ISMA.Domain.Models;

public sealed class IntegrationMethodParameters
{
    public string SelectedMethod { get; set; } = "";
    public double Accuracy { get; set; } = 0.1;
    public bool IsAccuracyInUse { get; set; }
    public bool IsStableAllowedInUse { get; set; }
    public bool IsStableInUse { get; set; }
    public bool IsParallelInUse { get; set; }
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 7890;
}
