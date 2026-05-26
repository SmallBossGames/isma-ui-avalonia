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

    [AvaloniaFact]
    public async Task MainWindow_Has_Empty_Projects_List()
    {
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task MainWindow_Has_No_Active_Project()
    {
        ViewModel.ActiveProject.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_Has_ErrorList_With_Zero_Errors()
    {
        ViewModel.ErrorList.Errors.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task MainWindow_Has_SimulationParameters()
    {
        ViewModel.SimulationParameters.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_Has_TasksPopOver()
    {
        ViewModel.TasksPopOver.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_SimulationParameters_Has_CauchyInitials()
    {
        ViewModel.SimulationParameters.CauchyInitials.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_SimulationParameters_Has_IntegrationMethod()
    {
        ViewModel.SimulationParameters.IntegrationMethod.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_SimulationParameters_Has_EventDetection()
    {
        ViewModel.SimulationParameters.EventDetection.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_SimulationParameters_Has_ResultSaving()
    {
        ViewModel.SimulationParameters.ResultSaving.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_SimulationParameters_Has_ResultProcessing()
    {
        ViewModel.SimulationParameters.ResultProcessing.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task MainWindow_ShowSettings_Property_Exists()
    {
        ViewModel.ShowSettings.Should().BeFalse();
        ViewModel.ShowSettings = true;
        ViewModel.ShowSettings.Should().BeTrue();
        ViewModel.ShowSettings = false;
        ViewModel.ShowSettings.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task MainWindow_ErrorList_Has_Zero_Error_Count()
    {
        ViewModel.ErrorList.ErrorCount.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task MainWindow_TasksPopOver_Has_Zero_Progress()
    {
        ViewModel.TasksPopOver.InProgressCount.Should().Be(0);
        ViewModel.TasksPopOver.CompletedCount.Should().Be(0);
    }
}
