using System.Diagnostics;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// Resolves the path of one configured CLI tool and launches it.
/// Path resolution order: environment variable, then the <see cref="AppContext"/>
/// config value set by the application from <c>appsettings.json</c>.
/// </summary>
public sealed class CliLauncher(string toolName, string envVarName, string configKey)
{
    /// <summary>
    /// Resolves the tool executable path.
    /// Throws <see cref="InvalidOperationException"/> when the tool is not configured.
    /// </summary>
    public string ResolvePath()
    {
        var envPath = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var configPath = AppContext.GetData(configKey) as string;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return configPath;
        }

        throw new InvalidOperationException(
            $"Cannot resolve {toolName} path. Set the {envVarName} environment variable or the '{configKey}' config key.");
    }

    /// <summary>
    /// Launches the tool with the given arguments. The blocking fork/exec is moved
    /// off the caller's thread, so this is safe to call from the UI thread.
    /// </summary>
    public async Task<CliProcess> StartAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        var process = CreateProcess(ResolvePath(), arguments);
        await Task.Run(() => process.Start(), cancellationToken).ConfigureAwait(false);
        return new CliProcess(process);
    }

    private static Process CreateProcess(string path, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return new Process { StartInfo = startInfo };
    }
}
