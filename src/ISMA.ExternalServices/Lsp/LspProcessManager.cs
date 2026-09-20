using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Lsp;

/// <summary>
/// Manages the LISMA language server process, mirroring
/// <see cref="Server.SimulationServerManager"/> for the gRPC server.
/// The script path is resolved from the <c>ISMA_LSP_SCRIPT</c> environment
/// variable or the <c>isma.lsp.script</c> config key.
/// </summary>
public sealed class LspProcessManager : IDisposable
{
    private const string EnvVar = "ISMA_LSP_SCRIPT";
    private const string ConfigKey = "isma.lsp.script";

    private readonly ILogger<LspProcessManager>? _logger;
    private readonly object _gate = new();
    private ILspTransport? _transport;
    private bool _running;

    public LspProcessManager(ILogger<LspProcessManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Resolves the language server script path from env/config.</summary>
    public string ResolveScriptPath()
    {
        var envPath = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var configPath = AppContext.GetData(ConfigKey) as string;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return configPath;
        }

        throw new InvalidOperationException(
            $"Cannot resolve isma-lsp script path. Set {EnvVar} environment variable or '{ConfigKey}' config key.");
    }

    /// <summary>Starts the language server process if needed and returns its transport.</summary>
    public ILspTransport Start(string? scriptPath = null)
    {
        lock (_gate)
        {
            if (_running && _transport is { } existing)
            {
                return existing;
            }

            var path = scriptPath ?? ResolveScriptPath();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"isma-lsp script not found at: {path}", path);
            }

            var transport = new ProcessLspTransport(path, _logger);
            _transport = transport;
            _running = true;
            _logger?.LogInformation("isma-lsp started from: {Path}", path);
            return transport;
        }
    }

    /// <summary>Stops the language server process and releases the transport.</summary>
    public void Stop()
    {
        ILspTransport? toClose;
        lock (_gate)
        {
            if (!_running)
            {
                return;
            }
            _running = false;
            toClose = _transport;
            _transport = null;
        }
        toClose?.Close();
        _logger?.LogInformation("isma-lsp stopped");
    }

    public void Dispose()
    {
        Stop();
    }
}
