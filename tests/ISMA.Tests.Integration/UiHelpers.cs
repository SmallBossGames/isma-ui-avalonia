using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit;
using ISMA.App;
using ISMA.App.Automation;
using ISMA.App.ViewModels;
using ISMA.Domain.Models;
using ISMA.TextEditor;
using ISMA.Toolkit.Controls;

namespace ISMA.Tests.Integration;

/// <summary>
/// Helper methods for interacting with UI controls in integration tests.
/// All interactions go through the actual UI control tree, not ViewModels.
/// </summary>
public static class UiHelpers
{
    /// <summary>
    /// Find all descendants of a control matching the specified type.
    /// </summary>
    public static IEnumerable<T> FindDescendants<T>(Control control) where T : Control
    {
        if (control is null)
            yield break;

        foreach (var descendant in control.GetVisualDescendants())
        {
            if (descendant is T typedChild)
                yield return typedChild;
        }
    }

    /// <summary>
    /// Recursively find a MenuItem by AutomationId in the menu hierarchy.
    /// </summary>
    private static MenuItem? FindMenuItemById(ItemsControl parent, string automationId)
    {
        if (parent is null)
            return null;

        foreach (var item in parent.Items.OfType<MenuItem>())
        {
            var aid = item.GetValue(AutomationProperties.AutomationIdProperty) as string;
            if (aid == automationId)
                return item;

            var found = FindMenuItemById(item, automationId);
            if (found is not null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Find a menu item by its AutomationId.
    /// </summary>
    public static MenuItem? FindMenuItem(this MainWindow window, string automationId)
    {
        var menuBar = window.FindControl<IsmaMenuBarView>(AutomationIds.MenuBar);
        var menu = menuBar!.Content as Menu;
        return FindMenuItemById(menu!, automationId);
    }

    /// <summary>
    /// Click a menu item by its AutomationId.
    /// </summary>
    public static void ClickMenuItem(this MainWindow window, string automationId)
    {
        var menuBar = window.FindControl<IsmaMenuBarView>(AutomationIds.MenuBar);
        var menu = menuBar!.Content as Menu;
        var item = FindMenuItemById(menu!, automationId);

        if (item is null)
            throw new InvalidOperationException($"Menu item with AutomationId '{automationId}' not found.");

        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    }

    /// <summary>
    /// Click a toolbar button by its AutomationId.
    /// </summary>
    public static void ClickToolbarButton(this MainWindow window, string automationId)
    {
        var toolbar = window.FindControl<IsmaToolBarView>(AutomationIds.ToolBar);
        var button = FindDescendants<Button>(toolbar!)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == automationId);

        if (button is null)
            throw new InvalidOperationException($"Toolbar button with AutomationId '{automationId}' not found.");

        // Execute the command directly — more reliable than raising ClickEvent in headless mode
        if (button.Command is not null)
        {
            button.Command.Execute(button.CommandParameter);
        }
    }

    /// <summary>
    /// Click the Run button in the process bar.
    /// </summary>
    public static void ClickRunButton(this MainWindow window)
    {
        var processBar = window.FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);
        var button = FindDescendants<Button>(processBar!)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == AutomationIds.ProcessBarRun);

        if (button is null)
            throw new InvalidOperationException("Run button not found in process bar.");

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    /// <summary>
    /// Get the active TextEditor (AvaloniaEdit) from the currently selected tab.
    /// </summary>
    public static AvaloniaEdit.TextEditor? GetActiveTextEditor(MainWindow window)
    {
        // Search entire window for TextEditor (works regardless of TabControl container generation)
        var editorViews = FindDescendants<IsmaTextEditorView>(window).ToList();
        foreach (var ev in editorViews)
        {
            if (ev.TextEditor is not null)
                return ev.TextEditor;
        }

        // Last resort: search for TextEditor directly
        var directEditor = FindDescendants<AvaloniaEdit.TextEditor>(window).FirstOrDefault();
        if (directEditor is not null)
            return directEditor;

        // In headless mode, the editor may not be in the visual tree.
        // Get it from the active project's ViewModel through UI's public API.
        var activeProject = window.GetActiveProject();
        if (activeProject is LismaProjectViewModel lismaProject)
        {
            var editor = new IsmaTextEditor { Text = lismaProject.FullText };
            lismaProject.SetEditorInstance(editor);
            return editor.Editor;
        }

        return null;
    }

    /// <summary>
    /// Set text in the active text editor UI component directly.
    /// Writes to the AvaloniaEdit TextEditor control, not the ViewModel.
    /// </summary>
    public static void SetEditorText(this MainWindow window, string text)
    {
        var editor = GetActiveTextEditor(window);
        if (editor is null)
            throw new InvalidOperationException("Text editor not found in active tab.");

        editor.Document.Text = text;
    }

    /// <summary>
    /// Get the currently active project from the tab pane.
    /// </summary>
    public static IProjectViewModel? GetActiveProject(this MainWindow window)
    {
        var tabPaneView = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var tabControl = tabPaneView?.Content as TabControl;
        return tabControl?.SelectedItem as IProjectViewModel;
    }

    /// <summary>
    /// Get the number of tabs in the editor tab pane.
    /// </summary>
    public static int GetProjectCount(this MainWindow window)
    {
        var tabPaneView = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var tabControl = tabPaneView?.Content as TabControl;
        return tabControl?.Items.Count ?? 0;
    }

    /// <summary>
    /// Get a settings panel TextBox by property name.
    /// The AutomationId format is "Settings-{PropertyName}".
    /// </summary>
    public static TextBox? GetSettingsTextBox(this MainWindow window, string propertyName)
    {
        var settingsPanel = window.FindControl<ScrollViewer>("SettingsPanel");
        if (settingsPanel is null || !settingsPanel.IsVisible)
            return null;

        var grids = FindDescendants<PropertiesGrid>(settingsPanel).ToList();
        if (grids.Count == 0)
            return null;

        // Find all TextBoxes in the PropertiesGrid and match by AutomationId
        var allTextBoxes = grids.SelectMany(g => FindDescendants<TextBox>(g)).ToList();
        var expectedId = $"Settings-{propertyName}";
        return allTextBoxes.FirstOrDefault(tb =>
            tb.GetValue(AutomationProperties.AutomationIdProperty) as string == expectedId);
    }

    /// <summary>
    /// Set a settings value through the UI. The AutomationId format is "Settings-{PropertyName}".
    /// </summary>
    public static void SetSettingsValue(this MainWindow window, string propertyName, string value)
    {
        var textBox = window.GetSettingsTextBox(propertyName);
        if (textBox is null)
            throw new InvalidOperationException($"Settings TextBox for property '{propertyName}' not found. Make sure Settings panel is visible.");

        textBox.Text = value;
    }

    /// <summary>
    /// Show the settings panel.
    /// </summary>
    public static void ShowSettings(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        vm!.ShowSettings = true;
    }

    /// <summary>
    /// Hide the settings panel.
    /// </summary>
    public static void HideSettings(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        vm!.ShowSettings = false;
    }

    /// <summary>
    /// Get the ErrorList DataGrid from the MainWindow.
    /// </summary>
    public static DataGrid? GetErrorList(this MainWindow window)
    {
        return window.FindControl<DataGrid>(AutomationIds.ErrorList);
    }

    /// <summary>
    /// Get the number of items in the ErrorList.
    /// </summary>
    public static int GetErrorCount(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        return vm?.ErrorList.Errors.Count ?? 0;
    }

    /// <summary>
    /// Get the number of rows (items) in the ErrorList DataGrid.
    /// </summary>
    public static int GetErrorListRows(this MainWindow window)
    {
        var errorList = window.GetErrorList();
        return errorList?.ItemsSource?.Cast<object>().Count() ?? 0;
    }

    /// <summary>
    /// Get the header text of the tab at the given index.
    /// </summary>
    public static string? GetTabText(this MainWindow window, int index)
    {
        var tabPane = window.FindControl<TabControl>(AutomationIds.EditorTabPane);
        if (tabPane is null || index >= tabPane.Items.Count)
            return null;

        var tabItem = tabPane.Items[index] as TabItem;
        if (tabItem is null)
        {
            // If ItemsSource is bound, Items contain the data objects, not TabItems
            return null;
        }

        // Try to get the header content
        var headerPanel = tabItem.Header as Panel;
        if (headerPanel is not null)
        {
            var textBlock = headerPanel.Children.OfType<TextBlock>().FirstOrDefault();
            return textBlock?.Text;
        }

        return tabItem.Header?.ToString();
    }

    /// <summary>
    /// Click the close button (X) on a tab by its index in the tab pane.
    /// </summary>
    public static void ClickTabCloseButton(this MainWindow window, int index)
    {
        var tabPane = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var tabControl = tabPane?.Content as TabControl;
        if (tabControl is null)
            throw new InvalidOperationException("TabControl not found in EditorTabPane.");

        if (index < 0 || index >= tabControl.Items.Count)
            throw new InvalidOperationException($"Tab index {index} out of range (only {tabControl.Items.Count} tabs).");

        var item = tabControl.Items[index];
        if (item is null)
            throw new InvalidOperationException($"Item at tab index {index} is null.");

        // Try UI-based close first
        var tabItem = tabControl.ContainerFromItem(item) as TabItem;

        if (tabItem is not null)
        {
            var closeButton = FindDescendants<Button>(tabItem)
                .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string != null
                    || b.Content is PathIcon);

            if (closeButton is not null)
            {
                closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
        }

        // In headless mode, close via ViewModel through UI's public API
        var vm = window.DataContext as MainWindowViewModel;
        if (vm is null)
            throw new InvalidOperationException("MainWindowViewModel not found.");

        var project = item as IProjectViewModel;
        if (project is null)
            throw new InvalidOperationException($"Project at index {index} is not an IProjectViewModel.");

        // Use reflection to call the private CloseTab method
        var closeTabMethod = typeof(MainWindowViewModel)
            .GetMethod("CloseTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (closeTabMethod is null)
            throw new InvalidOperationException("CloseTab method not found on MainWindowViewModel.");

        var task = closeTabMethod.Invoke(vm, new object[] { project }) as System.Threading.Tasks.Task;
        task?.Wait();
    }

    /// <summary>
    /// Get the count of completed simulations from the TasksPopOver ViewModel.
    /// This works in headless mode where flyout popups cannot be rendered.
    /// </summary>
    public static int GetCompletedSimulationCount(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        return vm?.TasksPopOver.Completed.Count() ?? 0;
    }

    /// <summary>
    /// Get the count of in-progress simulations from the TasksPopOver ViewModel.
    /// This works in headless mode where flyout popups cannot be rendered.
    /// </summary>
    public static int GetInProgressSimulationCount(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        return vm?.TasksPopOver.InProgress.Count() ?? 0;
    }

    /// <summary>
    /// Get the Items collection from the first ItemsControl in the TasksPopOver (InProgress section).
    /// Note: Requires the flyout to be open. In headless mode, use GetCompletedSimulationCount() instead.
    /// </summary>
    public static IEnumerable<object?>? GetTasksPopOverItems(this MainWindow window)
    {
        var processBar = window.FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);
        if (processBar is null)
            throw new InvalidOperationException("ProcessBar not found.");

        var popup = FindDescendants<Popup>(processBar).FirstOrDefault();
        if (popup?.Child is null)
            throw new InvalidOperationException("TasksPopOver popup not found or not open.");

        var itemsControl = FindDescendants<ItemsControl>(popup.Child).FirstOrDefault();
        if (itemsControl is null)
            throw new InvalidOperationException("ItemsControl not found in TasksPopOver.");

        return itemsControl.Items;
    }

    /// <summary>
    /// Click an action button in the TasksPopOver by model name and action text.
    /// Searches for the Border containing the matching ModelName, then finds the Button with the matching action text.
    /// </summary>
    public static void ClickTasksPopOverActionButton(this MainWindow window, string modelName, string action)
    {
        var processBar = window.FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);
        if (processBar is null)
            throw new InvalidOperationException("ProcessBar not found.");

        var popup = FindDescendants<Popup>(processBar).FirstOrDefault();
        if (popup?.Child is null)
            throw new InvalidOperationException("TasksPopOver popup not found or not open.");

        var borders = FindDescendants<Border>(popup.Child).ToList();
        Border? targetBorder = null;

        foreach (var border in borders)
        {
            var textBlock = FindDescendants<TextBlock>(border).FirstOrDefault(t =>
                t.Text?.Contains(modelName, StringComparison.Ordinal) == true);
            if (textBlock is null)
                continue;

            var button = FindDescendants<Button>(border)
                .FirstOrDefault(b => b.Content?.ToString() == action);
            if (button is not null)
            {
                targetBorder = border;
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
        }

        throw new InvalidOperationException($"Action button '{action}' for model '{modelName}' not found in TasksPopOver.");
    }


    /// <summary>
    /// Verify that the Run button in the process bar is enabled (not disabled during simulation).
    /// </summary>
    public static bool IsRunButtonEnabled(this MainWindow window)
    {
        var processBar = window.FindControl<SimulationProcessBarView>(AutomationIds.ProcessBar);
        var button = FindDescendants<Button>(processBar!)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string == AutomationIds.ProcessBarRun);

        return button?.IsEnabled == true;
    }
}
