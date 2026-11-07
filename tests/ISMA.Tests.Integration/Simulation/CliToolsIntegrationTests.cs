using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISMA.ExternalServices.Cli;

namespace ISMA.Tests.Integration.Simulation;

/// <summary>
/// Integration tests for the CLI-based simulation facade. The production
/// <see cref="CliSimulationFacade"/> and launchers are driven against fake
/// compiler/runner executables (shell scripts) that emulate the exact CLI
/// contracts: the compiler reads LISMA source from stdin and writes the
/// compiled .class (or row:col:message error lines) to stdout; the runner
/// takes --model/--start/--end/--step/--method, streams the result to stdout
/// in the real ISMA binary exchange format, and supports --list-methods.
/// </summary>
public class CliToolsIntegrationTests : IDisposable
{
    private readonly string _dir;
    private readonly CliSimulationFacade _facade;
    private readonly List<string> _tempFiles = new();

    public CliToolsIntegrationTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"isma-cli-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);

        var compilerScript = CreateScript("compiler.sh", BuildCompilerScript());
        var runnerScript = CreateScript("runner.sh", BuildRunnerScript());
        CreateResultFile();
        AppContext.SetData("isma.compiler.cli", compilerScript);
        AppContext.SetData("isma.runner.cli", runnerScript);

        var launchers = new CliToolLaunchers();
        _facade = new CliSimulationFacade(
            new LismaCompilerCli(launchers.Compiler),
            new SimulationRunnerCli(launchers.Runner));
    }

    [Fact]
    public async Task Compile_Success_WritesClassFileAndReturnsItsPath()
    {
        var result = await _facade.CompileModel("model m { ... }");

        result.Errors.Should().BeEmpty();
        result.ModelId.Should().NotBeEmpty();
        File.Exists(result.ModelId).Should().BeTrue("the compiled .class file must be stored locally");
        File.ReadAllText(result.ModelId).Should().Be("FAKECLASS");
        File.ReadAllText(Path.Combine(_dir, "compiler-stdin.txt")).Should().Be("model m { ... }");
    }

    [Fact]
    public async Task Compile_Failure_ParsesErrorLines()
    {
        File.WriteAllText(Path.Combine(_dir, "compile-errors.txt"), "3:7:Unknown symbol\n-1:-1:Model too big\n");

        var result = await _facade.CompileModel("bad source");

        result.ModelId.Should().BeEmpty();
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Row.Should().Be(3);
        result.Errors[0].Column.Should().Be(7);
        result.Errors[0].Message.Should().Be("Unknown symbol");
        result.Errors[1].Row.Should().Be(-1);
        result.Errors[1].Column.Should().Be(-1);
        result.Errors[1].Message.Should().Be("Model too big");
    }

    [Fact]
    public async Task Validate_Success_ReturnsNoErrors()
    {
        var result = await _facade.ValidateModel("model m { ... }");

        result.Errors.Should().BeEmpty();
        File.ReadAllText(Path.Combine(_dir, "compiler-stdin.txt")).Should().Be("model m { ... }");
    }

    [Fact]
    public async Task Validate_Failure_SurfacesErrorLines()
    {
        File.WriteAllText(Path.Combine(_dir, "validate-errors.txt"), "1:2:Bad token\n");

        var result = await _facade.ValidateModel("bad source");

        result.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Row = 1, Column = 2, Message = "Bad token" });
    }

    [Fact]
    public async Task RunSimulation_PassesCompiledModelAndParamsToRunner()
    {
        var compileResult = await _facade.CompileModel("model m { ... }");
        compileResult.Errors.Should().BeEmpty();

        var id = await _facade.RunSimulation(new RunSimulationParams
        {
            StartTime = 0,
            EndTime = 1,
            InitialStep = 0.1,
            MethodName = "euler",
            Accuracy = 1e-05,
            IsAccuracyInUse = true,
            IsStabilityControlInUse = true,
            CompiledModelId = compileResult.ModelId,
            IsEventDetectionInUse = true,
            EventDetectionGamma = 0.01,
            EventDetectionLowBorder = -1,
        });

        var argsFile = Path.Combine(_dir, "runner-args.txt");
        await WaitForFileAsync(argsFile);
        var args = File.ReadAllLines(argsFile).ToList();
        args.Should().ContainInOrder("--model", compileResult.ModelId);
        args.Should().ContainInOrder("--start", "0", "--end", "1", "--step", "0.1", "--method", "euler");
        args.Should().ContainInOrder("--accuracy", "1E-05", "--accuracy-enabled");
        args.Should().Contain("--stability-enabled");
        args.Should().ContainInOrder("--gamma", "0.01", "--low-border", "-1");
        File.ReadAllText(Path.Combine(_dir, "runner-model.txt")).Trim().Should().Be(compileResult.ModelId);

        var progress = new List<SimulationProgress>();
        await foreach (var p in _facade.MonitorSimulation(id))
        {
            progress.Add(p);
        }

        progress.Should().NotBeEmpty();
        progress.Select(p => p.CurrentTime).Last().Should().Be(1.0);
        progress.Should().OnlyContain(p => p.StartTime == 0 && p.EndTime == 1);

        var result = await _facade.DownloadResult(id);
        File.Exists(result.File).Should().BeTrue();
        result.ColumnNames.Should().Equal("TIME", "DE_0-x", "DE_1-y", "f0");
    }

    [Fact]
    public async Task RunSimulation_Failure_DownloadThrowsWithStderr()
    {
        var compileResult = await _facade.CompileModel("model m { ... }");
        compileResult.Errors.Should().BeEmpty();
        File.WriteAllText(Path.Combine(_dir, "runner-fail"), "1");

        var id = await _facade.RunSimulation(new RunSimulationParams
        {
            StartTime = 0,
            EndTime = 1,
            InitialStep = 0.1,
            MethodName = "euler",
            CompiledModelId = compileResult.ModelId,
        });

        await foreach (var _ in _facade.MonitorSimulation(id))
        {
            // the runner exits immediately on failure — the monitor must end
        }

        var act = async () => await _facade.DownloadResult(id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*simulation-runner: fake failure*");
    }

    [Fact]
    public async Task MonitorSimulation_CoalescesHighFrequencyStepStream()
    {
        var compileResult = await _facade.CompileModel("model m { ... }");
        compileResult.Errors.Should().BeEmpty();
        File.WriteAllText(Path.Combine(_dir, "runner-many"), "1");
        CreateManyRowsResultFile();

        var id = await _facade.RunSimulation(new RunSimulationParams
        {
            StartTime = 0,
            EndTime = 100,
            InitialStep = 0.001,
            MethodName = "euler",
            CompiledModelId = compileResult.ModelId,
        });

        var count = 0;
        double? last = null;
        await foreach (var p in _facade.MonitorSimulation(id))
        {
            count++;
            last = p.CurrentTime;
        }

        // the runner streams 100_001 rows; yielding one update per row would flood
        // the UI dispatcher, so the facade must coalesce to a bounded number of updates
        // (~200 delta steps + time-based updates + the final one)
        count.Should().BeLessThan(1000, "progress must be coalesced, not one update per integration step");
        last.Should().Be(100.0, "the final step must always be reported");
    }

    [Fact]
    public async Task GetSimulationMethods_ReturnsBundledMethods()
    {
        File.WriteAllText(Path.Combine(_dir, "methods.txt"), "euler\nrk2\nrkfehlberg\n");

        var methods = await _facade.GetSimulationMethods();

        methods.Should().Equal("euler", "rk2", "rkfehlberg");
    }

    [Fact]
    public async Task CancelSimulation_TerminatesRunnerProcess()
    {
        var compileResult = await _facade.CompileModel("model m { ... }");
        compileResult.Errors.Should().BeEmpty();
        File.WriteAllText(Path.Combine(_dir, "runner-slow"), "1");

        var id = await _facade.RunSimulation(new RunSimulationParams
        {
            StartTime = 0,
            EndTime = 1,
            InitialStep = 0.1,
            MethodName = "euler",
            CompiledModelId = compileResult.ModelId,
        });

        // the fake runner traps TERM and removes its marker file — observing
        // the marker disappearance proves the signal reached the process
        var marker = Path.Combine(_dir, "runner-running");
        await WaitForFileAsync(marker);
        await _facade.CancelSimulation(id);
        await WaitForFileGoneAsync(marker);
    }

    [Fact]
    public async Task Shutdown_KillsRunningProcessesAndDeletesModelFiles()
    {
        var compileResult = await _facade.CompileModel("model m { ... }");
        compileResult.Errors.Should().BeEmpty();
        File.Exists(compileResult.ModelId).Should().BeTrue();
        File.WriteAllText(Path.Combine(_dir, "runner-slow"), "1");

        var id = await _facade.RunSimulation(new RunSimulationParams
        {
            StartTime = 0,
            EndTime = 1,
            InitialStep = 0.1,
            MethodName = "euler",
            CompiledModelId = compileResult.ModelId,
        });

        var marker = Path.Combine(_dir, "runner-running");
        await WaitForFileAsync(marker);
        await _facade.Shutdown();

        await WaitForFileGoneAsync(marker);
        File.Exists(compileResult.ModelId).Should().BeFalse("model files must be cleaned on shutdown");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private string CreateScript(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        File.SetUnixFileMode(path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        _tempFiles.Add(path);
        return path;
    }

    private string BuildCompilerScript()
    {
        return $"""
            #!/bin/sh
            DIR='{_dir}'
            cat > "$DIR/compiler-stdin.txt"
            if [ "$1" = "--validate" ]; then
                if [ -f "$DIR/validate-errors.txt" ]; then
                    cat "$DIR/validate-errors.txt"
                    exit 1
                fi
                exit 0
            fi
            if [ -f "$DIR/compile-errors.txt" ]; then
                cat "$DIR/compile-errors.txt"
                exit 1
            fi
            printf 'FAKECLASS'
            exit 0
            """;
    }

    private string BuildRunnerScript()
    {
        return $"""
            #!/bin/sh
            DIR='{_dir}'
            if [ "$1" = "--list-methods" ]; then
                cat "$DIR/methods.txt"
                exit 0
            fi
            for a in "$@"; do printf '%s\n' "$a"; done > "$DIR/runner-args.txt"
            while [ $# -gt 0 ]; do
                case "$1" in
                    --model) printf '%s\n' "$2" > "$DIR/runner-model.txt"; shift 2;;
                    *) shift;;
                esac
            done
            if [ -f "$DIR/runner-fail" ]; then
                echo "simulation-runner: fake failure" >&2
                exit 1
            fi
            if [ -f "$DIR/runner-many" ]; then
                cat "$DIR/result-many.bin"
                exit 0
            fi
            if [ -f "$DIR/runner-slow" ]; then
                trap 'rm -f "$DIR/runner-running"; exit 143' TERM
                printf '1' > "$DIR/runner-running"
                sleep 30 &
                wait
                rm -f "$DIR/runner-running"
                exit 143
            fi
            cat "$DIR/result.bin"
            exit 0
            """;
    }

    /// <summary>
    /// Writes a result file in the real ISMA binary exchange format
    /// (big-endian: short column count, per-column short length + UTF-8 name,
    /// then columnCount big-endian doubles per row until EOF).
    /// </summary>
    private void CreateResultFile()
    {
        var path = Path.Combine(_dir, "result.bin");
        using var fs = File.Create(path);

        var columns = new[] { "TIME", "DE_0-x", "DE_1-y", "f0" };
        WriteUInt16BE(fs, (ushort)columns.Length);
        foreach (var name in columns)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(name);
            WriteUInt16BE(fs, (ushort)bytes.Length);
            fs.Write(bytes);
        }

        WriteRow(fs, 0.0, 1.0, 2.0, -1.0);
        WriteRow(fs, 0.25, 0.9, 1.8, -0.9);
        WriteRow(fs, 0.5, 0.8, 1.6, -0.8);
        WriteRow(fs, 0.75, 0.7, 1.4, -0.7);
        WriteRow(fs, 1.0, 0.6, 1.2, -0.6);

        _tempFiles.Add(path);
    }

    /// <summary>
    /// Writes a result file with 100_001 rows (TIME 0..100 in 0.001 increments)
    /// to emulate a long simulation run.
    /// </summary>
    private void CreateManyRowsResultFile()
    {
        var path = Path.Combine(_dir, "result-many.bin");
        using var fs = File.Create(path);

        var columns = new[] { "TIME", "DE_0-x", "f0" };
        WriteUInt16BE(fs, (ushort)columns.Length);
        foreach (var name in columns)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(name);
            WriteUInt16BE(fs, (ushort)bytes.Length);
            fs.Write(bytes);
        }

        for (var i = 0; i <= 100_000; i++)
        {
            WriteRow(fs, i / 1000.0, 1.0, -1.0);
        }

        _tempFiles.Add(path);
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

    /// <summary>
    /// Waits until the file exists and is non-empty — shell redirects create
    /// the file before its content is written, so existence alone is not enough.
    /// </summary>
    private static async Task WaitForFileAsync(string path)
    {
        for (var i = 0; i < 250; i++)
        {
            try
            {
                if (new FileInfo(path).Length > 0)
                {
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                // not created yet
            }

            await Task.Delay(20);
        }

        Fail($"expected file was not written: {path}");
    }

    private static async Task WaitForFileGoneAsync(string path)
    {
        for (var i = 0; i < 250; i++)
        {
            if (!File.Exists(path))
            {
                return;
            }

            await Task.Delay(20);
        }

        Fail($"file was still present (process not terminated): {path}");
    }

    private static void Fail(string message)
    {
        throw new InvalidOperationException(message);
    }

    public void Dispose()
    {
        _ = _facade.Shutdown();
        AppContext.SetData("isma.compiler.cli", null);
        AppContext.SetData("isma.runner.cli", null);

        foreach (var file in _tempFiles)
        {
            TryDelete(file);
        }

        TryDeleteDirectory(_dir);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best effort
        }
    }
}
