namespace ISMA.Domain.Models;

/// <summary>
/// .iscm2 blueprint project file format, compatible with the original ISMA UI.
/// States are referenced by name.
/// </summary>
public sealed class BlueprintFileModel
{
    public BlueprintFileStateModel Main { get; set; } = new();
    public BlueprintFileStateModel Init { get; set; } = new();
    public List<BlueprintFileStateModel> States { get; set; } = new();
    public List<BlueprintFileTransactionModel> Transactions { get; set; } = new();
    public List<BlueprintFileLoopTransactionModel> LoopTransactions { get; set; } = new();
}

/// <summary>
/// State entry in the .iscm2 file format.
/// </summary>
public sealed class BlueprintFileStateModel
{
    public double CanvasPositionX { get; set; }
    public double CanvasPositionY { get; set; }
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// Transition entry in the .iscm2 file format.
/// </summary>
public sealed class BlueprintFileTransactionModel
{
    public string StartStateName { get; set; } = "";
    public string EndStateName { get; set; } = "";
    public string Predicate { get; set; } = "";
    public string Alias { get; set; } = "";
}

/// <summary>
/// Loop transaction entry in the .iscm2 file format.
/// </summary>
public sealed class BlueprintFileLoopTransactionModel
{
    public string StateName { get; set; } = "";
    public string Predicate { get; set; } = "";
    public string Alias { get; set; } = "";
    public string Text { get; set; } = "";
}
