using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.BlueprintEditor.Utilities;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// The canvas document model: states, transactions and loops with name uniqueness
/// bookkeeping via a <see cref="NameChangingMonitor"/>.
/// </summary>
public partial class CanvasViewModel : ObservableObject
{
    private readonly NameChangingMonitor nameMonitor = new("State");

    /// <summary>All states on the canvas (Main, init and user states).</summary>
    public ObservableCollection<StateViewModel> States { get; } = new();

    /// <summary>Directed transitions between states.</summary>
    public ObservableCollection<TransactionViewModel> Transactions { get; } = new();

    /// <summary>Self-transitions (loops) on states.</summary>
    public ObservableCollection<LoopTransactionViewModel> LoopTransactions { get; } = new();

    /// <summary>
    /// Creates a state whose name uniqueness is gated by the canvas name monitor
    /// (a candidate is unique if it equals the state's own name or is not registered).
    /// </summary>
    public StateViewModel CreateState(string text, double x, double y, string name, StateKind kind, double squareWidth, double squareHeight)
    {
        return new StateViewModel(
            name: name,
            text: text,
            x: x,
            y: y,
            squareWidth: squareWidth,
            squareHeight: squareHeight,
            kind: kind,
            isNameUnique: candidate => candidate == name || !nameMonitor.IsRegistered(candidate));
    }

    /// <summary>Adds a state and registers its name.</summary>
    public void AddState(StateViewModel state)
    {
        States.Add(state);
        nameMonitor.TryRegister(state.Name);
    }

    /// <summary>
    /// Removes a state, cascades to transactions/loops referencing its name,
    /// and unregisters the name.
    /// </summary>
    public void RemoveState(StateViewModel state)
    {
        States.Remove(state);
        foreach (var t in Transactions.Where(t => t.StartState == state || t.EndState == state).ToList())
        {
            Transactions.Remove(t);
        }

        foreach (var l in LoopTransactions.Where(l => l.State == state).ToList())
        {
            LoopTransactions.Remove(l);
        }

        nameMonitor.TryUnregister(state.Name);
    }

    /// <summary>Adds a transaction, resolving its start/end state references by name.</summary>
    public void AddTransaction(TransactionViewModel tx)
    {
        tx.StartState = StateByName(tx.StartStateName);
        tx.EndState = StateByName(tx.EndStateName);
        Transactions.Add(tx);
    }

    /// <summary>Removes a transaction.</summary>
    public void RemoveTransaction(TransactionViewModel tx)
    {
        Transactions.Remove(tx);
    }

    /// <summary>Adds a loop, resolving its state reference by name.</summary>
    public void AddLoopTransaction(LoopTransactionViewModel loop)
    {
        loop.State = StateByName(loop.StateName);
        LoopTransactions.Add(loop);
    }

    /// <summary>Removes a loop.</summary>
    public void RemoveLoopTransaction(LoopTransactionViewModel loop)
    {
        LoopTransactions.Remove(loop);
    }

    /// <summary>Clears all states/transactions/loops and resets the name monitor.</summary>
    public void ClearAll()
    {
        States.Clear();
        Transactions.Clear();
        LoopTransactions.Clear();
        nameMonitor.Reset();
    }

    /// <summary>First state with the given name, or null.</summary>
    public StateViewModel? StateByName(string name)
    {
        return States.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>Next default state name ("State N") with counter recovery.</summary>
    public string CreateNextDefaultStateName()
    {
        return nameMonitor.CreateNextDefaultName();
    }
}
