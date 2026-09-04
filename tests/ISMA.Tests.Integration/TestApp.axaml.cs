using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ISMA.App;
using ISMA.App.Services;
using ISMA.App.ViewModels;
using ISMA.ExternalServices.ChartViewer;
using ISMA.ExternalServices.FileStorage;
using Microsoft.Extensions.Logging;

namespace ISMA.Tests.Integration;

public partial class TestApp : Application
{
    private IServiceProvider _services = null!;
    private MainWindow _window = null!;

    public TestApp()
    {
        _services = ConfigureServiceCollection().BuildServiceProvider();
        AppServiceLocator.Services = _services;
    }

    public MainWindow Window => _window;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var viewModel = _services.GetRequiredService<MainWindowViewModel>();
        var preferencesProvider = _services.GetService<IPreferencesProvider>();
        var editorPlatformService = _services.GetRequiredService<EditorPlatformService>();

        var mainWindow = preferencesProvider != null
            ? new MainWindow(viewModel, preferencesProvider)
            : new MainWindow(viewModel);

        editorPlatformService.AttachToWindow(mainWindow);
        mainWindow.Width = 1024;
        mainWindow.Height = 768;
        mainWindow.Show();

        _window = mainWindow;

        base.OnFrameworkInitializationCompleted();
    }

    public IServiceProvider Services => _services;

    public MockSimulationServerFacade MockServer => _services!.GetRequiredService<MockSimulationServerFacade>();

    public MainWindowViewModel ViewModel => _services!.GetRequiredService<MainWindowViewModel>();

    public T GetRequiredService<T>() where T : class
    {
        return _services.GetRequiredService<T>();
    }

    private static IServiceCollection ConfigureServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        services.AddSingleton<MockSimulationServerFacade>();
        services.AddSingleton<ISimulationServerFacade>(x => x.GetRequiredService<MockSimulationServerFacade>());
        services.AddSingleton<GrinProcessLauncher>();

        services.ConfigureAppServices();

        // Isolate preferences in a temp file so tests never read or write the
        // user's real settings (last-opened files would be auto-restored).
        var settingsPath = Path.Combine(Path.GetTempPath(), "isma-tests", "preferences.json");
        services.AddSingleton<PreferencesProvider>(_ => new PreferencesProvider(settingsPath));
        services.AddSingleton<IPreferencesProvider>(_ => _.GetRequiredService<PreferencesProvider>());

        return services;
    }
}

public static class TestAppExtensions
{
    public static T? FindControl<T>(this TestApp app, string name) where T : Control
    {
        return app.Window.FindControl<T>(name);
    }
}
