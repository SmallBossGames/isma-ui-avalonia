using System;
using System.Collections.Generic;
using System.Linq;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// Result of blueprint validation.
/// </summary>
public record ValidationResult
{
    public bool IsValid => Errors.Length == 0;
    public System.Collections.Immutable.ImmutableArray<string> Errors { get; init; } = System.Collections.Immutable.ImmutableArray<string>.Empty;
}

/// <summary>
/// Validates blueprint editor consistency (state references, duplicates).
/// </summary>
public class BlueprintValidationService
{
    /// <summary>
    /// Validates the blueprint editor state.
    /// Checks: all transition/loop state references exist, no duplicate transitions between same state pair with same predicate.
    /// </summary>
    /// <param name="editor">The editor view model to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    public ValidationResult Validate(BlueprintEditorViewModel editor)
    {
        var errors = new List<string>();

        var allStates = new HashSet<Guid>();
        var mainId = editor.MainState?.Id;
        if (mainId.HasValue && mainId.Value != Guid.Empty)
            allStates.Add(mainId.Value);
        var initId = editor.InitState?.Id;
        if (initId.HasValue && initId.Value != Guid.Empty)
            allStates.Add(initId.Value);
        foreach (var state in editor.States)
        {
            allStates.Add(state.Id);
        }

        // Validate transitions
        foreach (var tx in editor.Transitions)
        {
            if (!allStates.Contains(tx.StartStateId))
            {
                errors.Add($"Transition references non-existent start state: {tx.StartStateId}");
            }
            if (!allStates.Contains(tx.EndStateId))
            {
                errors.Add($"Transition references non-existent end state: {tx.EndStateId}");
            }
        }

        // Validate loop transactions
        foreach (var loop in editor.LoopTransactions)
        {
            if (!allStates.Contains(loop.StateId))
            {
                errors.Add($"Loop references non-existent state: {loop.StateId}");
            }
        }

        // Check for duplicate transitions (same start, end, and predicate)
        var seenTransitions = new HashSet<(Guid, Guid, string)>();
        foreach (var tx in editor.Transitions)
        {
            var key = (tx.StartStateId, tx.EndStateId, tx.Predicate ?? "");
            if (!seenTransitions.Add(key))
            {
                errors.Add($"Duplicate transition from {tx.StartStateId} to {tx.EndStateId} with predicate '{tx.Predicate}'");
            }
        }

        // Check for duplicate loops on same state
        var seenLoops = new HashSet<Guid>();
        foreach (var loop in editor.LoopTransactions)
        {
            if (!seenLoops.Add(loop.StateId))
            {
                errors.Add($"Duplicate loops on state {loop.StateId}");
            }
        }

        return new ValidationResult
        {
            Errors = System.Collections.Immutable.ImmutableArray.CreateRange(errors)
        };
    }
}
