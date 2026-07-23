using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Services;

/// <summary>
/// Default implementation of <see cref="IBlueprintValidationService"/>.
/// </summary>
public class BlueprintValidationService : IBlueprintValidationService
{
    /// <inheritdoc />
    public BlueprintValidationResult Validate(BlueprintEditorViewModel editor)
    {
        var errors = new List<string>();

        var stateIds = new HashSet<Guid>(editor.States.Select(s => s.Id));
        stateIds.Add(editor.MainState.Id);
        stateIds.Add(editor.InitState.Id);

        foreach (var transition in editor.Transitions)
        {
            if (!stateIds.Contains(transition.StartStateId))
            {
                errors.Add($"Transition references non-existent start state: {transition.StartStateId}");
            }

            if (!stateIds.Contains(transition.EndStateId))
            {
                errors.Add($"Transition references non-existent end state: {transition.EndStateId}");
            }
        }

        foreach (var loop in editor.LoopTransactions)
        {
            if (!stateIds.Contains(loop.StateId))
            {
                errors.Add($"Loop references non-existent state: {loop.StateId}");
            }
        }

        var transitionPairs = new HashSet<(Guid, Guid)>();
        foreach (var transition in editor.Transitions)
        {
            var pair = (transition.StartStateId, transition.EndStateId);
            if (!transitionPairs.Add(pair))
            {
                errors.Add($"Duplicate transition from {pair.Item1} to {pair.Item2}");
            }
        }

        var loopStateIds = new HashSet<Guid>();
        foreach (var loop in editor.LoopTransactions)
        {
            if (!loopStateIds.Add(loop.StateId))
            {
                errors.Add($"Duplicate loop on state: {loop.StateId}");
            }
        }

        return errors.Count > 0 ? BlueprintValidationResult.Invalid(errors) : BlueprintValidationResult.Valid();
    }
}
