using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ISMA.App;
using ISMA.App.Services;
using ISMA.App.Views;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.Tests.Integration;

/// <summary>
/// Base class for all integration tests. Sets up a headless Avalonia application
/// with the same DI configuration as the main app, but with a mocked ISimulationServerFacade.
/// 
/// All tests should resolve components from the shared service provider to ensure
/// they are tested with real dependencies (not manually constructed).
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected MockSimulationServerFacade MockServer { get; private set; } = null!;
    protected IServiceProvider Services { get; private set; } = null!;
    protected MainWindowViewModel ViewModel { get; private set; } = null!;
    protected MainWindow Window { get; private set; } = null!;

    protected IntegrationTestBase()
    {
        // Create the mock server facade FIRST - it must be registered before ConfigureTestServices
        MockServer = new MockSimulationServerFacade();

        var services = new ServiceCollection();
        
        // Register the mocked server facade (this is the only difference from the real app)
        services.AddSingleton<ISimulationServerFacade>(MockServer);
        
        // Use the shared service collection configuration from the app
        services.ConfigureTestServices();

        Services = services.BuildServiceProvider();

        // Resolve all components from the service provider (not manually constructed)
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

    public void Dispose()
    {
        Window?.Close();
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
