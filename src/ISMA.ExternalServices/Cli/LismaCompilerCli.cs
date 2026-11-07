using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// <see cref="ILismaCompiler"/> adapter for the standalone LISMA compiler CLI.
/// Protocol: LISMA source on stdin. Compile mode — exit 0 puts the compiled
/// <c>.class</c> bytes on stdout, exit 1 puts <c>row:col:message</c> error
/// lines on stdout. Validate mode (<c>--validate</c>) — exit 0 is clean,
/// exit 1 puts error lines on stdout. Any other exit code is a tool failure.
/// </summary>
public sealed class LismaCompilerCli(CliLauncher launcher, ILogger<LismaCompilerCli>? logger = null) : ILismaCompiler
{
    private const string ToolName = "lisma-compiler-cli";

    public async Task<CompilationOutcome> CompileAsync(string source, CancellationToken cancellationToken = default)
    {
        logger?.LogDebug("Launching LISMA compiler: compile");
        await using var process = await launcher.StartAsync([], cancellationToken).ConfigureAwait(false);
        var (exitCode, stdout) = await RunWithSourceAsync(process, source, cancellationToken).ConfigureAwait(false);

        return exitCode switch
        {
            0 => CompilationOutcome.Success(stdout),
            1 => CompilationOutcome.Failed(ParseErrorLines(stdout)),
            _ => throw await ToolFailureAsync(process, exitCode).ConfigureAwait(false),
        };
    }

    public async Task<ImmutableArray<CompilationError>> ValidateAsync(string source, CancellationToken cancellationToken = default)
    {
        logger?.LogDebug("Launching LISMA compiler: --validate");
        await using var process = await launcher.StartAsync(["--validate"], cancellationToken).ConfigureAwait(false);
        var (exitCode, stdout) = await RunWithSourceAsync(process, source, cancellationToken).ConfigureAwait(false);

        return exitCode switch
        {
            0 => ImmutableArray<CompilationError>.Empty,
            1 => ParseErrorLines(stdout),
            _ => throw await ToolFailureAsync(process, exitCode).ConfigureAwait(false),
        };
    }

    private static async Task<(int ExitCode, byte[] Stdout)> RunWithSourceAsync(
        CliProcess process, string source, CancellationToken cancellationToken)
    {
        // start draining stdout before writing stdin so a chatty tool cannot deadlock the pipe
        var stdoutTask = CliStreams.ReadAllAsync(process.Stdout, cancellationToken);
        await CliStreams.WriteAllAsync(process.Stdin, Encoding.UTF8.GetBytes(source), cancellationToken).ConfigureAwait(false);
        process.Stdin.Close();

        var stdout = await stdoutTask.ConfigureAwait(false);
        var exitCode = await process.WaitExitCodeAsync(cancellationToken).ConfigureAwait(false);
        return (exitCode, stdout);
    }

    private static async Task<CliToolException> ToolFailureAsync(CliProcess process, int exitCode)
    {
        var stderr = (await process.Stderr.ConfigureAwait(false)).Trim();
        return new CliToolException(ToolName, exitCode, stderr);
    }

    private static ImmutableArray<CompilationError> ParseErrorLines(byte[] stdout)
    {
        var text = Encoding.UTF8.GetString(stdout);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var errors = new List<CompilationError>(lines.Length);
        foreach (var line in lines)
        {
            errors.Add(ParseErrorLine(line));
        }

        return errors.ToImmutableArray();
    }

    /// <summary>
    /// Parses a <c>&lt;row&gt;:&lt;col&gt;:&lt;message&gt;</c> diagnostic line.
    /// Row/col of -1 mean the error has no source position.
    /// </summary>
    internal static CompilationError ParseErrorLine(string line)
    {
        var first = line.IndexOf(':');
        if (first <= 0)
        {
            return new CompilationError { Row = -1, Column = -1, Message = line };
        }

        var rest = line[(first + 1)..];
        var second = rest.IndexOf(':');
        if (second <= 0)
        {
            return new CompilationError { Row = ParsePosition(line[..first]), Column = -1, Message = rest };
        }

        return new CompilationError
        {
            Row = ParsePosition(line[..first]),
            Column = ParsePosition(rest[..second]),
            Message = rest[(second + 1)..],
        };
    }

    private static int ParsePosition(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : -1;
    }
}
