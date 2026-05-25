using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.Infrastructure.ChartViewer;
using ISMA.Infrastructure.FileStorage;
using ISMA.Infrastructure.Server;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.App;

/// <summary>
/// Shared service collection configuration for both the application and integration tests.
/// Tests can override specific registrations by calling this method and then replacing services.
/// </summary>
public static class ServiceCollectionExtensions
{
public static IServiceCollection ConfigureAppServices(this IServiceCollection services)
    {
        services.AddSingleton<GrinProcessLauncher>();
        services.AddSingleton<PreferencesProvider>();
        services.AddSingleton<SimulationServerManager>();
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<ITextEditorFactory, TextEditorFactory>();
        services.AddSingleton<ISimulationResultService, SimulationResultService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighterService>();
        services.AddSingleton<IModelErrorService, ModelErrorService>();
        services.AddSingleton<ISMA.ViewModels.Services.SimulationParametersService>();
        services.AddSingleton<ErrorListViewModel>();
        services.AddSingleton<SimulationParametersViewModel>();
        services.AddSingleton<TasksPopOverViewModel>();
        services.AddSingleton<SimulationServiceViewModel>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<MainWindowViewModel>();

        // Register App layer services after ViewModels so they can be injected
        services.AddSingleton<ISMA.App.Services.SimulationParametersService>();

        return services;
    }

    /// <summary>
    /// Creates a service collection suitable for integration tests.
    /// The ISimulationServerFacade must be registered before calling this method.
    /// </summary>
    public static IServiceCollection ConfigureTestServices(this IServiceCollection services)
    {
        // GrinProcessLauncher - replaced with mock in tests
        services.AddSingleton<GrinProcessLauncher>();
        
        // PreferencesProvider - use in-memory for tests
        services.AddSingleton<PreferencesProvider>();
        
        // ISimulationServerFacade - already registered by the test (mocked)
        
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<ITextEditorFactory, TextEditorFactory>();
        services.AddSingleton<ISimulationResultService, SimulationResultService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighterService>();
     services.AddSingleton<IModelErrorService, ModelErrorService>();
        services.AddSingleton<ISMA.ViewModels.Services.SimulationParametersService>();
        services.AddSingleton<ErrorListViewModel>();
        services.AddSingleton<SimulationParametersViewModel>();
        services.AddSingleton<TasksPopOverViewModel>();
        services.AddSingleton<SimulationServiceViewModel>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<MainWindowViewModel>();

        // Register App layer services after ViewModels
        services.AddSingleton<ISMA.App.Services.SimulationParametersService>();

        return services;
    }
}
