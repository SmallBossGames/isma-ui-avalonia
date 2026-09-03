using System.Collections.Immutable;
using System.Text;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ExternalServices.Server;

public sealed class BinaryFilePointProvider : ISimulationResultReader
{
    private readonly string _filePath;
    private readonly SimulationMetadata _metadata;
    private readonly IEnumerable<SimulationPoint> _points;

    private BinaryFilePointProvider(string filePath, SimulationMetadata metadata, IEnumerable<SimulationPoint> points)
    {
        _filePath = filePath;
        _metadata = metadata;
        _points = points;
    }

    public IEnumerable<SimulationPoint> Results => _points;

    public SimulationMetadata Metadata => _metadata;

    public static SimulationMetadata ReadMetadata(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

        var magic = ReadString(br, 4);
        if (magic != "ISMR")
        {
            throw new InvalidDataException($"Invalid simulation result file: bad magic '{magic}'");
        }

        var version = br.ReadInt32();
        if (version != 1)
        {
            throw new NotSupportedException($"Unsupported simulation result file version: {version}");
        }

        var columnCount = br.ReadInt32();
        var columnNames = new string[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            columnNames[i] = ReadString(br, br.ReadInt32());
        }

        var equationCount = br.ReadInt32();

        return new SimulationMetadata
        {
            ColumnNames = columnNames.ToImmutableArray(),
        };
    }

    public static BinaryFilePointProvider Read(string filePath)
    {
        var metadata = ReadMetadata(filePath);
        var points = ParsePoints(filePath, metadata);
        return new BinaryFilePointProvider(filePath, metadata, points);
    }

    private static IEnumerable<SimulationPoint> ParsePoints(string filePath, SimulationMetadata metadata)
    {
        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

        var magic = ReadString(br, 4);
        var version = br.ReadInt32();
        var columnCount = br.ReadInt32();
        for (var i = 0; i < columnCount; i++)
        {
            ReadString(br, br.ReadInt32());
        }
        br.ReadInt32();

        var pointCount = br.ReadInt64();

        for (var p = 0L; p < pointCount; p++)
        {
            var x = br.ReadDouble();

            var yCount = br.ReadInt32();
            var yForDe = new double[yCount];
            for (var i = 0; i < yCount; i++)
            {
                yForDe[i] = br.ReadDouble();
            }

            var rhsRowCount = br.ReadInt32();
            var rhs = new double[rhsRowCount][];
            for (var r = 0; r < rhsRowCount; r++)
            {
                var rhsColCount = br.ReadInt32();
                rhs[r] = new double[rhsColCount];
                for (var c = 0; c < rhsColCount; c++)
                {
                    rhs[r][c] = br.ReadDouble();
                }
            }

            yield return new SimulationPoint(x, yForDe, rhs);
        }
    }

    private static string ReadString(BinaryReader br, int length)
    {
        if (length <= 0) return string.Empty;
        var bytes = br.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }
}
