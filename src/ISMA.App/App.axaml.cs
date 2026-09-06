using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.ExternalServices.ChartViewer;
using ISMA.ExternalServices.FileStorage;
using ISMA.ExternalServices.Server;
using ISMA.App.Services;
using ISMA.App.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ISMA.App;

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ConfigureServerPaths();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_services is null)
            {
                _services = ConfigureServiceCollection().BuildServiceProvider();
            }

            AppServiceLocator.Services = _services;

            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            var preferencesProvider = _services.GetService<IPreferencesProvider>();
            var editorPlatformService = _services.GetService<EditorPlatformService>();
            var mainWindow = preferencesProvider != null
                ? new MainWindow(viewModel, preferencesProvider)
                : new MainWindow(viewModel);

            _services.GetRequiredService<WindowProvider>().Current = mainWindow;

            if (editorPlatformService != null)
            {
                editorPlatformService.AttachToWindow(mainWindow);
            }

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServerPaths()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(App).Assembly.Location) ?? Directory.GetCurrentDirectory();

        var config = new ConfigurationBuilder()
            .SetBasePath(assemblyDir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .Build();

        var serverPath = config["Server:ScriptPath"];
        if (!string.IsNullOrWhiteSpace(serverPath))
        {
            AppContext.SetData("isma.server.script", serverPath);
        }

        var grinPath = config["Grin:ScriptPath"];
        if (!string.IsNullOrWhiteSpace(grinPath))
        {
            AppContext.SetData("isma.grin.script", grinPath);
        }
    }

    private static IServiceCollection ConfigureServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        services.AddSingleton<ISimulationServerFacade>(sp =>
        {
            var manager = sp.GetRequiredService<SimulationServerManager>();
            var socketHandler = UnixSocketHandlerFactory.Create();
            var logger = sp.GetService<ILogger<SimulationServerFacade>>();
            return new SimulationServerFacade(manager, socketHandler, logger);
        });

        services.ConfigureAppServices();

        return services;
    }
}
