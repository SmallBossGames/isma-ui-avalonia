using System.Diagnostics;
using System.Globalization;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// A launched CLI tool process: async access to its stdin/stdout/stderr, its exit code,
/// and graceful termination (SIGTERM on Unix, hard kill as a fallback).
/// </summary>
public sealed class CliProcess(Process process) : IAsyncDisposable
{
    private static readonly TimeSpan GracefulExitTimeout = TimeSpan.FromSeconds(2);

    private readonly Task<string> _stderr = process.StandardError.ReadToEndAsync();
    private readonly Task _exitTask = process.WaitForExitAsync();

    public int Id => process.Id;

    /// <summary>The process stdin. Write the input, then close it.</summary>
    public Stream Stdin => process.StandardInput.BaseStream;

    /// <summary>The process stdout — the tool's output stream.</summary>
    public Stream Stdout => process.StandardOutput.BaseStream;

    /// <summary>The accumulated stderr; completes when the process exits.</summary>
    public Task<string> Stderr => _stderr;

    public bool HasExited => process.HasExited;

    /// <summary>Waits for the process to exit and returns its exit code.</summary>
    public async Task<int> WaitExitCodeAsync(CancellationToken cancellationToken = default)
    {
        await _exitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    /// <summary>
    /// Terminates the process: SIGTERM on Unix (so the tool can run its shutdown hooks),
    /// a hard kill if it does not exit in time; a hard kill on Windows.
    /// </summary>
    public async Task TerminateAsync()
    {
        if (process.HasExited)
        {
            return;
        }

        if (!OperatingSystem.IsWindows() && await TrySendSigTermAsync().ConfigureAwait(false))
        {
            using var timeout = new CancellationTokenSource(GracefulExitTimeout);
            try
            {
                await _exitTask.WaitAsync(timeout.Token).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                // the process ignored SIGTERM — fall through to a hard kill
            }
        }

        Kill();
    }

    /// <summary>
    /// Terminates the process if it is still running. The <see cref="Process"/> object itself
    /// is not disposed: in-flight awaits on the process (e.g. a concurrent
    /// <see cref="WaitExitCodeAsync"/>) must stay valid while the application shuts down.
    /// </summary>
    public async ValueTask DisposeAsync() => await TerminateAsync().ConfigureAwait(false);

    private async Task<bool> TrySendSigTermAsync()
    {
        try
        {
            var kill = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "kill",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            kill.StartInfo.ArgumentList.Add("-TERM");
            kill.StartInfo.ArgumentList.Add(Id.ToString(CultureInfo.InvariantCulture));
            kill.Start();
            await kill.WaitForExitAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Kill()
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // the process is already gone
        }
    }
}
