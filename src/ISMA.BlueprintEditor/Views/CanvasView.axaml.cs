using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Views;

public partial class CanvasView : Panel
{
    public CanvasView()
    {
    }

    public static readonly StyledProperty<CanvasViewModel?> CanvasViewModelProperty =
        AvaloniaProperty.Register<CanvasView, CanvasViewModel?>(nameof(CanvasViewModel));

    public CanvasViewModel? CanvasViewModel
    {
        get => GetValue(CanvasViewModelProperty);
        set => SetValue(CanvasViewModelProperty, value);
    }
}
