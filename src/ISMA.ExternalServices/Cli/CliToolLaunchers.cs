namespace ISMA.ExternalServices.Cli;

/// <summary>
/// The launchers for the standalone CLI tools, configured from the
/// <c>ISMA_COMPILER_CLI</c>/<c>ISMA_RUNNER_CLI</c> environment variables or the
/// <c>isma.compiler.cli</c>/<c>isma.runner.cli</c> appsettings keys.
/// </summary>
public sealed class CliToolLaunchers
{
    public CliToolLaunchers()
    {
        Compiler = new CliLauncher("lisma-compiler-cli", "ISMA_COMPILER_CLI", "isma.compiler.cli");
        Runner = new CliLauncher("simulation-runner-cli", "ISMA_RUNNER_CLI", "isma.runner.cli");
    }

    public CliLauncher Compiler { get; }

    public CliLauncher Runner { get; }
}
