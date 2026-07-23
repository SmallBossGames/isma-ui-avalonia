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
/// A canvas state box control with drag detection and click/disambiguation.
/// </summary>
public class StateBox : Control
{
    private const double DragThreshold = 3.0;
    private const double SingleClickDelay = 200.0;
    private const double StateWidth = 110.0;
    private const double BoxCornerRadius = 10.0;

    private bool _isDragging;
    private Point _pointerDownPosition;
    private double _pointerDownCanvasPositionX;
    private double _pointerDownCanvasPositionY;
    private DispatcherTimer? _singleClickTimer;
    private string? _previousName;
    private bool _timerFired;

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

    public static readonly StyledProperty<IBrush?> FillColorProperty =
        AvaloniaProperty.Register<StateBox, IBrush?>(nameof(FillColor));

    public IBrush? FillColor
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
    /// Raised when the state box is pressed.
    /// </summary>
    public event EventHandler<PointerEventArgs>? StatePressed;

    /// <summary>
    /// Raised when the state box is released after dragging.
    /// </summary>
    public event EventHandler<PointerEventArgs>? StateReleased;

    /// <summary>
    /// Raised when the state box is clicked (not dragged).
    /// </summary>
    public event EventHandler<RoutedEventArgs>? StateClicked;

    /// <summary>
    /// Raised when the state box is double-clicked.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? StateDoubleClicked;

    /// <summary>
    /// Raised when the inline name editor commits a new name.
    /// </summary>
    public event EventHandler<string?>? NameCommitted;

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
    /// Simulates a state press by invoking the ViewModel's OnStatePressed method.
    /// For testing purposes — bypasses the need to construct internal PointerEventArgs types.
    /// </summary>
    public void SimulateStatePressed(double positionX, double positionY)
    {
        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            var parent = Parent;
            while (parent is not null)
            {
                if (parent is Avalonia.Controls.UserControl uc &&
                    uc.DataContext is ISMA.ViewModels.ViewModels.BlueprintEditorViewModel editorVm)
                {
                    editorVm.OnStatePressed(stateVm, positionX, positionY);
                    return;
                }
                parent = parent.Parent;
            }
        }
    }

    /// <summary>
    /// Raises the NameCommitted event with the given new name. For testing purposes.
    /// </summary>
    public void RaiseNameCommitted(string? newName)
    {
        NameCommitted?.Invoke(this, newName);
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

        if (FillColor is not null)
        {
            context.FillRectangle(FillColor, rect, radius);
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
        _pointerDownCanvasPositionX = CanvasPositionX;
        _pointerDownCanvasPositionY = CanvasPositionY;
        _isDragging = false;
        _timerFired = false;

        _singleClickTimer?.Stop();
        _singleClickTimer?.Start();

        StatePressed?.Invoke(this, e);

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var position = e.GetPosition(this);
        var dx = position.X - _pointerDownPosition.X;
        var dy = position.Y - _pointerDownPosition.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (!_isDragging && distance > DragThreshold)
        {
            _isDragging = true;
            _singleClickTimer?.Stop();
            _pointerDownCanvasPositionX = CanvasPositionX;
            _pointerDownCanvasPositionY = CanvasPositionY;
        }

        if (_isDragging && DataContext is not null)
        {
            CanvasPositionX = Math.Max(0.0, _pointerDownCanvasPositionX + dx);
            CanvasPositionY = Math.Max(0.0, _pointerDownCanvasPositionY + dy);

            // Sync to ViewModel during drag for real-time arrow updates
            if (DataContext is BlueprintStateViewModel stateVm)
            {
                stateVm.CanvasPositionX = CanvasPositionX;
                stateVm.CanvasPositionY = CanvasPositionY;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            SyncPositionToViewModel();
            StateReleased?.Invoke(this, e);
            _isDragging = false;
            _pointerDownPosition = default;
        }
        else
        {
            _singleClickTimer?.Stop();

            if (!_timerFired)
            {
                StateClicked?.Invoke(this, new RoutedEventArgs());

                var position = e.GetPosition(this);
                var dx = position.X - _pointerDownPosition.X;
                var dy = position.Y - _pointerDownPosition.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= DragThreshold)
                {
                    SelectState();
                }
            }

            _isDragging = false;
            _pointerDownPosition = default;
        }

        e.Handled = true;
    }

    private void SelectState()
    {
        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            stateVm.IsSelected = true;
        }
    }

    private void SyncPositionToViewModel()
    {
        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            stateVm.CanvasPositionX = CanvasPositionX;
            stateVm.CanvasPositionY = CanvasPositionY;
        }
    }

    private void OnSingleClickTimerTick(object? sender, EventArgs e)
    {
        _singleClickTimer?.Stop();

        if (_isDragging)
            return;

        _timerFired = true;

        if (!IsEditable)
            return;

        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            if (stateVm.IsMain || stateVm.IsInit)
                return;
        }

        EnterInlineEditMode();
    }

    private Panel? _inlineEditPanel;
    private TextBox? _inlineEditTextBox;

    private void EnterInlineEditMode()
    {
        _previousName = Name;

        var parent = Parent;
        while (parent is not null)
        {
            if (parent is Panel panel)
            {
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

                panel.Children.Insert(0, textBox);
                _inlineEditPanel = panel;
                _inlineEditTextBox = textBox;
                _singleClickTimer?.Stop();
                textBox.Focus();
                textBox.SelectAll();
                return;
            }
            parent = parent.Parent;
        }
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

        NameCommitted?.Invoke(this, newName);

        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            stateVm.Name = newName ?? _previousName ?? "";
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

        if (_inlineEditPanel != null)
        {
            _inlineEditPanel.Children.Remove(textBox);
        }

        _inlineEditPanel = null;
        _inlineEditTextBox = null;
        _previousName = null;
    }

    private TextBox? FindTextBox()
    {
        if (_inlineEditTextBox != null && _inlineEditPanel != null)
        {
            if (_inlineEditPanel.Children.Contains(_inlineEditTextBox))
                return _inlineEditTextBox;
        }
        return null;
    }
}
