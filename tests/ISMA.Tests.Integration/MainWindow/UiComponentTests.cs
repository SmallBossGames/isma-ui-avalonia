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
public class UiComponentTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task MainWindow_HasAllControls()
    {
        var menubar = FindControl<IsmaMenuBarView>(AutomationIds.MenuBar);
        var toolbar = FindControl<IsmaToolBarView>(AutomationIds.ToolBar);
        var tabPane = FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var errorList = FindControl<DataGrid>(AutomationIds.ErrorList);
        var processBar = FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);

        menubar.Should().NotBeNull();
        toolbar.Should().NotBeNull();
        tabPane.Should().NotBeNull();
        errorList.Should().NotBeNull();
        processBar.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_MenuBar_HasAutomationIds()
    {
        var menuBar = FindControl<IsmaMenuBarView>(AutomationIds.MenuBar);
        menuBar.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_ToolBar_HasAutomationIds()
    {
        var toolbar = FindControl<IsmaToolBarView>(AutomationIds.ToolBar);
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
        var processBar = FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);
        processBar.Should().NotBeNull();

        var run = UiHelpers.FindDescendants<Button>(processBar!)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == AutomationIds.ProcessBarRun);
        run.Should().NotBeNull();
    }
}
