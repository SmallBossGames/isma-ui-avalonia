using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

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

    public static readonly StyledProperty<object?> ControlTemplateProperty =
        AvaloniaProperty.Register<PropertiesGrid, object?>(nameof(ControlTemplate));

    public object? ControlTemplate
    {
        get => GetValue(ControlTemplateProperty);
        set => SetValue(ControlTemplateProperty, value);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateControlTemplate();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewModelProperty)
        {
            UpdateControlTemplate();
        }
    }

    private void UpdateControlTemplate()
    {
        if (ViewModel is null)
        {
            ControlTemplate = null;
            return;
        }

        var type = ViewModel.GetType();
        ControlTemplate = type.Name switch
        {
            "String" or "Int32" or "Double" or "Boolean" => CreateTextBox(),
            _ => CreateTextBox()
        };
    }

    private Control CreateTextBox()
    {
        return new TextBox();
    }
}
