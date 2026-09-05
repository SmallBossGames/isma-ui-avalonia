global using global::Xunit;
using System.IO;
using System.Text;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ExternalServices.ChartViewer;

namespace ISMA.Tests.Services;

/// <summary>
/// Verifies CSV export matches the original Kotlin SimulationResultService format:
/// header "x, DE codes, AE codes, f0..f(n-1)" and rows "x, yForDe, AE rhs, DE rhs"
/// with ", " separators.
/// </summary>
public class CsvExportFormatTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string CreateResultFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"isma-test-{Guid.NewGuid():N}.bin");
        using var fs = File.Create(path);

        // Real ISMA exchange format (big-endian, as written by the server's Java
        // DataOutputStream): short column count, then per column short name length
        // + UTF-8 name; no magic, no version, no row count.
        var columns = new[] { "TIME", "DE_0-y0", "DE_1-y1", "AE_0-z0", "f0", "f1" };
        WriteUInt16BE(fs, (ushort)columns.Length);
        foreach (var name in columns)
        {
            var bytes = Encoding.UTF8.GetBytes(name);
            WriteUInt16BE(fs, (ushort)bytes.Length);
            fs.Write(bytes);
        }

        // Rows: columnCount big-endian doubles each, layout [x, DE values, AE values, f values], until EOF.
        WriteRow(fs, 0.0, 1.0, 2.0, 0.7, 0.5, 0.6);
        WriteRow(fs, 0.1, 3.0, 4.0, 3.7, 1.5, 2.6);

        _tempFiles.Add(path);
        return path;
    }

    private static void WriteUInt16BE(Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    private static void WriteRow(Stream stream, params double[] values)
    {
        foreach (var value in values)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            stream.Write(bytes);
        }
    }

    private sealed class TestEquationIndexProvider : IEquationIndexProvider
    {
        public int GetDifferentialEquationCount() => 2;
        public int GetAlgebraicEquationCount() => 1;
        public string GetDifferentialEquationCode(int index) => index == 0 ? "y0" : "y1";
        public string GetAlgebraicEquationCode(int index) => "z0";
    }

    [Fact]
    public async Task ExportToFile_WritesOriginalFormat()
    {
        var resultFile = CreateResultFile();
        var csvFile = Path.Combine(Path.GetTempPath(), $"isma-test-{Guid.NewGuid():N}.csv");
        _tempFiles.Add(csvFile);

        var completed = new CompletedSimulation
        {
            Id = 1,
            ModelName = "Test",
            EquationIndexProvider = new TestEquationIndexProvider(),
            CachedFile = resultFile
        };

        var service = new SimulationResultService(new GrinProcessLauncher());

        await service.ExportToFile(completed, csvFile);

        var lines = File.ReadAllLines(csvFile);
        lines[0].Should().Be("x, y0, y1, z0, f0, f1");
        lines[1].Should().Be("0.0, 1.0, 2.0, 0.7, 0.7, 0.5, 0.6");
        lines[2].Should().Be("0.1, 3.0, 4.0, 3.7, 3.7, 1.5, 2.6");
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
