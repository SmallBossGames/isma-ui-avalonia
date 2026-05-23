namespace ISMA.Domain.Models;

public sealed class BlueprintTransactionModel
{
    public string StartStateName { get; set; } = "";
    public string EndStateName { get; set; } = "";
    public string Predicate { get; set; } = "";
    public string Alias { get; set; } = "";
}
