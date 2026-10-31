using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// Launches the standalone LISMA compiler CLI (<c>lisma-compiler-cli</c>).
/// Contract: LISMA source on stdin. Compile mode — exit 0 puts the compiled
/// <c>.class</c> bytes on stdout, exit 1 puts <c>row:col:message</c> error
/// lines on stdout. Validate mode (<c>--validate</c>) — exit 0 is clean,
/// exit 1 puts error lines on stdout.
/// </summary>
public sealed class LismaCompilerCli
{
    private readonly ILogger<LismaCompilerCli>? _logger;

    public LismaCompilerCli(ILogger<LismaCompilerCli>? logger = null)
    {
        _logger = logger;
    }

    public string ResolveScriptPath()
    {
        var envPath = Environment.GetEnvironmentVariable("ISMA_COMPILER_CLI");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var configPath = AppContext.GetData("isma.compiler.cli") as string;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return configPath;
        }

        throw new InvalidOperationException(
            "Cannot resolve LISMA compiler CLI path. Set ISMA_COMPILER_CLI environment variable or 'isma.compiler.cli' config key.");
    }

    /// <summary>
    /// Compiles LISMA source and writes the compiled <c>.class</c> bytes to
    /// <paramref name="classFile"/>. Returns diagnostic errors (empty on success).
    /// </summary>
    public async Task<ImmutableArray<CompilationError>> CompileAsync(
        string source, string classFile, string? scriptPath = null, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunAsync(source, Array.Empty<string>(), scriptPath, ct).ConfigureAwait(false);

        if (exitCode == 0)
        {
            await File.WriteAllBytesAsync(classFile, stdout, ct).ConfigureAwait(false);
            return ImmutableArray<CompilationError>.Empty;
        }

        if (exitCode == 1)
        {
            return ParseErrorLines(stdout);
        }

        throw new InvalidOperationException($"LISMA compiler exited with code {exitCode}: {stderr}");
    }

    /// <summary>Validates LISMA source without producing a compiled model.</summary>
    public async Task<ImmutableArray<CompilationError>> ValidateAsync(
        string source, string? scriptPath = null, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunAsync(source, new[] { "--validate" }, scriptPath, ct).ConfigureAwait(false);

        if (exitCode == 0)
        {
            return ImmutableArray<CompilationError>.Empty;
        }

        if (exitCode == 1)
        {
            return ParseErrorLines(stdout);
        }

        throw new InvalidOperationException($"LISMA compiler exited with code {exitCode}: {stderr}");
    }

    private async Task<(int ExitCode, byte[] Stdout, string Stderr)> RunAsync(
        string source, string[] args, string? scriptPath, CancellationToken ct)
    {
        var path = scriptPath ?? ResolveScriptPath();

        var psi = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        _logger?.LogDebug("Launching LISMA compiler: {Path} {Args}", path, string.Join(' ', args));

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutBuffer = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdoutBuffer, ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.StandardInput.WriteAsync(source.AsMemory(), ct).ConfigureAwait(false);
        process.StandardInput.Close();

        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        await stdoutTask.ConfigureAwait(false);
        var stderr = (await stderrTask.ConfigureAwait(false)).Trim();

        _logger?.LogDebug("LISMA compiler exited with code {ExitCode}", process.ExitCode);
        return (process.ExitCode, stdoutBuffer.ToArray(), stderr);
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
