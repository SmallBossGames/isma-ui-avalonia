namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A directed transition between two states, referencing them by name.
/// </summary>
/// <param name="StartStateName">Name of the source state.</param>
/// <param name="EndStateName">Name of the target state.</param>
/// <param name="Predicate">LISMA guard predicate of the transition.</param>
/// <param name="Alias">Optional display name shown on the arrow.</param>
public record BlueprintTransactionModel(string StartStateName, string EndStateName, string Predicate, string Alias = "");
