namespace ISMA.ExternalServices.Cli;

/// <summary>
/// The shared work directory for CLI tool artifacts: compiled <c>.class</c> models
/// and simulation result files. Lives in the system temp directory.
/// </summary>
internal static class CliWorkDir
{
    public static string Root
    {
        get
        {
            var dir = Path.Combine(Path.GetTempPath(), "isma-ui");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
