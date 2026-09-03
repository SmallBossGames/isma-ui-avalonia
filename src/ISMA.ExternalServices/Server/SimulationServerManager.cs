using System.Diagnostics;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Server;

public sealed class SimulationServerManager : IDisposable
{
    private Process? _process;
    private readonly ILogger<SimulationServerManager>? _logger;
    private readonly object _exitLock = new();
    private bool _exitHookRegistered;

    public SimulationServerManager(ILogger<SimulationServerManager>? logger = null)
    {
        _logger = logger;
    }

    public SocketPaths? SocketPaths { get; private set; }

    public bool IsRunning => _process != null && !_process.HasExited;

    public string ResolveScriptPath()
    {
        var envPath = Environment.GetEnvironmentVariable("ISMA_SERVER_SCRIPT");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var configPath = AppContext.GetData("isma.server.script") as string;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return configPath;
        }

        throw new InvalidOperationException(
            "Cannot resolve ISMA server script path. Set ISMA_SERVER_SCRIPT environment variable or 'isma.server.script' config key.");
    }

    public SocketPaths Start(string? scriptPath = null)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        var path = scriptPath ?? ResolveScriptPath();
        var psi = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _process = new Process { StartInfo = psi };
        _process.Start();

        var tcs = new TaskCompletionSource<SocketPaths>();

        _ = Task.Run(async () =>
        {
            using var reader = _process!.StandardOutput;
            string? line;
            string? grpc = null;
            string? http = null;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (IsSkipLine(line))
                {
                    continue;
                }

                var (g, h) = ExtractSockets(line);
                if (g is not null)
                {
                    grpc = g;
                }
                if (h is not null)
                {
                    http = h;
                }

                if (grpc is not null && http is not null)
                {
                    SocketPaths = new SocketPaths(grpc, http);
                    tcs.TrySetResult(SocketPaths);
                    return;
                }
            }

            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetException(new TimeoutException("Server did not produce valid socket paths in stdout."));
            }
        });

        var result = tcs.Task.Wait(TimeSpan.FromSeconds(30)) ? SocketPaths! : throw new TimeoutException("Timed out waiting for server to start.");

        lock (_exitLock)
        {
            if (!_exitHookRegistered)
            {
                _exitHookRegistered = true;
                AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
            }
        }
        _process.Exited += (s, e) =>
        {
            _logger?.LogInformation("ISMA server process exited with code {Code}", _process?.ExitCode);
        };

        return result;
    }

    public void Shutdown()
    {
        if (_process == null || _process.HasExited)
        {
            return;
        }

        try
        {
            _process.Kill();
            _process.WaitForExit(5000);
            if (!_process.HasExited)
            {
                _process.Kill(true);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping ISMA server");
        }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        Shutdown();
    }

    private static bool IsSkipLine(string line)
    {
        if (line.StartsWith("WARNING:", StringComparison.Ordinal)) return true;
        if (line.StartsWith("SLF4J:", StringComparison.Ordinal)) return true;
        if (string.IsNullOrWhiteSpace(line)) return true;
        if (line.StartsWith("20", StringComparison.Ordinal) && line.IndexOf(':', 2) > 2) return true;
        return false;
    }

    /// <summary>
    /// Extracts gRPC/HTTP unix socket paths from a server stdout line.
    /// Supports both the modern <c>GRPC_SOCKET=</c> line and the legacy
    /// <c>Starting gRPC server on Unix socket: &lt;path&gt;</c> line.
    /// </summary>
    private static (string? Grpc, string? Http) ExtractSockets(string line)
    {
        string? grpc = null;
        string? http = null;

        const string grpcPrefix = "GRPC_SOCKET=";
        var grpcIdx = line.IndexOf(grpcPrefix, StringComparison.Ordinal);
        if (grpcIdx >= 0)
        {
            grpc = line[(grpcIdx + grpcPrefix.Length)..];
        }
        else
        {
            const string legacyPrefix = "Starting gRPC server on Unix socket:";
            if (line.StartsWith(legacyPrefix, StringComparison.Ordinal))
            {
                grpc = line[legacyPrefix.Length..].Trim();
            }
        }

        const string httpPrefix = "HTTP_SOCKET=";
        var httpIdx = line.IndexOf(httpPrefix, StringComparison.Ordinal);
        if (httpIdx >= 0)
        {
            http = line[(httpIdx + httpPrefix.Length)..];
        }

        return (grpc, http);
    }
}
