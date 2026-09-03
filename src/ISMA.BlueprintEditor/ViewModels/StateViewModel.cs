using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// A state (node) in the blueprint diagram. The name is the identity of the state
/// and is gated by a uniqueness check supplied at construction.
/// </summary>
public partial class StateViewModel : ObservableObject
{
    public StateViewModel(
        string name,
        string text,
        double x,
        double y,
        double squareWidth,
        double squareHeight,
        StateKind kind,
        Func<string, bool> isNameUnique)
    {
        Kind = kind;
        IsNameUnique = isNameUnique;
        Name = name;
        Text = text;
        X = x;
        Y = y;
        SquareWidth = squareWidth;
        SquareHeight = squareHeight;
    }

    /// <summary>Kind of the state; determines color and editability protections.</summary>
    public StateKind Kind { get; }

    /// <summary>Uniqueness check for candidate names.</summary>
    public Func<string, bool> IsNameUnique { get; }

    private string name = string.Empty;

    /// <summary>
    /// Name of the state (its identity). Changes are gated: a value equal to the
    /// current one is a no-op, and any other value is accepted only if
    /// <see cref="IsNameUnique"/> allows it.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            if (value == name)
            {
                return;
            }

            if (!IsNameUnique(value))
            {
                return;
            }

            name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    /// <summary>
    /// Margin that positions the state box inside the diagram canvas
    /// (left/top = state X/Y). Recomputed whenever X or Y changes.
    /// </summary>
    public Thickness BoxMargin => new(X, Y, 0, 0);

    partial void OnXChanged(double value) => OnPropertyChanged(nameof(BoxMargin));

    partial void OnYChanged(double value) => OnPropertyChanged(nameof(BoxMargin));

    [ObservableProperty]
    private double squareWidth;

    [ObservableProperty]
    private double squareHeight;

    [ObservableProperty]
    private bool editMode;

    /// <summary>X of the state box center.</summary>
    public double CenterX => X + SquareWidth / 2;

    /// <summary>Y of the state box center.</summary>
    public double CenterY => Y + SquareHeight / 2;

    /// <summary>Enters name-edit mode (shows the edit overlay).</summary>
    public void StartEdit()
    {
        EditMode = true;
    }

    /// <summary>Leaves name-edit mode.</summary>
    public void CommitEdit()
    {
        EditMode = false;
    }
}
