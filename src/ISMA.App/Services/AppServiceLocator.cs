namespace ISMA.App.Services;

/// <summary>
/// Process-wide access to the application service provider.
/// Set by <see cref="App"/> and by the test application.
/// </summary>
public static class AppServiceLocator
{
    public static IServiceProvider? Services { get; set; }
}
