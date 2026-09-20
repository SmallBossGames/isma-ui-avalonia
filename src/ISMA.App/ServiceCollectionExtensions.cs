using System.Collections.ObjectModel;
using ISMA.App.Services;
using ISMA.App.ViewModels;
using ISMA.Domain.Contracts;
using ISMA.ExternalServices.ChartViewer;
using ISMA.ExternalServices.FileStorage;
using ISMA.ExternalServices.Lsp;
using ISMA.ExternalServices.Server;
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
        services.AddSingleton<WindowProvider>();
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<ITextEditorFactory, TextEditorFactory>();
        services.AddSingleton<ISimulationResultService, SimulationResultService>();
        services.AddSingleton<LspClient>();
        services.AddSingleton<ISyntaxHighlighter, LspSyntaxHighlighter>();
        services.AddSingleton<IPreferencesProvider, PreferencesProvider>();
        services.AddSingleton<EditorPlatformService>();
        services.AddSingleton<ISimulationParametersStoreService, SimulationParametersService>();
        services.AddSingleton<ISMA.BlueprintEditor.Services.ITextEditorFactory, BlueprintTextEditorFactory>();
        services.AddSingleton<IProjectEditorPort, ProjectEditorPortImpl>();
        services.AddSingleton<ErrorListViewModel>();
        services.AddSingleton<IModelErrorService>(sp => sp.GetRequiredService<ErrorListViewModel>());
        services.AddSingleton<SimulationParametersViewModel>(sp =>
        {
            var vm = new SimulationParametersViewModel(sp.GetService<ISimulationServerFacade>());
            vm.SetSetMethodsAction(methods =>
            {
                vm.IntegrationMethods = new ObservableCollection<string>(methods);
                vm.IntegrationMethod.IntegrationMethods = new ObservableCollection<string>(methods);
                vm.IntegrationMethod.SelectedMethodIndex = methods.Count > 0 ? 0 : -1;
                if (methods.Count > 0)
                {
                    vm.IntegrationMethod.SelectedMethod = methods[0];
                }
            });
            return vm;
        });
        services.AddSingleton<TasksPopOverViewModel>();
        services.AddSingleton<SimulationServiceViewModel>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}
