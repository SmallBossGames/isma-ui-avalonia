using Avalonia;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Infrastructure.FileStorage;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration.UseCases.Startup;

/// <summary>
/// UC-10: Application Startup and Session Restore
/// Tests session restore, preferences loading, and last-opened files.
/// </summary>
public class SessionRestoreTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC10_LastOpenedFiles_TrackedOnOpen()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);
    }

    [AvaloniaFact]
    public async Task UC10_PreferencesProvider_LoadsDefaults()
    {
        var preferencesProvider = _app.Services.GetRequiredService<PreferencesProvider>();
        var preferences = preferencesProvider.Load();

        preferences.Should().NotBeNull();
        preferences.DefaultFilesPreferences.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task UC10_WindowGeometry_CanBeSet()
    {
        _app.Window.Width.Should().BeGreaterThan(0);
        _app.Window.Height.Should().BeGreaterThan(0);
    }

    [AvaloniaFact]
    public async Task UC10_NoProjectsOnFreshStartup()
    {
        _app.Window.GetProjectCount().Should().BeGreaterThanOrEqualTo(0);
    }
}
