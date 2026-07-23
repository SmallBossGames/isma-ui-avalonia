namespace ISMA.Domain.Models;

public sealed class BlueprintTransactionModel
{
    public Guid Id { get; init; } = Guid.Empty;
    public Guid StartStateId { get; init; }
    public Guid EndStateId { get; init; }
    public string Predicate { get; set; } = "";
    public string Alias { get; set; } = "";
}
