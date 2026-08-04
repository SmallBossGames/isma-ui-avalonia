using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.BlueprintEditor.Utilities;

namespace ISMA.BlueprintEditor.ViewModels;

public partial class CanvasViewModel : ObservableObject
{
    private readonly NameChangingMonitor _nameMonitor;
    private readonly List<StateViewModel> _states = new();
    private readonly List<TransactionViewModel> _transactions = new();
    private readonly List<LoopTransactionViewModel> _loopTransactions = new();

    public CanvasViewModel(string defaultStateName = "State")
    {
        _nameMonitor = new NameChangingMonitor(defaultStateName);
    }

    public IReadOnlyList<StateViewModel> States => _states;

    public IReadOnlyList<TransactionViewModel> Transactions => _transactions;

    public IReadOnlyList<LoopTransactionViewModel> LoopTransactions => _loopTransactions;

    public void AddTransaction(TransactionViewModel transaction)
    {
        _transactions.Add(transaction);
    }

    public void AddLoopTransaction(LoopTransactionViewModel loopTransaction)
    {
        _loopTransactions.Add(loopTransaction);
    }

    public void AddState(StateViewModel state)
    {
        if (!_nameMonitor.TryRegister(state.Name))
        {
            throw new InvalidOperationException($"State name '{state.Name}' is already taken.");
        }
        _states.Add(state);
    }

    public void RemoveState(StateViewModel state)
    {
        _nameMonitor.TryUnregister(state.Name);
        _states.Remove(state);

        _transactions.RemoveAll(tx => tx.StartStateName == state.Name || tx.EndStateName == state.Name);
        _loopTransactions.RemoveAll(loop => loop.StateName == state.Name);
    }

    public void RemoveTransaction(TransactionViewModel transaction)
    {
        _transactions.Remove(transaction);
    }

    public void RemoveLoopTransaction(LoopTransactionViewModel loopTransaction)
    {
        _loopTransactions.Remove(loopTransaction);
    }

    public void ClearAll()
    {
        _states.Clear();
        _transactions.Clear();
        _loopTransactions.Clear();
        _nameMonitor.Clear();
    }

    public bool TryRegisterStateName(string name)
    {
        return _nameMonitor.TryRegister(name);
    }

    public bool TryUnregisterStateName(string name)
    {
        return _nameMonitor.TryUnregister(name);
    }

    public string CreateNextDefaultStateName()
    {
        return _nameMonitor.CreateNextDefaultName();
    }

    public StateViewModel CreateState(
        string name,
        string color,
        double x,
        double y,
        double width,
        double height,
        bool editable)
    {
        var state = new StateViewModel(name, string.Empty, x, y, width, height, color, editable);
        state.IsNameUnique = TryRegisterStateName;
        AddState(state);
        return state;
    }
}
