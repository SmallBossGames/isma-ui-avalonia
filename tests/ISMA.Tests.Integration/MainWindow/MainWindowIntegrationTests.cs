using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Views;
using ISMA.Tests.Integration;

namespace ISMA.Tests.Integration;

public class MainWindowIntegrationTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task MainWindow_Shows_All_Controls()
    {
        var menubar = FindControl<IsmaMenuBarView>("MenuBar");
        var toolbar = FindControl<IsmaToolBarView>("ToolBar");
        var tabPane = FindControl<EditorTabPaneView>("EditorTabPane");
        var errorList = FindControl<DataGrid>("ErrorList");
        var processBar = FindControl<SimulationProcessBarView>("ProcessBar");

        menubar.Should().NotBeNull();
        toolbar.Should().NotBeNull();
        tabPane.Should().NotBeNull();
        errorList.Should().NotBeNull();
        processBar.Should().NotBeNull();
    }
}
