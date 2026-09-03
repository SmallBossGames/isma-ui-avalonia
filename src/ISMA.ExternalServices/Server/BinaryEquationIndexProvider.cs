using System.Collections.Immutable;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ExternalServices.Server;

public sealed class BinaryEquationIndexProvider : IEquationIndexProvider
{
    private readonly ImmutableArray<string> _columnNames;
    private readonly ImmutableArray<int> _deIndices;
    private readonly ImmutableArray<int> _aeIndices;
    private readonly ImmutableArray<int> _fIndices;

    public BinaryEquationIndexProvider(ImmutableArray<string> columnNames)
    {
        _columnNames = columnNames;

        var deList = new List<int>();
        var aeList = new List<int>();
        var fList = new List<int>();

        for (var i = 0; i < columnNames.Length; i++)
        {
            var name = columnNames[i];
            if (name.StartsWith("DE_", StringComparison.Ordinal))
            {
                deList.Add(i);
            }
            else if (name.StartsWith("AE_", StringComparison.Ordinal))
            {
                aeList.Add(i);
            }
            else if (name.StartsWith("f", StringComparison.Ordinal))
            {
                fList.Add(i);
            }
        }

        _deIndices = deList.ToImmutableArray();
        _aeIndices = aeList.ToImmutableArray();
        _fIndices = fList.ToImmutableArray();
    }

    public static BinaryEquationIndexProvider FromMetadata(SimulationMetadata metadata)
    {
        return new BinaryEquationIndexProvider(metadata.ColumnNames);
    }

    public int GetDifferentialEquationCount() => _deIndices.Length;

    public int GetAlgebraicEquationCount() => _aeIndices.Length;

    public string GetDifferentialEquationCode(int index)
    {
        if (index < 0 || index >= _deIndices.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Differential equation index {index} out of range [0, {_deIndices.Length})");
        }
        return _columnNames[_deIndices[index]];
    }

    public string GetAlgebraicEquationCode(int index)
    {
        if (index < 0 || index >= _aeIndices.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Algebraic equation index {index} out of range [0, {_aeIndices.Length})");
        }
        return _columnNames[_aeIndices[index]];
    }

    public int GetForcingFunctionCount() => _fIndices.Length;

    public string GetForcingFunctionCode(int index)
    {
        if (index < 0 || index >= _fIndices.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Forcing function index {index} out of range [0, {_fIndices.Length})");
        }
        return _columnNames[_fIndices[index]];
    }
}
