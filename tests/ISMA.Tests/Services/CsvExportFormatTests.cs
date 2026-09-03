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
        using var bw = new BinaryWriter(fs, Encoding.UTF8);

        bw.Write(Encoding.UTF8.GetBytes("ISMR"));
        bw.Write(1);
        bw.Write(3);
        WriteColumn(bw, "TIME");
        WriteColumn(bw, "y0");
        WriteColumn(bw, "y1");
        bw.Write(2);

        bw.Write(2L);

        bw.Write(0.0);
        bw.Write(2);
        bw.Write(1.0);
        bw.Write(2.0);
        bw.Write(2);
        bw.Write(2);
        bw.Write(0.5);
        bw.Write(0.6);
        bw.Write(1);
        bw.Write(0.7);

        bw.Write(0.1);
        bw.Write(2);
        bw.Write(3.0);
        bw.Write(4.0);
        bw.Write(2);
        bw.Write(2);
        bw.Write(1.5);
        bw.Write(2.6);
        bw.Write(1);
        bw.Write(3.7);

        _tempFiles.Add(path);
        return path;
    }

    private static void WriteColumn(BinaryWriter bw, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        bw.Write(bytes.Length);
        bw.Write(bytes);
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
        lines[1].Should().Be("0.0, 1.0, 2.0, 0.7, 0.5, 0.6");
        lines[2].Should().Be("0.1, 3.0, 4.0, 3.7, 1.5, 2.6");
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
