using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Services;

/// <summary>
/// Represents clipboard data for blueprint editor operations.
/// </summary>
public class BlueprintClipboardData
{
    public List<BlueprintStateViewModel> States { get; init; } = new();
    public List<Guid> StateIds { get; init; } = new();
}

/// <summary>
/// Service for clipboard operations in the blueprint editor.
/// </summary>
public interface IBlueprintClipboardService
{
    /// <summary>
    /// Copies selected states to the clipboard.
    /// </summary>
    void CopyStates(IEnumerable<BlueprintStateViewModel> states);

    /// <summary>
    /// Pastes states from the clipboard.
    /// </summary>
    IEnumerable<BlueprintStateViewModel> PasteStates(BlueprintEditorViewModel editor, double offsetX, double offsetY);

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    void Clear();

    /// <summary>
    /// Whether the clipboard has state data.
    /// </summary>
    bool HasStates { get; }
}

/// <summary>
/// Default implementation of <see cref="IBlueprintClipboardService"/>.
/// </summary>
public class BlueprintClipboardService : IBlueprintClipboardService
{
    private BlueprintClipboardData? _clipboardData;

    public bool HasStates => _clipboardData != null && _clipboardData.States.Count > 0;

    public void CopyStates(IEnumerable<BlueprintStateViewModel> states)
    {
        _clipboardData = new BlueprintClipboardData
        {
            States = states.ToList(),
            StateIds = states.Select(s => s.Id).ToList()
        };
    }

    public IEnumerable<BlueprintStateViewModel> PasteStates(BlueprintEditorViewModel editor, double offsetX, double offsetY)
    {
        if (_clipboardData == null || _clipboardData.States.Count == 0)
            yield break;

        foreach (var originalState in _clipboardData.States)
        {
            var fillColor = originalState.FillColor;
            var newFillColor = fillColor is Avalonia.Media.SolidColorBrush solidBrush
                ? new Avalonia.Media.SolidColorBrush(solidBrush.Color)
                : fillColor;

            var newState = new BlueprintStateViewModel
            {
                Id = Guid.NewGuid(),
                CanvasPositionX = originalState.CanvasPositionX + offsetX,
                CanvasPositionY = originalState.CanvasPositionY + offsetY,
                Name = originalState.Name + " (copy)",
                Text = originalState.Text,
                IsEditable = true,
                IsMain = false,
                IsInit = false,
                FillColor = newFillColor,
                StateHeight = originalState.StateHeight
            };

            yield return newState;
        }
    }

    public void Clear()
    {
        _clipboardData = null;
    }
}
