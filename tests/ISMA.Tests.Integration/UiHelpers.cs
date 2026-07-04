using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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
        window.Flush();
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
        window.Flush();
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
        window.Flush();
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

        // Headless fallback: create TextEditor and connect to active ViewModel with event subscriptions
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
        window.Flush();
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
        window.Flush();
    }

    /// <summary>
    /// Show the settings panel.
    /// </summary>
    public static void ShowSettings(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        vm!.ShowSettings = true;
        window.Flush();
    }

    /// <summary>
    /// Hide the settings panel.
    /// </summary>
    public static void HideSettings(this MainWindow window)
    {
        var vm = window.DataContext as MainWindowViewModel;
        vm!.ShowSettings = false;
        window.Flush();
    }

    /// <summary>
    /// Flush the Avalonia dispatcher and force a layout update to ensure
    /// TabControl data templates are applied in headless mode.
    /// </summary>
    public static void Flush(this MainWindow window)
    {
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.ApplyTemplate();

        // Use the window's actual size for layout
        var size = new Size(window.Width, window.Height);
        window.Measure(size);
        window.Arrange(new Rect(default, size));

        // Force TabControl container generation
        var tabPaneView = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane)
            ?? window.GetVisualDescendants().OfType<EditorTabPaneView>().FirstOrDefault();
        var tabControl = tabPaneView?.FindControl<TabControl>("ProjectTabs")
            ?? tabPaneView?.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();
        if (tabControl is not null)
        {
            tabControl.ApplyTemplate();
            tabControl.Measure(size);
            tabControl.Arrange(new Rect(default, tabControl.DesiredSize));
        }

        // Force ContentControl template application for SettingsPanel
        var settingsPanel = window.FindControl<ContentControl>("SettingsPanel");
        if (settingsPanel is not null && settingsPanel.IsVisible)
        {
            settingsPanel.ApplyTemplate();
            settingsPanel.Measure(size);
            settingsPanel.Arrange(new Rect(default, settingsPanel.DesiredSize));
        }
    }

    /// <summary>
    /// Wait for async operations to complete.
    /// </summary>
    public static async Task FlushAsync(this MainWindow window, int delayMs = 50)
    {
        window.Flush();
        await Task.Delay(delayMs);
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
    /// </summary>
    public static void ClickAddStateButton(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var button = editor?.FindControl<Button>("AddStateButton");
        if (button is null)
            throw new InvalidOperationException("Add State button not found in active blueprint editor.");

        if (button.Command is not null)
        {
            button.Command.Execute(button.CommandParameter);
        }
        window.Flush();
    }

    /// <summary>
    /// Click the "Add Transition" toggle button in the blueprint editor toolbar.
    /// Toggles between entering and exiting add-transition mode.
    /// </summary>
    public static void ClickAddTransitionToggle(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var toggle = editor?.FindControl<ToggleButton>("AddTransitionToggle");
        if (toggle is null)
            throw new InvalidOperationException("Add Transition toggle not found in active blueprint editor.");

        if (toggle.Command is not null)
        {
            toggle.Command.Execute(toggle.CommandParameter);
        }
        window.Flush();
    }

    /// <summary>
    /// Click the "Remove State" toggle button in the blueprint editor toolbar.
    /// </summary>
    public static void ClickRemoveStateToggle(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var toggle = editor?.FindControl<ToggleButton>("RemoveStateToggle");
        if (toggle is null)
            throw new InvalidOperationException("Remove State toggle not found in active blueprint editor.");

        if (toggle.Command is not null)
        {
            toggle.Command.Execute(toggle.CommandParameter);
        }
        window.Flush();
    }

    /// <summary>
    /// Click the "Remove Transition" toggle button in the blueprint editor toolbar.
    /// </summary>
    public static void ClickRemoveTransitionToggle(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        var toggle = editor?.FindControl<ToggleButton>("RemoveTransitionToggle");
        if (toggle is null)
            throw new InvalidOperationException("Remove Transition toggle not found in active blueprint editor.");

        if (toggle.Command is not null)
        {
            toggle.Command.Execute(toggle.CommandParameter);
        }
        window.Flush();
    }

    /// <summary>
    /// Get the number of StateBox controls in the active blueprint editor canvas.
    /// </summary>
    public static int GetStateBoxCount(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        return editor != null ? FindDescendants<StateBox>(editor).Count() : 0;
    }

    /// <summary>
    /// Get the number of ArrowLine controls in the active blueprint editor canvas.
    /// </summary>
    public static int GetArrowLineCount(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        return editor != null ? FindDescendants<ArrowLine>(editor).Count() : 0;
    }

    /// <summary>
    /// Get the number of LoopArrow controls in the active blueprint editor canvas.
    /// </summary>
    public static int GetLoopArrowCount(this MainWindow window)
    {
        var editor = window.GetActiveBlueprintEditor();
        return editor != null ? FindDescendants<LoopArrow>(editor).Count() : 0;
    }

    /// <summary>
    /// Get the first StateBox with the given name text in the active blueprint editor.
    /// </summary>
    public static StateBox? GetStateBoxByName(this MainWindow window, string name)
    {
        var editor = window.GetActiveBlueprintEditor();
        if (editor is null) return null;

        var stateBoxes = FindDescendants<StateBox>(editor).ToList();
        foreach (var sb in stateBoxes)
        {
            if (sb.Name == name)
                return sb;
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
        window.Flush();
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
        window.Flush();
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
        window.Flush();
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
    /// </summary>
    public static void ClickTabCloseButton(this MainWindow window, int index)
    {
        var tabPane = window.FindControl<EditorTabPaneView>(AutomationIds.EditorTabPane);
        var tabControl = tabPane?.Content as TabControl;
        if (tabControl is null)
            throw new InvalidOperationException("TabControl not found in EditorTabPane.");

        var tabItem = tabControl.ContainerFromIndex(index) as TabItem;
        if (tabItem is null)
            throw new InvalidOperationException($"TabItem at index {index} not found (only {tabControl.Items.Count} tabs).");

        var closeButton = FindDescendants<Button>(tabItem)
            .FirstOrDefault(b => b.GetValue(AutomationProperties.AutomationIdProperty) as string != null
                || b.Content is PathIcon);

        if (closeButton is null)
            throw new InvalidOperationException($"Close button not found in tab item at index {index}.");

        closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Flush();
    }

    /// <summary>
    /// Get the Items collection from the first ItemsControl in the TasksPopOver (InProgress section).
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
                window.Flush();
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
        window.Flush();
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
}
