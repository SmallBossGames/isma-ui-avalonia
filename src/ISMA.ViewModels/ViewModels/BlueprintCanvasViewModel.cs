namespace ISMA.ViewModels.ViewModels;

/// <summary>
/// Editor-only data tracker for the blueprint canvas. Manages runtime relationships
/// between UI nodes (states, transactions, loops) during an editing session.
/// </summary>
public class BlueprintCanvasViewModel
{
    private readonly List<BlueprintStateViewModel> _states = new();
    private readonly List<BlueprintTransactionViewModel> _transactions = new();
    private readonly List<BlueprintLoopTransactionViewModel> _loops = new();

    public void AddState(BlueprintStateViewModel state)
    {
        _states.Add(state);
    }

    public void AddTransaction(BlueprintTransactionViewModel tx)
    {
        _transactions.Add(tx);
    }

    public void AddLoop(BlueprintLoopTransactionViewModel loop)
    {
        _loops.Add(loop);
    }

    public void RemoveState(string stateName)
    {
        _states.RemoveAll(s => s.Name == stateName);

        _transactions.RemoveAll(tx =>
            tx.StartState.Name == stateName ||
            tx.EndState.Name == stateName);

        _loops.RemoveAll(loop => loop.State.Name == stateName);
    }

    public void RemoveTransaction(string startName, string endName, string predicate)
    {
        _transactions.RemoveAll(tx =>
            tx.StartState.Name == startName &&
            tx.EndState.Name == endName &&
            tx.Predicate == predicate);
    }

    public void RemoveLoop(string stateName)
    {
        _loops.RemoveAll(loop => loop.State.Name == stateName);
    }

    public BlueprintStateViewModel? GetStateByName(string name)
    {
        return _states.FirstOrDefault(s => s.Name == name);
    }

    public void ClearAll()
    {
        _states.Clear();
        _transactions.Clear();
        _loops.Clear();
    }
}
