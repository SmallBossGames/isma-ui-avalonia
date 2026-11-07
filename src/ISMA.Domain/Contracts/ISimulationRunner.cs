using ISMA.Domain.Dtos;

namespace ISMA.Domain.Contracts;

/// <summary>
/// Port for a tool that runs simulations of compiled LISMA models.
/// Implementations live in the infrastructure layer (the standalone simulation runner CLI).
/// </summary>
public interface ISimulationRunner
{
    /// <summary>
    /// Starts a simulation. The returned handle streams per-step progress and the final result.
    /// </summary>
    Task<IActiveSimulation> StartAsync(RunSimulationParams parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the integration methods bundled with the runner.
    /// </summary>
    Task<IReadOnlyList<string>> GetMethodsAsync(CancellationToken cancellationToken = default);
}
