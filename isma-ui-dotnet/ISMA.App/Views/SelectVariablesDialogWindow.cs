using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class SelectVariablesDialogWindow : Window
{
    private readonly SelectVariablesDialogViewModel _vm;

    public SelectVariablesDialogWindow(SelectVariablesDialogViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        Title = "Select Variables for Chart";
        Width = 400;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;

        Content = CreateContent();
    }

    private Control CreateContent()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            Margin = new Thickness(16)
        };

        var xLabel = new TextBlock { Text = "X-Axis:", Margin = new Thickness(0, 0, 8, 4) };
        var xComboBox = new ComboBox
        {
            ItemsSource = _vm.AllColumns,
            SelectedItem = _vm.SelectedXAxis,
            Margin = new Thickness(0, 0, 0, 16)
        };
        xComboBox.SelectionChanged += (s, e) =>
        {
            if (xComboBox.SelectedItem is string s2)
                _vm.SelectedXAxis = s2;
        };

        var yLabel = new TextBlock { Text = "Y-Axis (multi-select):", Margin = new Thickness(0, 0, 0, 4) };
        var yListBox = new ListBox
        {
            ItemsSource = _vm.YAxisItems,
            SelectionMode = SelectionMode.Multiple,
            MinHeight = 200
        };

        var selectAllBtn = new Button { Content = "Select All", Command = _vm.SelectAllCommand, Margin = new Thickness(0, 8, 4, 0) };
        var unselectAllBtn = new Button { Content = "Unselect All", Command = _vm.UnselectAllCommand, Margin = new Thickness(4, 8, 0, 0) };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttonPanel.Children.Add(new Button { Content = "OK", Width = 70, Command = _vm.OkCommand });
        buttonPanel.Children.Add(new Button { Content = "Cancel", Width = 70, Command = _vm.CloseCommand });

        Grid.SetRow(xLabel, 0);
        Grid.SetRow(xComboBox, 0);
        Grid.SetRow(yLabel, 1);
        Grid.SetRow(yListBox, 1);
        Grid.SetRow(selectAllBtn, 2);
        Grid.SetRow(unselectAllBtn, 2);
        Grid.SetRow(buttonPanel, 3);
        Grid.SetColumn(buttonPanel, 0);
        Grid.SetColumnSpan(buttonPanel, 2);

        grid.Children.Add(xLabel);
        grid.Children.Add(xComboBox);
        grid.Children.Add(yLabel);
        grid.Children.Add(yListBox);
        grid.Children.Add(selectAllBtn);
        grid.Children.Add(unselectAllBtn);
        grid.Children.Add(buttonPanel);

        return grid;
    }
}
