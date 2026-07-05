using Avalonia;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Views;

namespace ISMA.Tests.Integration;

public class MainWindowIntegrationTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task MainWindow_Shows_All_Controls()
    {
        var menubar = _app.FindControl<IsmaMenuBarView>("MenuBar");
        var toolbar = _app.FindControl<IsmaToolBarView>("ToolBar");
        var tabPane = _app.FindControl<EditorTabPaneView>("EditorTabPane");
        var errorList = _app.FindControl<DataGrid>("ErrorList");
        var processBar = _app.FindControl<SimulationProcessBarView>("ProcessBar");

        menubar.Should().NotBeNull();
        toolbar.Should().NotBeNull();
        tabPane.Should().NotBeNull();
        errorList.Should().NotBeNull();
        processBar.Should().NotBeNull();
    }
}
