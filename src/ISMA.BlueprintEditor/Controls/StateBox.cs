using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.Utilities;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// A state (node) in the blueprint diagram: a rounded, filled box with a name
/// label and an in-place name edit overlay. Ported from the original
/// Kotlin/JavaFX <c>StateBox</c> (visual tree built in code, drag with grab
/// offset clamped to the canvas, 200 ms single/double click disambiguation).
/// Position is owned by the bound <c>Margin</c> (state X/Y); this control only
/// owns its size, appearance and interaction.
/// </summary>
public class StateBox : Decorator
{
    private const double DragThreshold = 2.0;

    private readonly Border _border;
    private readonly TextBlock _nameLabel;
    private readonly TextBox _nameEditor;
    private readonly ClickDisambiguator _disambiguator;

    private StateViewModel? _state;
    private Visual? _dragSpace;
    private Point _pressPos;
    private double _initialX;
    private double _initialY;
    private bool _dragDetected;
    private bool _commitHandled;

    /// <summary>Identifies the state carried by <see cref="StateBox"/> click events.</summary>
    public StateBox()
    {
        _nameLabel = new TextBlock
        {
            FontSize = BlueprintEditorConstants.StateNameFontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _nameEditor = new TextBox
        {
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _nameEditor.GotFocus += (_, _) =>
        {
            if (State is { } state)
            {
                _nameEditor.Text = state.Name;
            }
        };

        _nameEditor.LostFocus += (_, _) =>
        {
            if (_commitHandled)
            {
                _commitHandled = false;
                return;
            }

            if (State is { } state)
            {
                RaiseEvent(new NameCommittedRoutedEventArgs(NameCommittedEvent, state, _nameEditor.Text ?? string.Empty));
            }
        };

        _nameEditor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                _commitHandled = true;
                if (State is { } state)
                {
                    RaiseEvent(new NameCommittedRoutedEventArgs(NameCommittedEvent, state, _nameEditor.Text ?? string.Empty));
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _commitHandled = true;
                State?.CommitEdit();
            }
        };

        var content = new Grid
        {
            Margin = new Thickness(BlueprintEditorConstants.StateInset),
            Children = { _nameLabel, _nameEditor },
        };

        _border = new Border
        {
            CornerRadius = new CornerRadius(BlueprintEditorConstants.CornerRadius),
            Child = content,
        };

        Child = _border;

        _disambiguator = new ClickDisambiguator(
            onSingleClick: HandleSingleClick,
            onDoubleClick: HandleDoubleClick,
            clickDelayMs: (int)BlueprintEditorConstants.ClickDelayMs);
    }

    /// <summary>The view model of the state this box renders.</summary>
    public static readonly StyledProperty<StateViewModel?> StateProperty =
        AvaloniaProperty.Register<StateBox, StateViewModel?>(nameof(State));

    /// <summary>The view model of the state this box renders.</summary>
    public StateViewModel? State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>Fill brush of the state box (by state kind).</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<StateBox, IBrush?>(nameof(Fill));

    /// <summary>Fill brush of the state box (by state kind).</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Raised on a (disambiguated) single click on the box.</summary>
    public static readonly RoutedEvent<StateBoxRoutedEventArgs> SingleClickEvent =
        RoutedEvent<StateBoxRoutedEventArgs>.Register<StateBox, StateBoxRoutedEventArgs>(nameof(SingleClick), RoutingStrategies.Bubble);

    /// <summary>Raised on a (disambiguated) single click on the box.</summary>
    public event EventHandler<StateBoxRoutedEventArgs> SingleClick
    {
        add => AddHandler(SingleClickEvent, value);
        remove => RemoveHandler(SingleClickEvent, value);
    }

    /// <summary>Raised on a double click on the box.</summary>
    public static readonly RoutedEvent<StateBoxRoutedEventArgs> DoubleClickEvent =
        RoutedEvent<StateBoxRoutedEventArgs>.Register<StateBox, StateBoxRoutedEventArgs>(nameof(DoubleClick), RoutingStrategies.Bubble);

    /// <summary>Raised on a double click on the box.</summary>
    public event EventHandler<StateBoxRoutedEventArgs> DoubleClick
    {
        add => AddHandler(DoubleClickEvent, value);
        remove => RemoveHandler(DoubleClickEvent, value);
    }

    /// <summary>Raised when the user commits an edited name (focus loss or Enter).</summary>
    public static readonly RoutedEvent<NameCommittedRoutedEventArgs> NameCommittedEvent =
        RoutedEvent<NameCommittedRoutedEventArgs>.Register<StateBox, NameCommittedRoutedEventArgs>(nameof(NameCommitted), RoutingStrategies.Bubble);

    /// <summary>Raised when the user commits an edited name (focus loss or Enter).</summary>
    public event EventHandler<NameCommittedRoutedEventArgs> NameCommitted
    {
        add => AddHandler(NameCommittedEvent, value);
        remove => RemoveHandler(NameCommittedEvent, value);
    }

    static StateBox()
    {
        StateProperty.Changed.AddClassHandler<StateBox, StateViewModel?>(
            (box, _) => box.OnStateChanged());
        FillProperty.Changed.AddClassHandler<StateBox, IBrush?>(
            (box, e) => box._border.Background = e.NewValue.Value);
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (State is not { } state)
        {
            return;
        }

        _disambiguator.OnKeyPress();
        _dragSpace = FindDragSpace();
        _pressPos = _dragSpace is null ? default(Point) : e.GetPosition(_dragSpace);
        _initialX = state.X;
        _initialY = state.Y;
        _dragDetected = false;
        e.Pointer.Capture(this);
        _disambiguator.OnClick(e.ClickCount);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (State is not { } state || _dragSpace is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pos = e.GetPosition(_dragSpace);
        double dx = pos.X - _pressPos.X;
        double dy = pos.Y - _pressPos.Y;

        if (!_dragDetected && (Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold))
        {
            _dragDetected = true;
            _disambiguator.OnDragged();
        }

        if (_dragDetected)
        {
            state.X = Math.Max(0, _initialX + dx);
            state.Y = Math.Max(0, _initialY + dy);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
    }

    /// <summary>
    /// Finds a stable ancestor coordinate space for drag deltas: the nearest
    /// canvas or top-level (the item container itself moves with the state
    /// and must be skipped).
    /// </summary>
    private Visual? FindDragSpace()
    {
        for (var cur = this.GetVisualParent(); cur is not null; cur = cur.GetVisualParent())
        {
            if (cur is Views.BlueprintCanvas || cur is TopLevel)
            {
                return cur;
            }
        }

        return this.GetVisualParent();
    }

    private void OnStateChanged()
    {
        if (_state is { } old)
        {
            old.PropertyChanged -= OnStatePropertyChanged;
        }

        _state = State;

        if (_state is not { } state)
        {
            return;
        }

        state.PropertyChanged += OnStatePropertyChanged;
        ApplyState(state);
    }

    private void OnStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (State is not { } state)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(StateViewModel.SquareWidth):
                Width = state.SquareWidth;
                break;
            case nameof(StateViewModel.SquareHeight):
                Height = state.SquareHeight;
                break;
            case nameof(StateViewModel.Name):
                _nameLabel.Text = state.Name;
                break;
            case nameof(StateViewModel.EditMode):
                ApplyEditMode(state);
                break;
        }
    }

    private void ApplyState(StateViewModel state)
    {
        Width = state.SquareWidth;
        Height = state.SquareHeight;
        _border.Background = Fill;
        _nameLabel.Text = state.Name;
        ApplyEditMode(state);
    }

    private void ApplyEditMode(StateViewModel state)
    {
        _nameEditor.IsVisible = state.EditMode;
        _nameLabel.IsVisible = !state.EditMode;
        if (state.EditMode)
        {
            _nameEditor.Text = state.Name;
        }
    }

    private void HandleSingleClick()
    {
        if (State is not { } state)
        {
            return;
        }

        RaiseEvent(new StateBoxRoutedEventArgs(SingleClickEvent, state));
        if (state.EditMode)
        {
            _nameEditor.Focus();
        }
    }

    private void HandleDoubleClick()
    {
        if (State is { } state)
        {
            RaiseEvent(new StateBoxRoutedEventArgs(DoubleClickEvent, state));
        }
    }
}

/// <summary>
/// Routed event args carrying the <see cref="StateViewModel"/> of a clicked state box.
/// </summary>
/// <param name="RoutedEvent">The routed event.</param>
/// <param name="State">The clicked state.</param>
public class StateBoxRoutedEventArgs(RoutedEvent routedEvent, StateViewModel state) : RoutedEventArgs(routedEvent)
{
    /// <summary>The clicked state.</summary>
    public StateViewModel State { get; } = state;
}

/// <summary>
/// Routed event args for a committed state name edit.
/// </summary>
/// <param name="RoutedEvent">The routed event.</param>
/// <param name="State">The state being renamed.</param>
/// <param name="NewName">The name typed by the user.</param>
public sealed class NameCommittedRoutedEventArgs(RoutedEvent routedEvent, StateViewModel state, string newName)
    : StateBoxRoutedEventArgs(routedEvent, state)
{
    /// <summary>The name typed by the user.</summary>
    public string NewName { get; } = newName;
}
