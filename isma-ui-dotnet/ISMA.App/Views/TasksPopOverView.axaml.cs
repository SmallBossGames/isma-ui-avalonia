using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ISMA.App.Views;

public partial class TasksPopOverView : UserControl
{
    public TasksPopOverView()
    {
        InitializeComponent();
    }

    private void OnChevronPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is TextBlock chevron)
        {
            chevron.SetValue(RotateTransform.AngleProperty, 90.0);
        }
    }

    private void OnChevronPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is TextBlock chevron)
        {
            chevron.SetValue(RotateTransform.AngleProperty, 0.0);
        }
    }

    private void OnChevronPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is TextBlock chevron)
        {
            var popup = chevron.FindAncestorOfType<Popup>();
            if (popup != null)
            {
                popup.PlacementTarget = chevron;
                popup.IsOpen = !popup.IsOpen;
            }
        }
    }
}
