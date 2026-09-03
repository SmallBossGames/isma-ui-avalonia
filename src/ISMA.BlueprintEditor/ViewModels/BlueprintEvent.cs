namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// Events the view model fires toward the editor view
/// (e.g. to open state/loop body editor tabs).
/// </summary>
public abstract record BlueprintEvent
{
    /// <summary>Open (or switch to) the text editor tab of a state body.</summary>
    public sealed record OpenStateEditor(StateViewModel State) : BlueprintEvent;

    /// <summary>Open (or switch to) the text editor tab of a loop body.</summary>
    public sealed record OpenLoopEditor(LoopTransactionViewModel Loop, StateViewModel State) : BlueprintEvent;
}
