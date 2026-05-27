using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App;
using ISMA.App.Services;
using ISMA.App.Views;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.Tests.Integration;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.Tests.Integration;

public abstract class IntegrationTestBase
{
    protected MockSimulationServerFacade MockServer { get; private set; } = null!;
    protected IServiceProvider Services { get; private set; } = null!;
    protected MainWindowViewModel ViewModel { get; private set; } = null!;
    protected MainWindow Window { get; private set; } = null!;

    protected IntegrationTestBase()
    {
        MockServer = new MockSimulationServerFacade();

        var services = new ServiceCollection();
        services.AddSingleton<ISimulationServerFacade>(MockServer);
        services.AddSingleton<ISMA.Infrastructure.ChartViewer.GrinProcessLauncher>();
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<ITextEditorFactory, TextEditorFactory>();
        services.AddSingleton<ISimulationResultService, SimulationResultService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighterService>();
        services.AddSingleton<ISMA.ViewModels.Services.SimulationParametersService>();
        services.AddSingleton<ISMA.ViewModels.Services.ISimulationParametersStoreService, ISMA.App.Services.SimulationParametersService>();
        services.AddSingleton<IModelErrorService, ModelErrorService>();
        services.AddSingleton<ErrorListViewModel>();
        services.AddSingleton<SimulationParametersViewModel>();
        services.AddSingleton<TasksPopOverViewModel>();
        services.AddSingleton<SimulationServiceViewModel>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        ViewModel = Services.GetRequiredService<MainWindowViewModel>();
        Window = new MainWindow(ViewModel);
        Window.Show();
    }

    protected T? FindControl<T>(string name) where T : Control
    {
        return Window.FindControl<T>(name);
    }

    protected async Task FlushDispatcher()
    {
        await Task.Delay(50);
    }
}
