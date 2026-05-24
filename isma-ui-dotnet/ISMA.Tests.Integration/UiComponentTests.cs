using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
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

    [AvaloniaFact]
    public async Task MainWindow_HasEmptyProjectsList()
    {
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task MainWindow_HasNoActiveProject()
    {
        ViewModel.ActiveProject.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_HasErrorListWithZeroErrors()
    {
        ViewModel.ErrorList.Errors.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task MainWindow_HasSimulationParameters()
    {
        ViewModel.SimulationParameters.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_HasTasksPopOver()
    {
        ViewModel.TasksPopOver.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_TasksPopOverHasZeroProgress()
    {
        ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
        ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task MainWindow_ErrorListHasZeroErrorCount()
    {
        ViewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task MainWindow_SimulationParametersHasAllSections()
    {
        ViewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
        ViewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
        ViewModel.SimulationParameters.EventDetection.Should().NotBeNull();
        ViewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
        ViewModel.SimulationParameters.ResultProcessing.Should().NotBeNull();
    }
}
