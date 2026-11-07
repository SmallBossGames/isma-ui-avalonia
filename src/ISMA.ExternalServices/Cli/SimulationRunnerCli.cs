using System.Globalization;
using System.Text;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// <see cref="ISimulationRunner"/> adapter for the standalone simulation runner CLI.
/// Protocol: <c>--model &lt;class&gt; --start --end --step --method</c> plus optional
/// accuracy/stability/event-detection flags. The runner streams the result to
/// stdout in the ISMA exchange binary format, one row per integration step;
/// exit 0 is success, exit 1 is failure (<c>simulation-runner: &lt;message&gt;</c>
/// on stderr). <c>--list-methods</c> prints the bundled integration method names,
/// one per line.
/// </summary>
public sealed class SimulationRunnerCli(CliLauncher launcher, ILogger<SimulationRunnerCli>? logger = null) : ISimulationRunner
{
    private const string ToolName = "simulation-runner-cli";

    public async Task<IActiveSimulation> StartAsync(RunSimulationParams parameters, CancellationToken cancellationToken = default)
    {
        var arguments = BuildArguments(parameters).ToList();
        logger?.LogDebug("Launching simulation runner: {Arguments}", string.Join(' ', arguments));
        var process = await launcher.StartAsync(arguments, cancellationToken).ConfigureAwait(false);
        return new ActiveSimulation(process);
    }

    public async Task<IReadOnlyList<string>> GetMethodsAsync(CancellationToken cancellationToken = default)
    {
        await using var process = await launcher.StartAsync(["--list-methods"], cancellationToken).ConfigureAwait(false);
        var stdoutTask = CliStreams.ReadAllAsync(process.Stdout, cancellationToken);
        var exitCode = await process.WaitExitCodeAsync(cancellationToken).ConfigureAwait(false);
        var stdout = Encoding.UTF8.GetString(await stdoutTask.ConfigureAwait(false));

        if (exitCode != 0)
        {
            var stderr = (await process.Stderr.ConfigureAwait(false)).Trim();
            throw new CliToolException(ToolName, exitCode, stderr);
        }

        return stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>Builds the runner command-line arguments for a simulation run.</summary>
    internal static IEnumerable<string> BuildArguments(RunSimulationParams p)
    {
        yield return "--model";
        yield return p.CompiledModelId;
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
