using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class SimulationMetadata
{
    public ImmutableArray<string> ColumnNames { get; set; } = ImmutableArray<string>.Empty;
}
