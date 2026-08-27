using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Controls;

/// <summary>
/// Event arguments for the <see cref="StateBox.NameCommitted"/> event.
/// </summary>
/// <param name="newName">The proposed new name for the state.</param>
/// <param name="handled">Set to <see langword="true"/> to reject the name change.</param>
public sealed class StateNameCommittedEventArgs(string? newName, bool handled) : EventArgs
{
    /// <summary>
    /// Gets the proposed new name for the state.
    /// </summary>
    public string? NewName { get; } = newName;

    /// <summary>
    /// Gets or sets whether the name change was handled (rejected).
    /// Set to <see langword="true"/> to prevent the control from committing the name.
    /// </summary>
    public bool Handled { get; set; } = handled;
}

/// <summary>
/// A canvas state box control with drag detection and click/disambiguation.
/// </summary>
public class StateBox : Control
{
    private const double DragThreshold = 3.0;
    private const double SingleClickDelay = 200.0;
    private const double StateWidth = 110.0;
    private const double BoxCornerRadius = 20.0;

    private Point _pointerDownPosition;
    private Point _dragStartCanvasPosition;
    private bool _wasDragging;
    private Avalonia.Input.IPointer? _pointerDownPointer;
    private DispatcherTimer? _singleClickTimer;
    private string? _previousName;

    public new static readonly StyledProperty<string?> NameProperty =
        AvaloniaProperty.Register<StateBox, string?>(nameof(Name));

    public new string? Name
    {
        get => GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<StateBox, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string?> FillColorProperty =
        AvaloniaProperty.Register<StateBox, string?>(nameof(FillColor));

    public string? FillColor
    {
        get => GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public static readonly StyledProperty<bool> IsEditableProperty =
        AvaloniaProperty.Register<StateBox, bool>(nameof(IsEditable), defaultValue: false);

    public bool IsEditable
    {
        get => GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    public static readonly StyledProperty<double> StateHeightProperty =
        AvaloniaProperty.Register<StateBox, double>(nameof(StateHeight), defaultValue: 65.0);

    public double StateHeight
    {
        get => GetValue(StateHeightProperty);
        set => SetValue(StateHeightProperty, value);
    }

    public static readonly StyledProperty<double> CanvasPositionXProperty =
        AvaloniaProperty.Register<StateBox, double>(nameof(CanvasPositionX));

    public double CanvasPositionX
    {
        get => GetValue(CanvasPositionXProperty);
        set => SetValue(CanvasPositionXProperty, value);
    }

    public static readonly StyledProperty<double> CanvasPositionYProperty =
        AvaloniaProperty.Register<StateBox, double>(nameof(CanvasPositionY));

    public double CanvasPositionY
    {
        get => GetValue(CanvasPositionYProperty);
        set => SetValue(CanvasPositionYProperty, value);
    }

    public static readonly StyledProperty<Guid?> IdProperty =
        AvaloniaProperty.Register<StateBox, Guid?>(nameof(Id));

    public Guid? Id
    {
        get => GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public double CenterX => CanvasPositionX + (base.Width > 0 ? base.Width : StateWidth) / 2;

    public double CenterY => CanvasPositionY + (StateHeight > 0 ? StateHeight : base.Width > 0 ? base.Width : StateWidth) / 2;

    /// <summary>
    /// Raised when the state box is clicked (single click, not dragged, after click disambiguation).
    /// </summary>
    public event EventHandler<RoutedEventArgs>? StateClicked;

    /// <summary>
    /// Raised when the state box is double-clicked.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? StateDoubleClicked;

    /// <summary>
    /// Raised when the inline name editor commits a new name.
    /// Set <see cref="StateNameCommittedEventArgs.Handled"/> to <see langword="true"/> to reject the name change.
    /// </summary>
    public event EventHandler<StateNameCommittedEventArgs>? NameCommitted;

    /// <summary>
    /// Raises the StateClicked event. For testing purposes.
    /// </summary>
    public void RaiseStateClicked()
    {
        StateClicked?.Invoke(this, new RoutedEventArgs());
    }

    /// <summary>
    /// Raises the StateDoubleClicked event. For testing purposes.
    /// </summary>
    public void RaiseStateDoubleClicked()
    {
        StateDoubleClicked?.Invoke(this, new RoutedEventArgs());
    }

    /// <summary>
    /// Raises the NameCommitted event with the given new name. For testing purposes.
    /// </summary>
    /// <param name="newName">The proposed new name.</param>
    /// <returns><see langword="true"/> if the name change was rejected (Handled); <see langword="false"/> otherwise.</returns>
    public bool RaiseNameCommitted(string? newName)
    {
        var args = new StateNameCommittedEventArgs(newName, false);
        NameCommitted?.Invoke(this, args);
        return args.Handled;
    }

    static StateBox()
    {
        AffectsRender<StateBox>(FillColorProperty, NameProperty, StateHeightProperty, CanvasPositionXProperty, CanvasPositionYProperty);
    }

    public StateBox()
    {
        _singleClickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SingleClickDelay) };
        _singleClickTimer.Tick += OnSingleClickTimerTick;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _singleClickTimer?.Stop();
    }

    public override void Render(DrawingContext context)
    {
        var width = base.Width > 0 ? base.Width : StateWidth;
        var height = StateHeight > 0 ? StateHeight : width;
        var rect = new Rect(0, 0, width, height);
        var radius = (float)BoxCornerRadius;

        if (!string.IsNullOrEmpty(FillColor) && Avalonia.Media.Color.TryParse(FillColor, out var fillColor))
        {
            context.FillRectangle(new Avalonia.Media.SolidColorBrush(fillColor), rect, radius);
        }

        context.DrawRectangle(Avalonia.Media.Brushes.Black, new Pen(Avalonia.Media.Brushes.Black, 1), rect, radius);

        var text = Name ?? "";
        if (!string.IsNullOrEmpty(text))
        {
            var formattedText = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 16, Avalonia.Media.Brushes.Black);
            var textRect = new Rect(0, 0, width, height);
            var textPosition = new Point(
                textRect.X + (textRect.Width - formattedText.Width) / 2,
                textRect.Y + (textRect.Height - formattedText.Height) / 2);
            context.DrawText(formattedText, textPosition);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.ClickCount > 1)
        {
            _singleClickTimer?.Stop();
            StateDoubleClicked?.Invoke(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }
        if (!IsEnabled)
        {
            e.Handled = true;
            return;
        }
        _pointerDownPosition = e.GetPosition(this);
        _pointerDownPointer = e.Pointer;
        _wasDragging = false;
        _singleClickTimer?.Stop();
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointerDownPointer != e.Pointer || _pointerDownPointer is null)
            return;

        var localPos = e.GetPosition(this);
        if (!_wasDragging)
        {
            var dx = localPos.X - _pointerDownPosition.X;
            var dy = localPos.Y - _pointerDownPosition.Y;
            if (Math.Abs(dx) < DragThreshold && Math.Abs(dy) < DragThreshold)
                return;
            _wasDragging = true;
            if (DataContext is not BlueprintStateViewModel startVm)
                return;
            _dragStartCanvasPosition = new Point(startVm.CanvasPositionX, startVm.CanvasPositionY);
        }

        if (DataContext is not BlueprintStateViewModel stateVm)
            return;

        stateVm.CanvasPositionX = Math.Max(0.0, _dragStartCanvasPosition.X + (localPos.X - _pointerDownPosition.X));
        stateVm.CanvasPositionY = Math.Max(0.0, _dragStartCanvasPosition.Y + (localPos.Y - _pointerDownPosition.Y));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _pointerDownPointer?.Capture(null);
        if (!_wasDragging)
        {
            _singleClickTimer?.Stop();
            _singleClickTimer?.Start();
        }
        _wasDragging = false;
        _pointerDownPosition = default;
        _pointerDownPointer = null;
        e.Handled = true;
    }

    private void OnSingleClickTimerTick(object? sender, EventArgs e)
    {
        _singleClickTimer?.Stop();

        StateClicked?.Invoke(this, new RoutedEventArgs());

        if (!IsEditable)
            return;

        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            if (stateVm.IsMain || stateVm.IsInit)
                return;
        }

        EnterInlineEditMode();
    }

    private Canvas? _inlineEditCanvas;
    private Canvas? _inlineEditOverlay;
    private TextBox? _inlineEditTextBox;

    private void EnterInlineEditMode()
    {
        _previousName = Name;

        var canvas = Parent;
        while (canvas is not null && canvas is not Canvas)
        {
            canvas = canvas.Parent;
        }

        if (canvas is not Canvas parentCanvas)
            return;

        var textBox = new TextBox
        {
            Text = Name,
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Width = 90,
            MaxWidth = 90
        };

        textBox.KeyDown += OnTextBoxKeyDown;
        textBox.LostFocus += OnTextBoxLostFocus;

        var overlay = new Canvas
        {
            Background = null
        };
        Canvas.SetLeft(overlay, CanvasPositionX);
        Canvas.SetTop(overlay, CanvasPositionY);
        overlay.ZIndex = 10;
        overlay.Children.Add(textBox);

        _inlineEditCanvas = parentCanvas;
        _inlineEditOverlay = overlay;
        _inlineEditTextBox = textBox;
        _singleClickTimer?.Stop();
        parentCanvas.Children.Add(overlay);
        textBox.Focus();
        textBox.SelectAll();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitName();
        }
        else if (e.Key == Key.Escape)
        {
            CancelName();
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitName();
    }

    private void CommitName()
    {
        var textBox = FindTextBox();
        if (textBox == null)
            return;

        var newName = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            newName = _previousName;
        }

        var args = new StateNameCommittedEventArgs(newName, false);
        NameCommitted?.Invoke(this, args);

        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            if (args.Handled)
            {
                stateVm.Name = _previousName ?? "";
            }
            else
            {
                stateVm.Name = newName ?? _previousName ?? "";
            }
        }

        ExitInlineEditMode(textBox);
    }

    private void CancelName()
    {
        var textBox = FindTextBox();
        if (textBox == null)
            return;

        if (!string.IsNullOrEmpty(_previousName))
        {
            if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
            {
                stateVm.Name = _previousName;
            }
        }

        ExitInlineEditMode(textBox);
    }

    private void ExitInlineEditMode(TextBox textBox)
    {
        textBox.KeyDown -= OnTextBoxKeyDown;
        textBox.LostFocus -= OnTextBoxLostFocus;

        if (_inlineEditOverlay != null && _inlineEditCanvas != null)
        {
            _inlineEditCanvas.Children.Remove(_inlineEditOverlay);
        }

        _inlineEditCanvas = null;
        _inlineEditOverlay = null;
        _inlineEditTextBox = null;
        _previousName = null;
    }

    private TextBox? FindTextBox()
    {
        if (_inlineEditTextBox != null && _inlineEditOverlay != null && _inlineEditCanvas != null)
        {
            if (_inlineEditCanvas.Children.Contains(_inlineEditOverlay) &&
                _inlineEditOverlay.Children.Contains(_inlineEditTextBox))
                return _inlineEditTextBox;
        }
        return null;
    }
}
