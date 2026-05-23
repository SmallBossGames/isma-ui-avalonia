using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ISMA.App.Services;
using ISMA.Infrastructure.ChartViewer;
using ISMA.Infrastructure.FileStorage;
using ISMA.Infrastructure.Server;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_services is null)
            {
                _services = ConfigureServiceCollection().BuildServiceProvider();
            }

            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow(viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    internal static IServiceCollection ConfigureServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddSingleton<GrinProcessLauncher>();
        services.AddSingleton<PreferencesProvider>();
        services.AddSingleton<SimulationServerManager>();
        services.AddSingleton<SimulationServerFacade>(sp =>
        {
            var manager = sp.GetRequiredService<SimulationServerManager>();
            var socketHandler = UnixSocketHandlerFactory.Create();
            var logger = sp.GetService<ILogger<SimulationServerFacade>>();
            return new SimulationServerFacade(manager, socketHandler, logger);
        });

        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<ITextEditorFactory, TextEditorFactory>();
        services.AddSingleton<ISimulationResultService, SimulationResultService>();
        services.AddSingleton<SyntaxHighlighterService>();
        services.AddSingleton<IModelErrorService, ModelErrorService>();
        services.AddSingleton<SimulationParametersService>();

        services.AddSingleton<ErrorListViewModel>();
        services.AddSingleton<SimulationParametersViewModel>();
        services.AddSingleton<TasksPopOverViewModel>();
        services.AddSingleton<SimulationServiceViewModel>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}
