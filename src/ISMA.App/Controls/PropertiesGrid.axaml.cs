using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using ISMA.App.Converters;

namespace ISMA.App.Controls;

public partial class PropertiesGrid : UserControl
{
    public PropertiesGrid()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<object?> ViewModelProperty =
        AvaloniaProperty.Register<PropertiesGrid, object?>(nameof(ViewModel));

    public object? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly StyledProperty<string?> AutomationPrefixProperty =
        AvaloniaProperty.Register<PropertiesGrid, string?>(nameof(AutomationPrefix));

    public string? AutomationPrefix
    {
        get => GetValue(AutomationPrefixProperty);
        set => SetValue(AutomationPrefixProperty, value);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BuildProperties();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewModelProperty || change.Property == AutomationPrefixProperty)
        {
            BuildProperties();
        }
    }

    private void BuildProperties()
    {
        PropertyPanel.Children.Clear();

        if (ViewModel is null)
            return;

        var properties = ViewModel.GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .OrderBy(p => p.Name)
            .ToList();

        foreach (var prop in properties)
        {
            var row = CreatePropertyRow(prop, ViewModel);
            PropertyPanel.Children.Add(row);
        }
    }

    private Control CreatePropertyRow(System.Reflection.PropertyInfo prop, object viewModel)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            Margin = new Thickness(0, 2)
        };

        var label = new TextBlock
        {
            Text = prop.Name,
            Margin = new Thickness(4, 2),
            FontSize = 11,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        var valueControl = CreateValueControl(prop, viewModel);
        Grid.SetColumn(valueControl, 1);
        grid.Children.Add(valueControl);

        return grid;
    }

   private Control CreateValueControl(System.Reflection.PropertyInfo prop, object viewModel)
    {
        var automationId = $"{AutomationPrefix}-{prop.Name}";
        var binding = new Binding(prop.Name) 
        { 
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        if (prop.PropertyType == typeof(bool))
        {
            var checkBox = new CheckBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = viewModel
            };
            checkBox.SetValue(AutomationProperties.AutomationIdProperty, automationId);
            checkBox.Bind(CheckBox.IsCheckedProperty, binding);
            return checkBox;
        }
        else if (prop.PropertyType.IsEnum)
        {
            var comboBox = new ComboBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = viewModel
            };
            comboBox.SetValue(AutomationProperties.AutomationIdProperty, automationId);
            comboBox.ItemsSource = Enum.GetValues(prop.PropertyType).Cast<object>().Select(v => v.ToString());
            var enumBinding = new Binding(prop.Name) 
            { 
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new EnumToStringConverter(prop.PropertyType)
            };
            comboBox.Bind(ComboBox.TextProperty, enumBinding);
            return comboBox;
        }
        else if (prop.PropertyType == typeof(string))
        {
            var textBox = new TextBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = viewModel
            };
            textBox.SetValue(AutomationProperties.AutomationIdProperty, automationId);
            textBox.Bind(TextBox.TextProperty, binding);
            return textBox;
        }
        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(double) ||
                 prop.PropertyType == typeof(float) || prop.PropertyType == typeof(long))
        {
            var textBox = new TextBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = viewModel
            };
            textBox.SetValue(AutomationProperties.AutomationIdProperty, automationId);
            textBox.Bind(TextBox.TextProperty, binding);
            return textBox;
        }
        else
        {
            var textBox = new TextBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = viewModel
            };
            textBox.SetValue(AutomationProperties.AutomationIdProperty, automationId);
            textBox.Bind(TextBox.TextProperty, binding);
            return textBox;
        }
    }
}
