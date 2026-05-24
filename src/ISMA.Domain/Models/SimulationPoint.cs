using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class SimulationPoint : IEquatable<SimulationPoint>
{
    public double X { get; }
    public ImmutableArray<double> YForDe { get; }
    public ImmutableArray<ImmutableArray<double>> Rhs { get; }

    public SimulationPoint(double x, IEnumerable<double> yForDe, IEnumerable<IEnumerable<double>> rhs)
    {
        X = x;
        YForDe = yForDe.ToImmutableArray();
        Rhs = rhs.Select(r => r.ToImmutableArray()).ToImmutableArray();
    }

    public bool Equals(SimulationPoint? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return X == other.X && YForDe.SequenceEqual(other.YForDe) && Rhs.SequenceEqual(other.Rhs);
    }

    public override bool Equals(object? obj) => Equals(obj as SimulationPoint);

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + X.GetHashCode();
        foreach (var y in YForDe) hash = hash * 31 + y.GetHashCode();
        foreach (var r in Rhs)
            foreach (var v in r) hash = hash * 31 + v.GetHashCode();
        return hash;
    }
}
