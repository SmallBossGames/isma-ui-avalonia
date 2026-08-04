namespace ISMA.BlueprintEditor.Models;

public class BlueprintLoopTransactionModel
{
    public string StateName { get; }
    public string Predicate { get; }
    public string Alias { get; }
    public string Text { get; }

    public BlueprintLoopTransactionModel(string stateName, string predicate, string alias = "", string text = "")
    {
        StateName = stateName;
        Predicate = predicate;
        Alias = alias;
        Text = text;
    }
}
