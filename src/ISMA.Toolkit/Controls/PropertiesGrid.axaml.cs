using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using ISMA.Toolkit.Converters;

namespace ISMA.Toolkit.Controls;

/// <summary>
/// Specifies a single row of a <see cref="PropertiesGrid"/>: the label, the view-model
/// property to bind, and optional enable condition / combo items source.
/// </summary>
/// <param name="Label">The row label text.</param>
/// <param name="PropertyName">The view-model property to bind.</param>
/// <param name="EnabledWhen">Optional view-model bool property; the control is enabled only while it is true.</param>
/// <param name="ItemsSource">Optional view-model collection property; when set, the row renders a ComboBox.</param>
/// <param name="SelectionProperty">Optional view-model int property bound to the ComboBox SelectedIndex; when omitted, SelectedItem is bound to <paramref name="PropertyName"/>.</param>
public sealed record PropertyRowSpec(
    string Label,
    string PropertyName,
    string? EnabledWhen = null,
    string? ItemsSource = null,
    string? SelectionProperty = null)
{
    /// <summary>
    /// Parameterless constructor for XAML instantiation; properties are set via attributes.
    /// </summary>
    public PropertyRowSpec() : this(string.Empty, string.Empty)
    {
    }
}

public partial class PropertiesGrid : UserControl
{
    public PropertiesGrid()
    {
        InitializeComponent();
        Rows.CollectionChanged += OnRowsChanged;
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

    /// <summary>
    /// Explicit row definitions, rendered in order.
    /// </summary>
    public ObservableCollection<PropertyRowSpec> Rows { get; } = new();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewModelProperty || change.Property == AutomationPrefixProperty)
        {
            BuildProperties();
        }
    }

    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BuildProperties();
    }

    private void BuildProperties()
    {
        PropertyPanel.Children.Clear();

        if (ViewModel is null)
            return;

        foreach (var row in Rows)
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
                Text = row.Label,
                Margin = new Thickness(4, 2),
                FontSize = 11,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            var valueControl = CreateValueControl(row);
            Grid.SetColumn(valueControl, 1);
            grid.Children.Add(valueControl);

            PropertyPanel.Children.Add(grid);
        }
    }

    private Control CreateValueControl(PropertyRowSpec row)
    {
        var automationId = $"{AutomationPrefix}-{row.PropertyName}";

        Control control = row.ItemsSource != null
            ? CreateComboBox(row)
            : CreateControlForProperty(row.PropertyName);

        control.SetValue(AutomationProperties.AutomationIdProperty, automationId);

        if (row.EnabledWhen != null)
        {
            control.Bind(IsEnabledProperty, new Binding(row.EnabledWhen) { Mode = BindingMode.OneWay });
        }

        return control;
    }

    private ComboBox CreateComboBox(PropertyRowSpec row)
    {
        var comboBox = new ComboBox
        {
            Margin = new Thickness(4, 2),
            FontSize = 11,
            DataContext = ViewModel
        };
        comboBox.Bind(ComboBox.ItemsSourceProperty, new Binding(row.ItemsSource!) { Mode = BindingMode.OneWay });
        if (row.SelectionProperty != null)
        {
            comboBox.Bind(ComboBox.SelectedIndexProperty, new Binding(row.SelectionProperty)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }
        else
        {
            comboBox.Bind(ComboBox.SelectedItemProperty, new Binding(row.PropertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }
        return comboBox;
    }

    private Control CreateControlForProperty(string propertyName)
    {
        var property = ViewModel!.GetType().GetProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"View model has no property '{propertyName}'.");

        if (property.PropertyType == typeof(bool))
        {
            var checkBox = new CheckBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = ViewModel
            };
            checkBox.Bind(CheckBox.IsCheckedProperty, new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            return checkBox;
        }

        if (property.PropertyType.IsEnum)
        {
            var comboBox = new ComboBox
            {
                Margin = new Thickness(4, 2),
                FontSize = 11,
                DataContext = ViewModel,
                ItemsSource = System.Enum.GetValues(property.PropertyType).Cast<object>().Select(v => v.ToString()).ToList()
            };
            var enumBinding = new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new EnumToStringConverter(property.PropertyType)
            };
            comboBox.Bind(ComboBox.SelectedItemProperty, enumBinding);
            return comboBox;
        }

        var textBox = new TextBox
        {
            Margin = new Thickness(4, 2),
            FontSize = 11,
            DataContext = ViewModel
        };
        textBox.Bind(TextBox.TextProperty, new Binding(propertyName)
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return textBox;
    }
}
