using System.Collections.Immutable;

namespace ISMA.Domain.Dtos;

/// <summary>
/// A finished simulation result: the result file in the ISMA binary exchange format
/// and the column names from its header.
/// </summary>
public sealed record SimulationResult(string File, ImmutableArray<string> ColumnNames);
