using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// Interface for grid snapping support.
/// </summary>
public interface ISnapPosition
{
    /// <summary>
    /// Snaps the X position to the grid.
    /// </summary>
    void SnapPositionX(double gridSize);

    /// <summary>
    /// Snaps the Y position to the grid.
    /// </summary>
    void SnapPositionY(double gridSize);
}

/// <summary>
/// View model for a single state node on the blueprint canvas.
/// </summary>
public partial class BlueprintStateViewModel : ObservableObject, ISnapPosition
{
    private double _canvasPositionX;
    private double _canvasPositionY;
    private string _name = "";
    private string _text = "";
    private string _fillColor = "#F08080";
    private bool _isSelected;
    private bool _isEditable;
    private bool _isMain;
    private bool _isInit;
    private double _stateHeight = 65.0;
    private bool _isEnabled = true;

    /// <summary>
    /// Unique identifier for this state.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// X position on the editor canvas.
    /// </summary>
    public double CanvasPositionX
    {
        get => _canvasPositionX;
        set => SetProperty(ref _canvasPositionX, value);
    }

    /// <summary>
    /// Y position on the editor canvas.
    /// </summary>
    public double CanvasPositionY
    {
        get => _canvasPositionY;
        set => SetProperty(ref _canvasPositionY, value);
    }

    /// <summary>
    /// Display name of the state.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// Body text/code content of the state.
    /// </summary>
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    /// <summary>
    /// Fill color for the state box (hex string).
    /// </summary>
    public string FillColor
    {
        get => _fillColor;
        set => SetProperty(ref _fillColor, value);
    }

    /// <summary>
    /// Whether the state is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Whether the state name can be edited inline.
    /// </summary>
    public bool IsEditable
    {
        get => _isEditable;
        set => SetProperty(ref _isEditable, value);
    }

    /// <summary>
    /// Whether this is the permanent Main state.
    /// </summary>
    public bool IsMain
    {
        get => _isMain;
        set => SetProperty(ref _isMain, value);
    }

    /// <summary>
    /// Whether this is the permanent Init state.
    /// </summary>
    public bool IsInit
    {
        get => _isInit;
        set => SetProperty(ref _isInit, value);
    }

    /// <summary>
    /// Height of the state box.
    /// </summary>
    public double StateHeight
    {
        get => _stateHeight;
        set => SetProperty(ref _stateHeight, value);
    }

    /// <summary>
    /// Whether the state is enabled for interaction.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// Creates a new state view model.
    /// </summary>
    public BlueprintStateViewModel()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Snaps the X position to the grid.
    /// </summary>
    public void SnapPositionX(double gridSize)
    {
        CanvasPositionX = Math.Round(CanvasPositionX / gridSize) * gridSize;
    }

    /// <summary>
    /// Snaps the Y position to the grid.
    /// </summary>
    public void SnapPositionY(double gridSize)
    {
        CanvasPositionY = Math.Round(CanvasPositionY / gridSize) * gridSize;
    }
}
