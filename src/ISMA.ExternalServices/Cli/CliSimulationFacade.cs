using System.Collections.Immutable;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// <see cref="ISimulationServerFacade"/> implementation that drives the standalone
/// LISMA compiler CLI and simulation runner CLI directly, without the isma-daemon.
/// Model IDs are local paths of compiled <c>.class</c> files; simulation IDs are local.
/// </summary>
public sealed class CliSimulationFacade : ISimulationServerFacade, IDisposable
{
    private static readonly TimeSpan MinProgressInterval = TimeSpan.FromMilliseconds(100);
    private const double MinProgressDelta = 0.005;

    private readonly ILismaCompiler _compiler;
    private readonly ISimulationRunner _runner;
    private readonly ILogger<CliSimulationFacade>? _logger;
    private readonly object _gate = new();
    private readonly Dictionary<long, SimulationEntry> _simulations = new();
    private readonly List<string> _modelFiles = new();
    private long _nextSimulationId = 1;

    private sealed class SimulationEntry
    {
        public required IActiveSimulation Simulation { get; init; }
        public required double StartTime { get; init; }
        public required double EndTime { get; init; }
    }

    public CliSimulationFacade(ILismaCompiler compiler, ISimulationRunner runner, ILogger<CliSimulationFacade>? logger = null)
    {
        _compiler = compiler;
        _runner = runner;
        _logger = logger;
    }

    public async Task<CompileResult> CompileModel(string source)
    {
        var outcome = await _compiler.CompileAsync(source).ConfigureAwait(false);

        if (!outcome.Succeeded)
        {
            return new CompileResult { ModelId = "", Errors = outcome.Errors, Warnings = ImmutableArray<string>.Empty };
        }

        var classFile = Path.Combine(CliWorkDir.Root, $"isma-model-{Guid.NewGuid():N}.class");
        await File.WriteAllBytesAsync(classFile, outcome.CompiledModel).ConfigureAwait(false);

        lock (_gate)
        {
            _modelFiles.Add(classFile);
        }

        return new CompileResult { ModelId = classFile, Errors = outcome.Errors, Warnings = ImmutableArray<string>.Empty };
    }

    public async Task<ValidationResult> ValidateModel(string source)
    {
        var errors = await _compiler.ValidateAsync(source).ConfigureAwait(false);
        return new ValidationResult { Errors = errors, Warnings = ImmutableArray<string>.Empty };
    }

    public async Task<long> RunSimulation(RunSimulationParams @params)
    {
        var simulation = await _runner.StartAsync(@params).ConfigureAwait(false);

        long id;
        lock (_gate)
        {
            id = _nextSimulationId++;
            _simulations[id] = new SimulationEntry
            {
                Simulation = simulation,
                StartTime = @params.StartTime,
                EndTime = @params.EndTime,
            };
        }

        _logger?.LogDebug("Started simulation {SimulationId}", id);
        return id;
    }

    /// <summary>
    /// Streams simulation progress, coalesced to at most one update per
    /// <see cref="MinProgressInterval"/> or <see cref="MinProgressDelta"/> of the time span —
    /// the runner emits one row per integration step, and yielding every row would
    /// flood the UI dispatcher. The final time is always yielded. The coalescing loop
    /// runs off the caller's synchronization context.
    /// </summary>
    public async IAsyncEnumerable<SimulationProgress> MonitorSimulation(long id)
    {
        var entry = GetEntry(id);
        var span = entry.EndTime - entry.StartTime;
        if (span <= 0)
        {
            yield break;
        }

        double? lastYieldedTime = null;
        double? lastYieldedNormalized = null;
        var lastYieldedAt = DateTime.MinValue;
        var pending = 0.0;
        var hasPending = false;

        var enumerator = entry.Simulation.Times.GetAsyncEnumerator();
        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                pending = enumerator.Current;
                hasPending = true;
                var normalized = (pending - entry.StartTime) / span;
                var now = DateTime.UtcNow;
                var due = lastYieldedNormalized is null
                          || Math.Abs(normalized - lastYieldedNormalized.Value) >= MinProgressDelta
                          || (now - lastYieldedAt) >= MinProgressInterval;
                if (!due)
                {
                    continue;
                }

                lastYieldedTime = pending;
                lastYieldedNormalized = normalized;
                lastYieldedAt = now;
                yield return new SimulationProgress
                {
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    CurrentTime = pending,
                };
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        if (hasPending && lastYieldedTime != pending)
        {
            // the final step was coalesced away — report it so progress reaches the end
            yield return new SimulationProgress
            {
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
                CurrentTime = pending,
            };
        }
    }

    public async Task<CachedSimulationResult> DownloadResult(long id)
    {
        var entry = GetEntry(id);
        var result = await entry.Simulation.GetResultAsync().ConfigureAwait(false);

        return new CachedSimulationResult
        {
            File = result.File,
            ColumnNames = result.ColumnNames,
        };
    }

    public Task CancelSimulation(long id) => GetEntry(id).Simulation.CancelAsync();

    public Task<IReadOnlyList<string>> GetSimulationMethods() => _runner.GetMethodsAsync();

    public async Task Shutdown()
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
            await entry.Simulation.DisposeAsync().ConfigureAwait(false);
        }

        foreach (var file in modelFiles)
        {
            TryDelete(file);
        }

        _logger?.LogInformation("Shut down CLI simulation facade");
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
}
