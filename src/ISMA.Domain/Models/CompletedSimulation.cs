using System.Collections.Immutable;
using ISMA.Domain.Contracts;

namespace ISMA.Domain.Models;

public sealed class CompletedSimulation
{
    public int Id { get; set; }
    public string ModelName { get; set; } = "";
    public IEquationIndexProvider? EquationIndexProvider { get; set; }
    public MetricData MetricData { get; set; } = new();
    public SimulationParameters Parameters { get; set; } = new();
    public string CachedFile { get; set; } = "";
    public ImmutableArray<string> CachedColumnNames { get; set; } = ImmutableArray<string>.Empty;
}
