using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ISMA.Infrastructure.ChartViewer;

public sealed class GrinProcessLauncher : IDisposable
{
    private Process? _process;
    private readonly ILogger<GrinProcessLauncher>? _logger;

    public GrinProcessLauncher(ILogger<GrinProcessLauncher>? logger = null)
    {
        _logger = logger;
    }

    public string ResolveScriptPath()
    {
        var envPath = Environment.GetEnvironmentVariable("ISMA_GRIN_SCRIPT");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var configPath = AppContext.GetData("isma.grin.script") as string;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return configPath;
        }

        throw new InvalidOperationException(
            "Cannot resolve Grin script path. Set ISMA_GRIN_SCRIPT environment variable or 'isma.grin.script' config key.");
    }

    public Task RunAsync(string resultFile, string xAxisColumn, IEnumerable<string> chartColumns, string? scriptPath = null, CancellationToken ct = default)
    {
        var path = scriptPath ?? ResolveScriptPath();
        var args = BuildArguments(resultFile, xAxisColumn, chartColumns);

        var psi = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _process = new Process { StartInfo = psi };

        var outputTask = Task.Run(async () =>
        {
            using var reader = _process!.StandardOutput;
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                _logger?.LogInformation("[Grin] {Line}", line);
            }
        });

        var errorTask = Task.Run(async () =>
        {
            using var reader = _process!.StandardError;
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                _logger?.LogError("[Grin] {Line}", line);
            }
        });

        _process.Start();

        var exitTask = _process.WaitForExitAsync(ct);

        _ = Task.WhenAll(exitTask, outputTask, errorTask).ContinueWith(_ =>
        {
            if (_process != null && _process.HasExited)
            {
                _logger?.LogInformation("Grin process exited with code {Code}", _process.ExitCode);
            }
        });

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill();
                }
                catch { }
            }
        };

        return exitTask;
    }

    public void Run(string resultFile, string xAxisColumn, IEnumerable<string> chartColumns, string? scriptPath = null)
    {
        RunAsync(resultFile, xAxisColumn, chartColumns, scriptPath).Wait();
    }

    public void Dispose()
    {
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill();
            }
            catch { }
            _process.Dispose();
        }
        _process = null;
    }

    private static string BuildArguments(string resultFile, string xAxisColumn, IEnumerable<string> chartColumns)
    {
        var parts = new List<string>();
        parts.Add("--result-file");
        parts.Add($"\"{resultFile}\"");
        parts.Add("--x-axis");
        parts.Add(xAxisColumn);
        parts.Add("--charts");
        parts.Add(string.Join(",", chartColumns));
        return string.Join(" ", parts);
    }
}
