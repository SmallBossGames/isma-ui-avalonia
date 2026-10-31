using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// <see cref="ISimulationServerFacade"/> implementation that drives the
/// standalone LISMA compiler CLI and simulation runner CLI directly, without
/// the isma-daemon. Model IDs are local paths of compiled <c>.class</c> files;
/// simulation IDs are local.
/// </summary>
public sealed class CliSimulationFacade : ISimulationServerFacade, IDisposable
{
    private static readonly TimeSpan ProgressPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly LismaCompilerCli _compiler;
    private readonly SimulationRunnerCli _runner;
    private readonly ILogger? _logger;
    private readonly object _gate = new();
    private readonly Dictionary<long, SimulationEntry> _simulations = new();
    private readonly List<string> _modelFiles = new();
    private long _nextSimulationId = 1;

    private sealed class SimulationEntry
    {
        public required RunnerProcess Runner { get; init; }
        public required double StartTime { get; init; }
        public required double EndTime { get; init; }
    }

    public CliSimulationFacade(LismaCompilerCli compiler, SimulationRunnerCli runner, ILogger? logger = null)
    {
        _compiler = compiler;
        _runner = runner;
        _logger = logger;
    }

    private static string WorkDir
    {
        get
        {
            var dir = Path.Combine(Path.GetTempPath(), "isma-ui");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public async Task<CompileResult> CompileModel(string source)
    {
        var classFile = Path.Combine(WorkDir, $"isma-model-{Guid.NewGuid():N}.class");
        var errors = await _compiler.CompileAsync(source, classFile).ConfigureAwait(false);

        if (errors.IsDefaultOrEmpty)
        {
            lock (_gate)
            {
                _modelFiles.Add(classFile);
            }

            return new CompileResult { ModelId = classFile, Errors = errors, Warnings = ImmutableArray<string>.Empty };
        }

        return new CompileResult { ModelId = "", Errors = errors, Warnings = ImmutableArray<string>.Empty };
    }

    public async Task<ValidationResult> ValidateModel(string source)
    {
        var errors = await _compiler.ValidateAsync(source).ConfigureAwait(false);
        return new ValidationResult { Errors = errors, Warnings = ImmutableArray<string>.Empty };
    }

    public Task<long> RunSimulation(RunSimulationParams @params)
    {
        lock (_gate)
        {
            var id = _nextSimulationId++;
            var resultFile = Path.Combine(WorkDir, $"isma-simulation-{id}.bin");
            var progressFile = Path.Combine(WorkDir, $"isma-progress-{id}.txt");
            var runner = _runner.Start(@params, resultFile, progressFile);
            _simulations[id] = new SimulationEntry
            {
                Runner = runner,
                StartTime = @params.StartTime,
                EndTime = @params.EndTime,
            };
            return Task.FromResult(id);
        }
    }

    public async IAsyncEnumerable<SimulationProgress> MonitorSimulation(long id)
    {
        var entry = GetEntry(id);

        while (true)
        {
            var exited = false;
            try
            {
                await entry.Runner.ExitTask.WaitAsync(ProgressPollInterval).ConfigureAwait(false);
                exited = true;
            }
            catch (TimeoutException)
            {
                // process still running — keep polling
            }

            var current = ReadProgress(entry.Runner.ProgressFile);
            if (current is not null)
            {
                yield return new SimulationProgress
                {
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    CurrentTime = current.Value,
                };
            }

            if (exited)
            {
                yield break;
            }
        }
    }

    public async Task<CachedSimulationResult> DownloadResult(long id)
    {
        var entry = GetEntry(id);

        var (exitCode, stderr) = await entry.Runner.WaitAsync().ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Simulation runner failed (exit code {exitCode}): {Truncate(stderr)}");
        }

        if (!File.Exists(entry.Runner.ResultFile))
        {
            throw new InvalidOperationException($"Simulation result file not found: {entry.Runner.ResultFile}");
        }

        ImmutableArray<string> columnNames;
        try
        {
            columnNames = BinaryFilePointProvider.ReadMetadata(entry.Runner.ResultFile).ColumnNames;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read simulation result metadata from {File}", entry.Runner.ResultFile);
            columnNames = ImmutableArray<string>.Empty;
        }

        return new CachedSimulationResult
        {
            File = entry.Runner.ResultFile,
            ColumnNames = columnNames,
        };
    }

    public Task CancelSimulation(long id)
    {
        var entry = GetEntry(id);

        if (!entry.Runner.Process.HasExited)
        {
            Terminate(entry.Runner.Process);
        }

        return Task.CompletedTask;
    }

    public Task<string[]> GetSimulationMethods()
    {
        return _runner.ListMethodsAsync();
    }

    public Task Shutdown()
    {
        List<SimulationEntry> entries;
        List<string> modelFiles;
        lock (_gate)
        {
            entries = _simulations.Values.ToList();
            _simulations.Clear();
            modelFiles = _modelFiles.ToList();
            _modelFiles.Clear();
        }

        foreach (var entry in entries)
        {
            if (!entry.Runner.Process.HasExited)
            {
                Terminate(entry.Runner.Process);
            }
        }

        foreach (var file in modelFiles)
        {
            TryDelete(file);
        }

        _logger?.LogInformation("Shut down CLI simulation facade");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _ = Shutdown();
    }

    private SimulationEntry GetEntry(long id)
    {
        lock (_gate)
        {
            if (!_simulations.TryGetValue(id, out var entry))
            {
                throw new InvalidOperationException($"Unknown simulation {id}");
            }

            return entry;
        }
    }

    private static double? ReadProgress(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var line = File.ReadAllText(path).Trim();
            if (line.Length == 0)
            {
                return null;
            }

            return double.Parse(line, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            // partially written file or transient IO error — retry on next poll
            return null;
        }
    }

    /// <summary>
    /// Sends SIGTERM on Unix (the runner's shutdown hook cancels the run);
    /// falls back to a hard kill if the signal cannot be delivered.
    /// </summary>
    private static void Terminate(Process process)
    {
        if (OperatingSystem.IsWindows())
        {
            process.Kill();
            return;
        }

        try
        {
            var killInfo = new ProcessStartInfo
            {
                FileName = "kill",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            killInfo.ArgumentList.Add("-TERM");
            killInfo.ArgumentList.Add(process.Id.ToString(CultureInfo.InvariantCulture));
            using var kill = new Process { StartInfo = killInfo };
            kill.Start();
            kill.WaitForExit(2000);
        }
        catch (Exception)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // process already gone
            }
        }
    }

    private static string Truncate(string text)
    {
        return text.Length <= 500 ? text : text[..500] + "…";
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
        catch (Exception)
        {
            // best effort
        }
    }
}
