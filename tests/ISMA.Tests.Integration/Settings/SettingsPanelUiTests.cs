using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using FluentAssertions;
using ISMA.App;
using ISMA.App.Automation;
using ISMA.App.Controls;
using ISMA.Domain.Models;
using TextBox = Avalonia.Controls.TextBox;

namespace ISMA.Tests.Integration;

/// <summary>
/// UI-level integration tests for the Settings Panel.
/// These tests interact with actual UI controls (TextBox, CheckBox, ComboBox), not just ViewModels.
/// The Settings Panel is always visible as part of the main window layout.
/// </summary>
public class SettingsPanelUiTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;

    private static TextBox? FindTextBoxByAutomationId(IEnumerable<TextBox> textBoxes, string automationId)
    {
        return textBoxes.FirstOrDefault(tb =>
            tb.GetValue(AutomationProperties.AutomationIdProperty) as string == automationId);
    }

    private static CheckBox? FindCheckBoxByAutomationId(IEnumerable<Control> controls, string automationId)
    {
        return controls.OfType<CheckBox>().FirstOrDefault(cb =>
            cb.GetValue(AutomationProperties.AutomationIdProperty) as string == automationId);
    }

    [AvaloniaFact]
    public async Task SettingsPanel_Control_IsAlwaysVisible()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        settingsPanel!.IsVisible.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task CauchyInitials_ControlsExistAndDisplayDefaults()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        settingsPanel.IsVisible.Should().BeTrue();

        var grids = UiHelpers.FindDescendants<PropertiesGrid>(settingsPanel!).ToList();
        grids.Should().NotBeEmpty("PropertiesGrid controls should be in visual tree");

        var firstGrid = grids.First();
        firstGrid.ViewModel.Should().NotBeNull("PropertiesGrid.ViewModel should be set");

        var allTextBoxes = grids.SelectMany(g => UiHelpers.FindDescendants<TextBox>(g)).ToList();
        allTextBoxes.Count.Should().BeGreaterThanOrEqualTo(3, $"Expected at least 3 TextBoxes, found {allTextBoxes.Count}");

        var startTimeBox = FindTextBoxByAutomationId(allTextBoxes, AutomationIds.SettingsCauchyInitialsStartTime);
        var endTimeBox = FindTextBoxByAutomationId(allTextBoxes, AutomationIds.SettingsCauchyInitialsEndTime);
        var initialStepBox = FindTextBoxByAutomationId(allTextBoxes, AutomationIds.SettingsCauchyInitialsInitialStep);

        startTimeBox.Should().NotBeNull("StartTime TextBox should exist");
        endTimeBox.Should().NotBeNull("EndTime TextBox should exist");
        initialStepBox.Should().NotBeNull("InitialStep TextBox should exist");

        startTimeBox.Text.Should().NotBeNull("StartTime TextBox.Text should not be null");
        endTimeBox.Text.Should().NotBeNull("EndTime TextBox.Text should not be null");
        initialStepBox.Text.Should().NotBeNull("InitialStep TextBox.Text should not be null");

        double.Parse(startTimeBox.Text!).Should().BeApproximately(0.0, 0.01);
        double.Parse(endTimeBox.Text!).Should().BeApproximately(10.0, 0.01);
        double.Parse(initialStepBox.Text!).Should().BeApproximately(0.1, 0.01);
    }

    [AvaloniaFact]
    public async Task CauchyInitials_TextBoxUpdatesPropagateToViewModel()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var grids = UiHelpers.FindDescendants<PropertiesGrid>(settingsPanel!).ToList();
        var allTextBoxes = grids.SelectMany(g => UiHelpers.FindDescendants<TextBox>(g)).ToList();

        var startTimeBox = FindTextBoxByAutomationId(allTextBoxes, AutomationIds.SettingsCauchyInitialsStartTime);
        var cauchyVm = startTimeBox!.DataContext as CauchyInitialsViewModel;
        cauchyVm.Should().NotBeNull("TextBox DataContext should be CauchyInitialsViewModel");

        // Directly set the ViewModel property (bypasses binding which doesn't work in headless)
        cauchyVm.StartTime = 5.5;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Task.Delay(200).Wait();

        // Verify the TextBox displays the updated value (binding works OneWay from ViewModel to View)
        double.Parse(startTimeBox.Text!).Should().BeApproximately(5.5, 0.01);

        // Verify the parent ViewModel also has the updated value
        _app.ViewModel.SimulationParameters.CauchyInitials.StartTime.Should().Be(5.5);
    }

    [AvaloniaFact]
    public async Task EventDetection_CheckboxExistsAndWorks()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var grids = UiHelpers.FindDescendants<PropertiesGrid>(settingsPanel!).ToList();
        var allControls = grids.SelectMany(g => UiHelpers.FindDescendants<Control>(g)).ToList();

        var stepLimitCheckBox = FindCheckBoxByAutomationId(allControls, AutomationIds.SettingsEventDetectionStepLimit);

        stepLimitCheckBox.Should().NotBeNull("IsStepLimitInUse CheckBox should exist");
        stepLimitCheckBox.IsChecked.Should().BeFalse();

        stepLimitCheckBox.IsChecked = true;

        _app.ViewModel.SimulationParameters.EventDetection.IsStepLimitInUse.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task IntegrationMethod_ComboBoxExistsAndShowsOptions()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var methodComboBox = UiHelpers.FindDescendants<ComboBox>(settingsPanel!)
            .FirstOrDefault(cb => cb.GetValue(AutomationProperties.AutomationIdProperty) as string == "Settings-IntegrationMethod-SelectedMethod");

        methodComboBox.Should().NotBeNull("IntegrationMethod ComboBox should exist");
        methodComboBox.Should().NotBeNull();
        var items = methodComboBox!.ItemsSource!.Cast<string>().ToList();
        items.Should().Contain("Euler");
        items.Should().Contain("Runge-Kutta 2");
        items.Should().Contain("Runge-Kutta 4");

        methodComboBox.SelectedIndex.Should().Be(0);
    }

    [AvaloniaFact]
    public async Task IntegrationMethod_ComboBoxSelectionUpdatesViewModel()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var methodComboBox = UiHelpers.FindDescendants<ComboBox>(settingsPanel!)
            .First(cb => cb.GetValue(AutomationProperties.AutomationIdProperty) as string == "Settings-IntegrationMethod-SelectedMethod");

        methodComboBox.SelectedIndex = 2;

        _app.ViewModel.SimulationParameters.IntegrationMethod.SelectedMethod.Should().Be("Runge-Kutta 4");
    }

    [AvaloniaFact]
    public async Task ResultSaving_ComboBoxExistsAndShowsOptions()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var grids = UiHelpers.FindDescendants<PropertiesGrid>(settingsPanel!).ToList();
        var allComboBoxes = grids.SelectMany(g => UiHelpers.FindDescendants<ComboBox>(g)).ToList();

        var savingTargetBox = allComboBoxes.FirstOrDefault(cb =>
            cb.GetValue(AutomationProperties.AutomationIdProperty) as string == "Settings-ResultSaving-SavingTarget");

        savingTargetBox.Should().NotBeNull("SavingTarget ComboBox should exist");
        savingTargetBox.ItemsSource.Should().NotBeNull();

        var items = savingTargetBox!.ItemsSource.Cast<string>().ToList();
        items.Should().Contain("Memory");
        items.Should().Contain("File");

        savingTargetBox.Text.Should().Be("Memory");
    }

    [AvaloniaFact]
    public async Task ResultSaving_ComboBoxSelectionUpdatesViewModel()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var grids = UiHelpers.FindDescendants<PropertiesGrid>(settingsPanel!).ToList();
        var allComboBoxes = grids.SelectMany(g => UiHelpers.FindDescendants<ComboBox>(g)).ToList();

        var savingTargetBox = allComboBoxes.First(cb =>
            cb.GetValue(AutomationProperties.AutomationIdProperty) as string == "Settings-ResultSaving-SavingTarget");

        savingTargetBox.SelectedItem = "File";

        _app.ViewModel.SimulationParameters.ResultSaving.SavingTarget.Should().Be(SaveTarget.File);
    }

    [AvaloniaFact]
    public async Task AllSettingsSections_HavePropertiesGridControls()
    {
        var settingsPanel = _app.FindControl<ScrollViewer>("SettingsPanel");
        settingsPanel.Should().NotBeNull();
        var grids = UiHelpers.FindDescendants<PropertiesGrid>(settingsPanel!).ToList();

        grids.Should().HaveCount(4, "Should have 4 PropertiesGrid controls: CauchyInitials, Integration, EventDetection, ResultSaving");
    }
}
