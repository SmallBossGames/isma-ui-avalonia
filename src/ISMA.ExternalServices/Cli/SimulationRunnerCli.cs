using System.Diagnostics;
using System.Globalization;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// Launches the standalone simulation runner CLI (<c>simulation-runner-cli</c>).
/// Contract: <c>--model &lt;class&gt; --out &lt;result.bin&gt; --progress &lt;file&gt;
/// --start --end --step --method</c> plus optional accuracy/stability/event-detection
/// flags. Exit 0 is success, exit 1 is failure (<c>simulation-runner: &lt;message&gt;</c>
/// on stderr, <c>--out</c> deleted). <c>--list-methods</c> prints bundled integration
/// method names, one per line. The progress file is plain text, overwritten each
/// step with the current time.
/// </summary>
public sealed class SimulationRunnerCli
{
    private readonly ILogger<SimulationRunnerCli>? _logger;

    public SimulationRunnerCli(ILogger<SimulationRunnerCli>? logger = null)
    {
        _logger = logger;
    }

    public string ResolveScriptPath()
    {
        var envPath = Environment.GetEnvironmentVariable("ISMA_RUNNER_CLI");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var configPath = AppContext.GetData("isma.runner.cli") as string;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return configPath;
        }

        throw new InvalidOperationException(
            "Cannot resolve simulation runner CLI path. Set ISMA_RUNNER_CLI environment variable or 'isma.runner.cli' config key.");
    }

    /// <summary>Lists the integration methods bundled with the runner.</summary>
    public async Task<string[]> ListMethodsAsync(string? scriptPath = null, CancellationToken ct = default)
    {
        var path = scriptPath ?? ResolveScriptPath();

        var psi = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("--list-methods");

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        var exitCode = process.ExitCode;

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Simulation runner exited with code {exitCode}: {stderr.Trim()}");
        }

        return stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Starts a simulation run. The runner writes results to
    /// <paramref name="resultFile"/> and progress (current time, one line,
    /// overwritten per step) to <paramref name="progressFile"/>.
    /// </summary>
    public RunnerProcess Start(RunSimulationParams parameters, string resultFile, string progressFile, string? scriptPath = null)
    {
        var path = scriptPath ?? ResolveScriptPath();

        var psi = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var arg in BuildArguments(parameters, resultFile, progressFile))
        {
            psi.ArgumentList.Add(arg);
        }

        _logger?.LogDebug("Launching simulation runner: {Path} {Args}", path, string.Join(' ', psi.ArgumentList));

        var process = new Process { StartInfo = psi };
        process.Start();

        return new RunnerProcess(process, resultFile, progressFile, process.StandardError.ReadToEndAsync());
    }

    /// <summary>Builds the runner command-line arguments for a simulation run.</summary>
    internal static IEnumerable<string> BuildArguments(RunSimulationParams p, string resultFile, string progressFile)
    {
        yield return "--model";
        yield return p.CompiledModelId;
        yield return "--out";
        yield return resultFile;
        yield return "--progress";
        yield return progressFile;
        yield return "--start";
        yield return FormatNumber(p.StartTime);
        yield return "--end";
        yield return FormatNumber(p.EndTime);
        yield return "--step";
        yield return FormatNumber(p.InitialStep);
        yield return "--method";
        yield return p.MethodName;
        if (p.IsAccuracyInUse)
        {
            yield return "--accuracy";
            yield return FormatNumber(p.Accuracy);
            yield return "--accuracy-enabled";
        }
        if (p.IsStabilityControlInUse)
        {
            yield return "--stability-enabled";
        }
        if (p.IsEventDetectionInUse)
        {
            yield return "--gamma";
            yield return FormatNumber(p.EventDetectionGamma);
            yield return "--low-border";
            yield return FormatNumber(p.EventDetectionLowBorder);
        }
    }

    internal static string FormatNumber(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// A running simulation runner process with its output files.
/// </summary>
public sealed class RunnerProcess
{
    private readonly Task<string> _stderrTask;

    public Process Process { get; }

    public string ResultFile { get; }

    public string ProgressFile { get; }

    public Task ExitTask { get; }

    internal RunnerProcess(Process process, string resultFile, string progressFile, Task<string> stderrTask)
    {
        Process = process;
        ResultFile = resultFile;
        ProgressFile = progressFile;
        _stderrTask = stderrTask;
        ExitTask = process.WaitForExitAsync();
    }

    /// <summary>Waits for the process to exit and returns the exit code and accumulated stderr.</summary>
    public async Task<(int ExitCode, string Stderr)> WaitAsync(CancellationToken ct = default)
    {
        await ExitTask.WaitAsync(ct).ConfigureAwait(false);
        var stderr = await _stderrTask.ConfigureAwait(false);
        return (Process.ExitCode, stderr.Trim());
    }
}
