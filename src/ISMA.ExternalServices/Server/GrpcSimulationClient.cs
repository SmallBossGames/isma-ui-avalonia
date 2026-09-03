using Grpc.Net.Client;
using Isma.Contracts.V1.SimulationService;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace ISMA.ExternalServices.Server;

public sealed class GrpcSimulationClient : IDisposable
{
    private readonly SimulationService.SimulationServiceClient _client;
    private readonly ILogger? _logger;
    private readonly IDisposable? _channelDispose;

    public GrpcSimulationClient(GrpcChannel channel, ILogger? logger = null)
    {
        _client = new SimulationService.SimulationServiceClient(channel);
        _logger = logger;
        _channelDispose = channel;
    }

    public GrpcSimulationClient(IUnixSocketHandler socketHandler, string grpcAddress, ILogger? logger = null)
    {
        _client = new SimulationService.SimulationServiceClient(socketHandler.CreateGrpcChannel(grpcAddress, logger));
        _logger = logger;
    }

    public async Task<long> RunSimulationAsync(RunSimulationParams @params, CancellationToken ct = default)
    {
        var request = new RunSimulationRequest
        {
            StartTime = @params.StartTime,
            EndTime = @params.EndTime,
            InitialStep = @params.InitialStep,
            MethodName = @params.MethodName,
            CompiledModelId = @params.CompiledModelId,
        };

        if (@params.IsAccuracyInUse)
        {
            request.AccuracyConfig = new AccuracyConfig { Accuracy = @params.Accuracy };
        }

        if (@params.IsStabilityControlInUse)
        {
            request.StabilityConfig = new StabilityConfig();
        }

        if (@params.IsEventDetectionInUse)
        {
            request.EventDetection = new EventDetectionConfig
            {
                Gamma = @params.EventDetectionGamma,
                LowBorder = @params.EventDetectionLowBorder,
            };
        }

 

        _logger?.LogInformation("Starting simulation: method={Method}, model={Model}", @params.MethodName, @params.CompiledModelId);

        var response = await _client.RunSimulationAsync(request, cancellationToken: ct).ConfigureAwait(false);
        return response.SimulationId;
    }

    public long RunSimulation(RunSimulationParams @params, CancellationToken ct = default)
    {
        return RunSimulationAsync(@params, ct).GetAwaiter().GetResult();
    }

    public async IAsyncEnumerable<SimulationProgress> MonitorSimulationAsync(long simulationId, double accuracy = MonitorIntervalSeconds)
    {
        var request = new MonitorSimulationRequest
        {
            SimulationId = simulationId,
            Accuracy = accuracy,
        };

        var responseStream = _client.MonitorSimulation(request);
        while (await responseStream.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
        {
            var response = responseStream.ResponseStream.Current;
            yield return new SimulationProgress
            {
                StartTime = response.StartTime,
                EndTime = response.EndTime,
                CurrentTime = response.CurrentTime,
            };
        }
    }

    public async Task<CachedSimulationResult> DownloadResultAsync(long simulationId, CancellationToken ct = default)
    {
        var request = new GetSimulationResultRequest { SimulationId = simulationId };
        var response = await _client.GetSimulationResultAsync(request, cancellationToken: ct).ConfigureAwait(false);

        return new CachedSimulationResult
        {
            File = response.DownloadUrl,
            ColumnNames = ImmutableArray<string>.Empty,
        };
    }

    public CachedSimulationResult DownloadResult(long simulationId, CancellationToken ct = default)
    {
        return DownloadResultAsync(simulationId, ct).GetAwaiter().GetResult();
    }

    public async Task CancelSimulationAsync(long simulationId, CancellationToken ct = default)
    {
        var request = new CancelSimulationRequest { SimulationId = simulationId };
        await _client.CancelSimulationAsync(request, cancellationToken: ct).ConfigureAwait(false);
        _logger?.LogInformation("Cancelled simulation {Id}", simulationId);
    }

    public void CancelSimulation(long simulationId, CancellationToken ct = default)
    {
        CancelSimulationAsync(simulationId, ct).GetAwaiter().GetResult();
    }

    public async Task<string[]> GetSimulationMethodsAsync(CancellationToken ct = default)
    {
        var request = new ListSimulationMethodsRequest();
        var response = await _client.ListSimulationMethodsAsync(request, cancellationToken: ct).ConfigureAwait(false);
        return response.Methods.Select(m => m.Name).ToArray();
    }

    public string[] GetSimulationMethods(CancellationToken ct = default)
    {
        return GetSimulationMethodsAsync(ct).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _channelDispose?.Dispose();
    }

    private const double MonitorIntervalSeconds = 0.01;
}
