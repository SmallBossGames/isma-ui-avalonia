using System;
using Avalonia;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// Event arguments for arrow hit test results.
/// </summary>
public class ArrowHitTestEventArgs : EventArgs
{
    /// <summary>
    /// Whether the click was on the arrow head or body.
    /// </summary>
    public ArrowHitTestResult Result { get; }

    /// <summary>
    /// The click position.
    /// </summary>
    public Point ClickPosition { get; }

    public ArrowHitTestEventArgs(ArrowHitTestResult result, Point clickPosition)
    {
        Result = result;
        ClickPosition = clickPosition;
    }
}

/// <summary>
/// Hit test result for arrow controls.
/// </summary>
public class ArrowHitTestResult
{
    /// <summary>
    /// Whether the click was on the arrow head.
    /// </summary>
    public bool IsArrowHead { get; set; }

    /// <summary>
    /// Whether the click was on the arrow body.
    /// </summary>
    public bool IsArrowBody { get; set; }
}
