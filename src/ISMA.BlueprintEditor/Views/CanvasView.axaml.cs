using Avalonia.Controls;

namespace ISMA.BlueprintEditor.Views;

/// <summary>
/// The blueprint diagram canvas: a scrollable canvas with layered
/// <see cref="ItemsControl"/>s (states, transaction arrows, loop arrows).
/// Pure presentation — all interaction logic is routed to the view model via
/// the controls' routed events.
/// </summary>
public partial class CanvasView : UserControl
{
    /// <summary>Creates the canvas view.</summary>
    public CanvasView()
    {
        InitializeComponent();
    }

    /// <summary>The root canvas (host of the diagram layers and popovers).</summary>
    public BlueprintCanvas DiagramCanvas => RootCanvas;
}
