namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A self-transition (loop) on a state, with its own LISMA body.
/// </summary>
/// <param name="StateName">Name of the state the loop is attached to.</param>
/// <param name="Predicate">LISMA guard predicate of the loop.</param>
/// <param name="Alias">Optional display name shown on the loop arrow.</param>
/// <param name="Text">LISMA body code of the loop (compiled to a pseudo-state).</param>
public record BlueprintLoopTransactionModel(string StateName, string Predicate, string Alias = "", string Text = "");
