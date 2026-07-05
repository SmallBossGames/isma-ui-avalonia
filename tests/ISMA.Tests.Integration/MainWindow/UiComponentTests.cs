using Avalonia;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Automation;
using ISMA.App.Views;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration;

/// <summary>
/// End-to-end tests for UI components and their interactions.
/// Tests that all UI controls are properly initialized and accessible.
/// </summary>
public class UiComponentTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task MainWindow_HasAllControls()
    {
        var menubar = _app.FindControl<IsmaMenuBarView>(AutomationIds.MenuBar);
        var toolbar = _app.FindControl<IsmaToolBarView>(AutomationIds.ToolBar);
        var tabPane = _app.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var errorList = _app.FindControl<DataGrid>(AutomationIds.ErrorList);
        var processBar = _app.FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);

        menubar.Should().NotBeNull();
        toolbar.Should().NotBeNull();
        tabPane.Should().NotBeNull();
        errorList.Should().NotBeNull();
        processBar.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_MenuBar_HasAutomationIds()
    {
        var menuBar = _app.FindControl<IsmaMenuBarView>(AutomationIds.MenuBar);
        menuBar.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_ToolBar_HasAutomationIds()
    {
        var toolbar = _app.FindControl<IsmaToolBarView>(AutomationIds.ToolBar);
        toolbar.Should().NotBeNull();

        var newText = UiHelpers.FindDescendants<Button>(toolbar!)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == AutomationIds.ToolBarNewText);
        newText.Should().NotBeNull();

        var verify = UiHelpers.FindDescendants<Button>(toolbar)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == AutomationIds.ToolBarVerify);
        verify.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_ProcessBar_HasAutomationIds()
    {
        var processBar = _app.FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);
        processBar.Should().NotBeNull();

        var run = UiHelpers.FindDescendants<Button>(processBar!)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == AutomationIds.ProcessBarRun);
        run.Should().NotBeNull();
    }
}
