using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintStateViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    private double _canvasPositionX;
    private double _canvasPositionY;

    public double CanvasPositionX
    {
        get => _canvasPositionX;
        set => SetProperty(ref _canvasPositionX, Math.Max(0, value));
    }

    public double CanvasPositionY
    {
        get => _canvasPositionY;
        set => SetProperty(ref _canvasPositionY, Math.Max(0, value));
    }

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private bool _isEditable;

    [ObservableProperty]
    private bool _isMain;

    [ObservableProperty]
    private bool _isInit;

    [ObservableProperty]
    private string? _fillColor = "#FF7F50";

    [ObservableProperty]
    private double _stateHeight;
}
