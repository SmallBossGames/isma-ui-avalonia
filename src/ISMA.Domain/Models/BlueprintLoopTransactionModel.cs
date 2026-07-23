namespace ISMA.Domain.Models;

public sealed class BlueprintLoopTransactionModel
{
    public Guid Id { get; init; } = Guid.Empty;
    public Guid StateId { get; init; }
    public string Predicate { get; set; } = "";
    public string Alias { get; set; } = "";
    public string Text { get; set; } = "";
}
