using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class BlueprintStateViewModel : ObservableObject
{
    [ObservableProperty]
    private double _canvasPositionX;

    [ObservableProperty]
    private double _canvasPositionY;

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
    private string _fillColorHex = "#F08080";
}
