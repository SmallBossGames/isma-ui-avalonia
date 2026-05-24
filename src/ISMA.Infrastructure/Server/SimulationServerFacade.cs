using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ISMA.Infrastructure.Server;

public sealed class SimulationServerFacade : ISimulationServerFacade, IDisposable
{
    private readonly SimulationServerManager _serverManager;
    private readonly IUnixSocketHandler _socketHandler;
    private readonly ILogger? _logger;

    private GrpcSimulationClient? _simulationClient;
    private GrpcLismaCompilerClient? _compilerClient;
    private HttpSimulationClient? _httpClient;

    public SimulationServerFacade(
        SimulationServerManager serverManager,
        IUnixSocketHandler? socketHandler = null,
        ILogger? logger = null)
    {
        _serverManager = serverManager;
        _socketHandler = socketHandler ?? UnixSocketHandlerFactory.Create();
        _logger = logger;
    }

    public async Task<CompileResult> CompileModel(string source)
    {
        EnsureClients();
        return await _compilerClient!.CompileModelAsync(source).ConfigureAwait(false);
    }

    public async Task<ValidationResult> ValidateModel(string source)
    {
        EnsureClients();
        return await _compilerClient!.ValidateModelAsync(source).ConfigureAwait(false);
    }

    public async Task<SyntaxTokenDto[]> HighlightSource(string source)
    {
        EnsureClients();
        return await _compilerClient!.HighlightSourceAsync(source).ConfigureAwait(false);
    }

    public async Task<long> RunSimulation(RunSimulationParams @params)
    {
        EnsureClients();
        return await _simulationClient!.RunSimulationAsync(@params).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<SimulationProgress> MonitorSimulation(long id)
    {
        EnsureClients();
        await foreach (var progress in _simulationClient!.MonitorSimulationAsync(id).ConfigureAwait(false))
        {
            yield return progress;
        }
    }

    public async Task<CachedSimulationResult> DownloadResult(long id)
    {
        EnsureClients();
        return await _simulationClient!.DownloadResultAsync(id).ConfigureAwait(false);
    }

    public async Task CancelSimulation(long id)
    {
        EnsureClients();
        await _simulationClient!.CancelSimulationAsync(id).ConfigureAwait(false);
    }

    public async Task<string[]> GetSimulationMethods()
    {
        EnsureClients();
        return await _simulationClient!.GetSimulationMethodsAsync().ConfigureAwait(false);
    }

    public async Task Shutdown()
    {
        _logger?.LogInformation("Shutting down simulation server facade");

        _simulationClient?.Dispose();
        _simulationClient = null;

        _compilerClient?.Dispose();
        _compilerClient = null;

        _httpClient?.Dispose();
        _httpClient = null;

        _serverManager.Shutdown();
    }

    public void Dispose()
    {
        _ = Shutdown();
    }

    private void EnsureClients()
    {
        if (_simulationClient != null && _compilerClient != null && _httpClient != null)
        {
            return;
        }

        if (!_serverManager.IsRunning)
        {
            _logger?.LogInformation("Starting ISMA server");
            var paths = _serverManager.Start();
            _logger?.LogInformation("Server started: gRPC={Grpc}, HTTP={Http}", paths.Grpc, paths.Http);
        }

        if (_simulationClient == null)
        {
            _simulationClient = new GrpcSimulationClient(_socketHandler, _serverManager.SocketPaths!.Grpc, _logger);
        }

        if (_compilerClient == null)
        {
            _compilerClient = new GrpcLismaCompilerClient(_socketHandler, _serverManager.SocketPaths!.Grpc, _logger);
        }

        if (_httpClient == null)
        {
            _httpClient = new HttpSimulationClient(_socketHandler, _serverManager.SocketPaths!.Http, _logger);
        }
    }
}
