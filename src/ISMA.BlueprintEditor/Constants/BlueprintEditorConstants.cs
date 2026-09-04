using Avalonia.Media;

namespace ISMA.BlueprintEditor.Constants;

/// <summary>
/// Geometry and interaction constants of the blueprint editor,
/// ported from the original ISMA Kotlin/JavaFX editor.
/// </summary>
public static class BlueprintEditorConstants
{
    // State dimensions
    public const double DefaultStateWidth = 110.0;
    public const double DefaultStateHeight = 65.0;
    public const double FixedStateHeight = 60.0;
    public const double CornerRadius = 20.0;
    public const double StateNameFontSize = 16.0;
    public const double StateInset = 10.0;

    // Arrow geometry
    public const double ArrowLineOffset = 10.0;
    public const double ArrowTextXOffset = 75.0;
    public const double ArrowTextYOffset = 50.0;
    public const double ArrowLineStroke = 3.0;
    public const double ArrowheadStroke = 3.0;
    public const double ArrowheadWidth = 7.0;
    public const double ArrowLabelFontSize = 16.0;
    public const double ArrowLabelFieldWidth = 120.0;

    // Loop arrow
    public const double LoopCircleRadius = 40.0;
    public const double LoopCircleCenterX = 60.0;
    public const double LoopArrowheadX = 100.0;
    public const double LoopLabelX = 120.0;
    public const double LoopLabelYOffset = -10.0;

    // Interaction
    public const long ClickDelayMs = 200;

    // PopOver
    public const double PopoverMinWidth = 300.0;
    public const double PopoverPadding = 10.0;
    public const double PopoverCornerRadius = 5.0;
    public const double PopoverShadowRadius = 20.0;

    // Colors
    /// <summary>Arrow/label color — light so arrows stay visible on the dark canvas.</summary>
    public static readonly IBrush ArrowColor = Brushes.White;
}
