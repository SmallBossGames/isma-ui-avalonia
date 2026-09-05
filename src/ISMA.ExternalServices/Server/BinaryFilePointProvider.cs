using System.Text;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ExternalServices.Server;

/// <summary>
/// Reads simulation result files in the ISMA binary exchange format written by
/// the server (big-endian, Java <c>DataOutputStream</c>): a <c>short</c> column
/// count, then per column a <c>short</c> name length + UTF-8 name, then
/// <c>columnCount</c> big-endian doubles per row until EOF. Row layout:
/// [x, DE values, AE values, f values].
/// </summary>
public sealed class BinaryFilePointProvider : ISimulationResultReader
{
    private readonly SimulationMetadata _metadata;
    private readonly IEnumerable<SimulationPoint> _points;

    private BinaryFilePointProvider(SimulationMetadata metadata, IEnumerable<SimulationPoint> points)
    {
        _metadata = metadata;
        _points = points;
    }

    public IEnumerable<SimulationPoint> Results => _points;

    public SimulationMetadata Metadata => _metadata;

    public static SimulationMetadata ReadMetadata(string filePath)
    {
        using var fs = File.OpenRead(filePath);

        var columnCount = ReadUInt16BE(fs);
        var columnNames = new string[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            var length = ReadUInt16BE(fs);
            var bytes = new byte[length];
            ReadFully(fs, bytes);
            columnNames[i] = Encoding.UTF8.GetString(bytes);
        }

        return new SimulationMetadata
        {
            ColumnNames = [.. columnNames],
        };
    }

    public static BinaryFilePointProvider Read(string filePath)
    {
        var metadata = ReadMetadata(filePath);
        var points = ParsePoints(filePath, metadata);
        return new BinaryFilePointProvider(metadata, points);
    }

    private static IEnumerable<SimulationPoint> ParsePoints(string filePath, SimulationMetadata metadata)
    {
        var columnNames = metadata.ColumnNames;
        var columnCount = columnNames.Length;
        if (columnCount == 0)
        {
            yield break;
        }

        var deCount = 0;
        var aeCount = 0;
        foreach (var name in columnNames)
        {
            if (name.StartsWith("DE_"))
            {
                deCount++;
            }
            else if (name.StartsWith("AE_"))
            {
                aeCount++;
            }
        }

        using var fs = File.OpenRead(filePath);

        // Skip the header (column count + per-column name length + name bytes).
        ReadUInt16BE(fs);
        for (var i = 0; i < columnCount; i++)
        {
            var length = ReadUInt16BE(fs);
            fs.Seek(length, SeekOrigin.Current);
        }

        var row = new double[columnCount];
        while (true)
        {
            var complete = true;
            for (var i = 0; i < columnCount; i++)
            {
                try
                {
                    row[i] = ReadDoubleBE(fs);
                }
                catch (EndOfStreamException)
                {
                    complete = false;
                    break;
                }
            }

            if (!complete)
            {
                yield break;
            }

            // Row layout written by the server: [x, DE values, AE values, f values].
            // Mapping ported verbatim from the original Kotlin BinaryFilePointProvider.
            var yForDe = new double[deCount + aeCount];
            for (var i = 0; i < yForDe.Length; i++)
            {
                yForDe[i] = row[1 + i];
            }

            var rhsDe = new double[deCount];
            for (var i = 0; i < rhsDe.Length; i++)
            {
                rhsDe[i] = row[1 + deCount + aeCount + i];
            }

            var rhsAe = new double[aeCount];
            for (var i = 0; i < rhsAe.Length; i++)
            {
                rhsAe[i] = row[1 + deCount + i];
            }

            yield return new SimulationPoint(row[0], yForDe, [rhsDe, rhsAe]);
        }
    }

    private static ushort ReadUInt16BE(Stream stream)
    {
        var hi = stream.ReadByte();
        var lo = stream.ReadByte();
        if (hi < 0 || lo < 0)
        {
            throw new EndOfStreamException();
        }

        return (ushort)((hi << 8) | lo);
    }

    private static double ReadDoubleBE(Stream stream)
    {
        var bytes = new byte[8];
        ReadFully(stream, bytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToDouble(bytes, 0);
    }

    private static void ReadFully(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var count = stream.Read(buffer, offset, buffer.Length - offset);
            if (count == 0)
            {
                throw new EndOfStreamException();
            }

            offset += count;
        }
    }
}
