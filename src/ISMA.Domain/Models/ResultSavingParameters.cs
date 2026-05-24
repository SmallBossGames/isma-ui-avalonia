namespace ISMA.Domain.Models;

public enum SaveTarget { Memory, File }

public sealed class ResultSavingParameters
{
    public SaveTarget SavingTarget { get; set; } = SaveTarget.Memory;
}
