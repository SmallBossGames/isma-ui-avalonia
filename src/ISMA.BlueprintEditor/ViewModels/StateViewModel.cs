using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

public partial class StateViewModel : ObservableObject
{
    private Func<string, bool>? _isNameUnique;

    public StateViewModel(
        string name,
        string text,
        double x,
        double y,
        double squareWidth,
        double squareHeight,
        string color,
        bool editable)
    {
        Name = name;
        Text = text;
        X = x;
        Y = y;
        SquareWidth = squareWidth;
        SquareHeight = squareHeight;
        Color = color;
        Editable = editable;
        EditButtonVisible = editable;
    }

    public Func<string, bool>? IsNameUnique
    {
        get => _isNameUnique;
        set => SetProperty(ref _isNameUnique, value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string _text;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _squareWidth;

    [ObservableProperty]
    private double _squareHeight;

    [ObservableProperty]
    private string _color;

    [ObservableProperty]
    private bool _editable;

    [ObservableProperty]
    private bool _editMode;

    [ObservableProperty]
    private bool _editButtonVisible;

    public double CenterX => X + SquareWidth / 2.0;

    public double CenterY => Y + SquareHeight / 2.0;

    public string DisplayText => Name;

    partial void OnNameChanging(string? oldValue, string newValue)
    {
        if (IsNameUnique != null && !string.IsNullOrEmpty(newValue) && !IsNameUnique(newValue))
        {
            _name = oldValue ?? _name;
        }
    }

    public void StartEdit()
    {
        EditMode = true;
        EditButtonVisible = false;
    }

    public void CommitEdit(string newName)
    {
        Name = newName;
        EditMode = false;
        EditButtonVisible = Editable;
    }

    public void CancelEdit()
    {
        EditMode = false;
        EditButtonVisible = Editable;
    }
}
