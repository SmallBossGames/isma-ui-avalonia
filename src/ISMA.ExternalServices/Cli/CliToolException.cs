namespace ISMA.ExternalServices.Cli;

/// <summary>
/// A CLI tool failed outside its documented contract (an unexpected exit code,
/// a launch failure). Carries the tool's stderr for diagnostics.
/// </summary>
public sealed class CliToolException : InvalidOperationException
{
    public CliToolException(string toolName, int exitCode, string stderr)
        : base($"{toolName} exited with code {exitCode}: {stderr}")
    {
    }
}
