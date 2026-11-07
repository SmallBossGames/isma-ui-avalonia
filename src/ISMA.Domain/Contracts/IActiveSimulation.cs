using ISMA.Domain.Dtos;

namespace ISMA.Domain.Contracts;

/// <summary>
/// Handle for a running simulation: streams per-step progress and the final result.
/// </summary>
public interface IActiveSimulation : IAsyncDisposable
{
    /// <summary>
    /// Streams the TIME value of each integration step, in order.
    /// Completes when the run finishes (success, failure, or cancellation).
    /// </summary>
    IAsyncEnumerable<double> Times { get; }

    /// <summary>
    /// Waits for the run to finish and returns the result.
    /// Throws when the run failed or was cancelled.
    /// </summary>
    Task<SimulationResult> GetResultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the run: graceful termination of the underlying tool, hard kill as a fallback.
    /// </summary>
    Task CancelAsync();
}
