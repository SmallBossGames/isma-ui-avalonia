namespace ISMA.BlueprintEditor.Models;

public class BlueprintTransactionModel
{
    public string StartStateName { get; }
    public string EndStateName { get; }
    public string Predicate { get; }
    public string Alias { get; }

    public BlueprintTransactionModel(string startStateName, string endStateName, string predicate, string alias = "")
    {
        StartStateName = startStateName;
        EndStateName = endStateName;
        Predicate = predicate;
        Alias = alias;
    }
}
