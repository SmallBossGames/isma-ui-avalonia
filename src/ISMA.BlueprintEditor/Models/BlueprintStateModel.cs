namespace ISMA.BlueprintEditor.Models;

/// <summary>
/// A state (node) in a blueprint state machine. The state name is the identity
/// (no explicit ids) — this matches the original ISMA .iscm2 file format.
/// </summary>
/// <param name="CanvasPositionX">X position of the state box on the canvas.</param>
/// <param name="CanvasPositionY">Y position of the state box on the canvas.</param>
/// <param name="Name">Unique state name; identity of the state.</param>
/// <param name="Text">LISMA body code of the state.</param>
public record BlueprintStateModel(double CanvasPositionX, double CanvasPositionY, string Name, string Text);
