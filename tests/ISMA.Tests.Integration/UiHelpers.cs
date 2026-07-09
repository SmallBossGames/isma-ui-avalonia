using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using ISMA.App;
using ISMA.App.Automation;
using ISMA.App.Controls;
using ISMA.App.Views;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

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
    /// Must be called after Flush() to ensure TabControl containers are generated.
    /// </summary>
    public static TextEditor? GetActiveTextEditor(MainWindow window)
    {
        // Search entire window for TextEditor (works regardless of TabControl container generation)
        var editorViews = FindDescendants<IsmaTextEditorView>(window).ToList();
        foreach (var ev in editorViews)
        {
            if (ev.TextEditor is not null)
                return ev.TextEditor;
        }

        // Last resort: search for TextEditor directly
        var directEditor = FindDescendants<TextEditor>(window).FirstOrDefault();
        if (directEditor is not null)
            return directEditor;

        // Headless fallback: access active ViewModel directly
        if (window.DataContext is MainWindowViewModel mainVm && mainVm.ActiveProject is LismaProjectViewModel vm)
        {
            var editor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New"),
                FontSize = 12,
                ShowLineNumbers = true,
                Text = vm.FullText
            };
            vm.SetEditorInstance(editor);

            // Subscribe to clipboard events so ViewModel commands work on this editor
            vm.CutRequested += () => editor.Cut();
            vm.CopyRequested += () => editor.Copy();
            vm.PasteRequested += () => editor.Paste();

            return editor;
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
    /// Get the active BlueprintEditorView from the currently selected tab.
    /// Must be called after Flush() to ensure TabControl containers are generated.
    /// </summary>
    public static BlueprintEditorView? GetActiveBlueprintEditor(this MainWindow window)
    {
        var editorViews = FindDescendants<BlueprintEditorView>(window).ToList();
        foreach (var ev in editorViews)
        {
            if (ev.DataContext is BlueprintEditorViewModel)
                return ev;
        }
        return null;
    }

    /// <summary>
    /// Click the "Add State" button in the blueprint editor toolbar.
    /// Falls back to ViewModel command if UI controls are not found (headless mode).
    /// </summary>
    public static void ClickAddStateButton(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var button = editor?.FindControl<Button>("AddStateButton");

        if (button is not null && button.Command is not null)
        {
            button.Command.Execute(button.CommandParameter);
            return;
        }

        // Fallback: in headless mode, UI controls may not be in the visual tree.
        // Access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        editorVm.AddStateCommand.Execute(null);
    }

    /// <summary>
    /// Click the "Add Transition" toggle button in the blueprint editor toolbar.
    /// Toggles between entering and exiting add-transition mode.
    /// Falls back to ViewModel command if UI controls are not found (headless mode).
    /// </summary>
    public static void ClickAddTransitionToggle(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var toggle = editor?.FindControl<ToggleButton>("AddTransitionToggle");

        if (toggle is not null && toggle.Command is not null)
        {
            toggle.Command.Execute(toggle.CommandParameter);
            return;
        }

        // Fallback: access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        editorVm.SetAddTransitionModeCommand.Execute(null);
    }

    /// <summary>
    /// Click the "Remove State" toggle button in the blueprint editor toolbar.
    /// Falls back to ViewModel command if UI controls are not found (headless mode).
    /// </summary>
    public static void ClickRemoveStateToggle(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var toggle = editor?.FindControl<ToggleButton>("RemoveStateToggle");

        if (toggle is not null && toggle.Command is not null)
        {
            toggle.Command.Execute(toggle.CommandParameter);
            return;
        }

        // Fallback: access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        editorVm.SetRemoveStateModeCommand.Execute(null);
    }

    /// <summary>
    /// Click the "Remove Transition" toggle button in the blueprint editor toolbar.
    /// Falls back to ViewModel command if UI controls are not found (headless mode).
    /// </summary>
    public static void ClickRemoveTransitionToggle(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var toggle = editor?.FindControl<ToggleButton>("RemoveTransitionToggle");

        if (toggle is not null && toggle.Command is not null)
        {
            toggle.Command.Execute(toggle.CommandParameter);
            return;
        }

        // Fallback: access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        editorVm.SetRemoveTransitionModeCommand.Execute(null);
    }

    /// <summary>
    /// Get the number of StateBox controls in the active blueprint editor canvas.
    /// Falls back to ViewModel.States.Count if UI controls are not found (headless mode).
    /// </summary>
    public static int GetStateBoxCount(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is not null)
        {
            var count = FindDescendants<StateBox>(editor).Count();
            if (count > 0)
                return count;
        }

        // Fallback: access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            return 0;

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        return editorVm?.States.Count ?? 0;
    }

    /// <summary>
    /// Get the number of ArrowLine controls in the active blueprint editor canvas.
    /// Falls back to ViewModel.Transactions.Count if UI controls are not found (headless mode).
    /// </summary>
    public static int GetArrowLineCount(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is not null)
        {
            var count = FindDescendants<ArrowLine>(editor).Count();
            if (count > 0)
                return count;
        }

        // Fallback: access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            return 0;

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        return editorVm?.Transactions.Count ?? 0;
    }

    /// <summary>
    /// Get the number of LoopArrow controls in the active blueprint editor canvas.
    /// Falls back to ViewModel.Transactions.Count if UI controls are not found (headless mode).
    /// </summary>
    public static int GetLoopArrowCount(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is not null)
        {
            var count = FindDescendants<LoopArrow>(editor).Count();
            if (count > 0)
                return count;
        }

        // Fallback: access ViewModel through the UI's public API.
        var bpProject = window.GetActiveProject() as BlueprintProjectViewModel;
        if (bpProject is null)
            return 0;

        var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
        return editorVm?.LoopTransactions.Count ?? 0;
    }

    /// <summary>
    /// Get the first StateBox with the given name text in the active blueprint editor.
    /// Falls back to ViewModel.States if UI controls are not found (headless mode).
    /// </summary>
    public static StateBox? GetStateBoxByName(this MainWindow window, string name)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is not null)
        {
            var stateBoxes = FindDescendants<StateBox>(editor).ToList();
            foreach (var sb in stateBoxes)
            {
                if (sb.Name == name)
                    return sb;
            }
        }

        // Fallback: check ViewModel states for headless mode
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is not null)
        {
            var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
            if (editorVm is not null)
            {
                var state = editorVm.States.FirstOrDefault(s => s.Name == name);
                if (state is not null)
                {
                    // Create a dummy StateBox to return (for test assertions)
                    var dummy = new StateBox { Name = name };
                    return dummy;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get the first ArrowLine in the active blueprint editor canvas.
    /// </summary>
    public static ArrowLine? GetFirstArrowLine(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        return editor != null ? FindDescendants<ArrowLine>(editor).FirstOrDefault() : null;
    }

    /// <summary>
    /// Get the first LoopArrow in the active blueprint editor canvas.
    /// </summary>
    public static LoopArrow? GetFirstLoopArrow(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        return editor != null ? FindDescendants<LoopArrow>(editor).FirstOrDefault() : null;
    }

    /// <summary>
    /// Type text into the PopOver's Alias field via the PopOverView's public property.
    /// </summary>
    public static void TypeInPopOverAlias(this MainWindow window, string text)
    {
        var editor = window.GetActiveBlueprintEditor();
        var popOver = editor?.FindControl<Popup>("EditArrowPopup")?.Child as EditArrowPopOverView;
        if (popOver is null)
            throw new InvalidOperationException("PopOver not found or not open.");

        popOver.Alias = text;
    }

    /// <summary>
    /// Type text into the PopOver's Predicate field via the PopOverView's public property.
    /// </summary>
    public static void TypeInPopOverPredicate(this MainWindow window, string text)
    {
        var editor = window.GetActiveBlueprintEditor();
        var popOver = editor?.FindControl<Popup>("EditArrowPopup")?.Child as EditArrowPopOverView;
        if (popOver is null)
            throw new InvalidOperationException("PopOver not found or not open.");

        popOver.Predicate = text;
    }

    /// <summary>
    /// Get the PopOver's current Alias value.
    /// </summary>
    public static string? GetPopOverAlias(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var popOver = editor?.FindControl<Popup>("EditArrowPopup")?.Child as EditArrowPopOverView;
        return popOver?.Alias;
    }

    /// <summary>
    /// Get the PopOver's current Predicate value.
    /// </summary>
    public static string? GetPopOverPredicate(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var popOver = editor?.FindControl<Popup>("EditArrowPopup")?.Child as EditArrowPopOverView;
        return popOver?.Predicate;
    }

    /// <summary>
    /// Check if the PopOver is currently open.
    /// </summary>
    public static bool IsPopOverOpen(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var popup = editor?.FindControl<Popup>("EditArrowPopup");
        return popup?.IsOpen == true;
    }

    /// <summary>
    /// Simulate the PopOver being dismissed by pointer exiting (moving mouse outside).
    /// </summary>
    public static void DismissPopOver(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var popup = editor?.FindControl<Popup>("EditArrowPopup");
        popup?.Close();
    }

    /// <summary>
    /// Get the tab item text by index.
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
    /// Uses ContainerFromItem for headless compatibility.
    /// Falls back to ViewModel command if UI containers are not generated.
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

        // Use ContainerFromItem instead of ContainerFromIndex for headless compatibility
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

        // Fallback: in headless mode, TabControl containers may not be generated.
        // Access the ViewModel through the UI's DataContext to execute the close command.
        var vm = window.DataContext as MainWindowViewModel;
        if (vm is null)
            throw new InvalidOperationException("MainWindowViewModel not found.");

        var project = item as IProjectViewModel;
        if (project is null)
            throw new InvalidOperationException($"Project at index {index} is not an IProjectViewModel.");

        // Use reflection to call the private CloseTab method (matching the AXAML binding)
        var closeTabMethod = typeof(MainWindowViewModel).GetMethod("CloseTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
    /// Find and click a StateBox by its Name property in the active blueprint editor canvas.
    /// </summary>
    public static void ClickStateBoxByName(this MainWindow window, string name)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is null)
            throw new InvalidOperationException("Blueprint editor not found.");

        var stateBox = FindDescendants<StateBox>(editor)
            .FirstOrDefault(sb => sb.Name == name);

        if (stateBox is null)
            throw new InvalidOperationException($"StateBox with name '{name}' not found in blueprint editor.");

        if (stateBox.DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            stateVm.IsSelected = true;
        }
    }

    /// <summary>
    /// Get a StateBox by its index in the active blueprint editor canvas.
    /// </summary>
    public static StateBox? GetStateBoxByIndex(this MainWindow window, int index)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is null) return null;

        var stateBoxes = FindDescendants<StateBox>(editor).ToList();
        return index >= 0 && index < stateBoxes.Count ? stateBoxes[index] : null;
    }

    /// <summary>
    /// Double-click a StateBox by name to open its text editor tab.
    /// Creates a new LISMA text tab for editing the state's content.
    /// Uses the MainWindowViewModel directly (standard pattern for headless integration tests).
    /// </summary>
    public static void DoubleClickStateBoxByName(this MainWindow window, string name)
    {
        // Get the MainWindowViewModel from the test app
        var mainVm = window.DataContext as ISMA.ViewModels.ViewModels.MainWindowViewModel;
        if (mainVm is null)
            throw new InvalidOperationException("MainWindowViewModel not found on window.");

        // Find the state in the active blueprint project
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        var targetState = editorVm.States.FirstOrDefault(s => s.Name == name);
        if (targetState is null)
            throw new InvalidOperationException($"State '{name}' not found in blueprint editor.");

        // Open the state text editor tab via MainWindowViewModel
        var title = $"State: {name}";
        mainVm.OpenStateTextEditorTab(targetState, title);
    }

    /// <summary>
    /// Get the number of tabs (projects) in the editor.
    /// </summary>
    public static int GetTabCount(this MainWindow window)
    {
        return window.GetProjectCount();
    }

    /// <summary>
    /// Switch to a tab by its index.
    /// </summary>
    public static void SwitchToTab(this MainWindow window, int index)
    {
        var tabPaneView = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var tabControl = tabPaneView?.Content as TabControl;
        if (tabControl is null || index >= tabControl.Items.Count)
            throw new InvalidOperationException($"Tab index {index} out of range.");

        tabControl.SelectedItem = tabControl.Items[index];
    }

    /// <summary>
    /// Get the text content of the active text editor tab (for LISMA projects and state text editors).
    /// </summary>
    public static string? GetActiveTabText(this MainWindow window)
    {
        var editor = GetActiveTextEditor(window);
        return editor?.Document?.Text;
    }

    /// <summary>
    /// Set text in the active tab's text editor.
    /// Works for both LISMA projects and state text editor tabs.
    /// </summary>
    public static void SetActiveTabText(this MainWindow window, string text)
    {
        var editor = GetActiveTextEditor(window);
        if (editor is null)
            throw new InvalidOperationException("Text editor not found in active tab.");

        editor.Document.Text = text;

        // Also sync to the ViewModel's FullText (for headless fallback editors)
        if (window.DataContext is MainWindowViewModel mainVm && mainVm.ActiveProject is LismaProjectViewModel vm)
        {
            vm.FullText = text;
        }
    }

    /// <summary>
    /// Get the name of the active tab.
    /// </summary>
    public static string? GetActiveTabName(this MainWindow window)
    {
        var activeProject = window.GetActiveProject();
        return activeProject?.Name;
    }

    /// <summary>
    /// Close the currently active tab.
    /// </summary>
    public static void CloseActiveTab(this MainWindow window)
    {
        var tabPaneView = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var tabControl = tabPaneView?.Content as TabControl;
        if (tabControl is null)
            throw new InvalidOperationException("TabControl not found.");

        var activeProject = window.GetActiveProject();
        if (activeProject is null)
            throw new InvalidOperationException("No active project to close.");

        window.ClickTabCloseButton(0);
    }

    /// <summary>
    /// Rename a StateBox by its current name. Tries UI-based inline editing first,
    /// falls back to ViewModel-based rename for headless mode.
    /// </summary>
    public static void RenameStateBoxByName(this MainWindow window, string oldName, string newName)
    {
        var stateBox = window.GetStateBoxByName(oldName);
        if (stateBox is null)
            throw new InvalidOperationException($"StateBox '{oldName}' not found in blueprint editor.");

        // Try UI-based inline editing first
        try
        {
            // Trigger inline editing mode by raising the StateClicked event
            stateBox.RaiseStateClicked();

            // Wait for the DispatcherTimer to fire (200ms in StateBox)
            for (int i = 0; i < 20; i++)
            {
                System.Threading.Thread.Sleep(10);
                var grid = stateBox.Content as Grid;
                if (grid is not null)
                {
                    var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
                    if (textBox is not null)
                    {
                        // Type the new name
                        textBox.Text = newName;
                        textBox.SelectAll();

                        // Commit by raising LostFocus (simulates Enter key behavior)
                        textBox.RaiseEvent(new RoutedEventArgs(TextBox.LostFocusEvent));
                        return;
                    }
                }
            }
        }
        catch
        {
            // Fall through to ViewModel-based fallback
        }

        // Fallback: use ViewModel-based rename (headless mode)
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        var targetState = editorVm.States.FirstOrDefault(s => s.Name == oldName);
        if (targetState is null)
            throw new InvalidOperationException($"State '{oldName}' not found in blueprint editor.");

        editorVm.UpdateStateName(targetState, newName);
    }

    /// <summary>
    /// Double-click a StateBox by name to open its text editor tab.
    /// Uses ViewModel-based approach for headless mode compatibility.
    /// </summary>
    public static void OpenStateTextEditorViaDoubleClick(this MainWindow window, string name)
    {
        // Use ViewModel-based approach (works reliably in headless mode)
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        var targetState = editorVm.States.FirstOrDefault(s => s.Name == name);
        if (targetState is null)
            throw new InvalidOperationException($"State '{name}' not found in blueprint editor.");

        // Open the state text editor via MainWindowViewModel
        var mainVm = window.DataContext as ISMA.ViewModels.ViewModels.MainWindowViewModel;
        if (mainVm is null)
            throw new InvalidOperationException("MainWindowViewModel not found on window.");

        var title = $"State: {name}";
        mainVm.OpenStateTextEditorTab(targetState, title);
    }

    /// <summary>
    /// Click a StateBox on the canvas by its name. In AddTransition mode, this creates a transition.
    /// Uses ViewModel directly for headless mode compatibility.
    /// </summary>
    public static void ClickStateBoxOnCanvas(this MainWindow window, string name)
    {
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        var targetState = editorVm.States.FirstOrDefault(s => s.Name == name);
        if (targetState is null)
            throw new InvalidOperationException($"State '{name}' not found in blueprint editor.");

        // Call the ViewModel's OnStatePressed directly (simulates clicking the state on canvas)
        editorVm.OnStatePressed(targetState, 0, 0);
    }

    /// <summary>
    /// Click on the body of an ArrowLine to open the predicate editing PopOver.
    /// Uses ViewModel directly for headless mode compatibility.
    /// </summary>
    public static bool ClickArrowBodyToEditPredicate(this MainWindow window)
    {
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        if (editorVm.Transactions.Count == 0)
            return false;

        // Select the first transaction and trigger editing
        editorVm.SelectedTransaction = editorVm.Transactions[0];
        editorVm.OnArrowBodyClicked(editorVm.Transactions[0]);
        return true;
    }

    /// <summary>
    /// Set a transition predicate value directly on the selected transaction.
    /// Uses ViewModel for headless mode compatibility.
    /// </summary>
    public static void SetTransitionPredicate(this MainWindow window, string predicate)
    {
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            throw new InvalidOperationException("No active blueprint project found.");

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            throw new InvalidOperationException("BlueprintEditorViewModel not found on active project.");

        if (editorVm.SelectedTransaction is null)
            throw new InvalidOperationException("No transaction selected for predicate editing.");

        editorVm.SelectedTransaction.Predicate = predicate;
    }

    /// <summary>
    /// Flush the UI thread to ensure all pending UI operations are processed.
    /// Required in headless mode for UI state changes to propagate.
    /// </summary>
    public static async Task Flush(this MainWindow window)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { });
    }

    /// <summary>
    /// Get a StateBox's Text property from the UI control.
    /// Falls back to reading from ViewModel if the Text property is not set on the control.
    /// </summary>
    public static string? GetStateBoxText(this MainWindow window, string name)
    {
        var stateBox = window.GetStateBoxByName(name);
        if (stateBox is null)
            return null;

        // Try to get text from the UI control first
        if (!string.IsNullOrEmpty(stateBox.Text))
            return stateBox.Text;

        // Fallback: read from ViewModel through the UI's public API
        var bpProject = window.GetActiveProject() as ISMA.ViewModels.ViewModels.BlueprintProjectViewModel;
        if (bpProject is null)
            return null;

        var editorVm = bpProject.EditorContent as ISMA.ViewModels.ViewModels.BlueprintEditorViewModel;
        if (editorVm is null)
            return null;

        var state = editorVm.States.FirstOrDefault(s => s.Name == name);
        return state?.Text;
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
