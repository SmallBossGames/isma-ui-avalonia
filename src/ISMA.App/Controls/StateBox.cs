using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace ISMA.App.Controls;

/// <summary>
/// A canvas state box control with drag detection, click/disambiguation, and inline name editing.
/// </summary>
public class StateBox : ContentControl
{
    private const double DragThreshold = 3.0;
    private const double SingleClickDelay = 200.0;
    private const double StateWidth = 110.0;
    private const double BoxCornerRadius = 10.0;

    private bool _isDragging;
    private Point _pointerDownPosition;
    private DispatcherTimer? _singleClickTimer;
    private TextBlock? _textBlock;
    private TextBox? _textBox;
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

    public new static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<StateBox, bool>(nameof(IsEnabled), defaultValue: true);

    public new bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
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

    public double CenterX => CanvasPositionX + StateWidth / 2;

    public double CenterY => CanvasPositionY + (StateHeight > 0 ? StateHeight : StateWidth) / 2;

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
            // Find the parent BlueprintEditorView to get its ViewModel
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
        AffectsRender<StateBox>(FillColorProperty, StateHeightProperty);
    }

    public StateBox()
    {
        _singleClickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SingleClickDelay) };
        _singleClickTimer.Tick += OnSingleClickTimerTick;
        BuildVisualTree();
    }



    private void BuildVisualTree()
    {
        var grid = new Grid
        {
            Width = StateWidth,
            Height = StateHeight > 0 ? StateHeight : StateWidth
        };

        var rect = new Border
        {
            Background = FillColor,
            BorderBrush = null,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(BoxCornerRadius)
        };

        _textBlock = new TextBlock
        {
            Text = Name,
            FontFamily = new FontFamily("Arial"),
            FontWeight = Avalonia.Media.FontWeight.Bold,
            FontSize = 16,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        grid.Children.Add(rect);
        grid.Children.Add(_textBlock);
        Content = grid;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NameProperty && _textBlock != null)
        {
            _textBlock.Text = change.NewValue as string;
        }

        if (change.Property == FillColorProperty && Content is Grid grid)
        {
            if (grid.Children[0] is Border border)
            {
                border.Background = change.NewValue as IBrush;
            }
        }

        if (change.Property == StateHeightProperty && Content is Grid g)
        {
            g.Height = change.NewValue as double? ?? StateWidth;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var position = e.GetPosition(this);

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

        _pointerDownPosition = position;
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

        if (_isDragging)
        {
            var position = e.GetPosition(this);
            var dx = position.X - _pointerDownPosition.X;
            var dy = position.Y - _pointerDownPosition.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > DragThreshold)
            {
                _isDragging = true;
                _singleClickTimer?.Stop();
            }

            if (DataContext is not null)
            {
                var newX = Math.Max(0.0, position.X - (_pointerDownPosition.X - CanvasPositionX));
                var newY = Math.Max(0.0, position.Y - (_pointerDownPosition.Y - CanvasPositionY));
                CanvasPositionX = newX;
                CanvasPositionY = newY;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
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

    private void EnterInlineEditMode()
    {
        if (Content is not Grid grid)
            return;

        _previousName = Name;

        _textBox = new TextBox
        {
            Text = Name,
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Width = 90,
            MaxWidth = 90
        };

        _textBox.KeyDown += OnTextBoxKeyDown;
        _textBox.LostFocus += OnTextBoxLostFocus;

        if (_textBlock is not null)
            grid.Children.Remove(_textBlock);
        grid.Children.Add(_textBox);
        _textBox.Focus();
        _textBox.SelectAll();
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
        if (_textBox == null)
            return;

        var newName = _textBox.Text?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            newName = _previousName;
        }

        NameCommitted?.Invoke(this, newName);

        if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
        {
            stateVm.Name = newName ?? _previousName ?? "";
        }

        ExitInlineEditMode();
    }

    private void CancelName()
    {
        if (!string.IsNullOrEmpty(_previousName))
        {
            if (DataContext is ISMA.ViewModels.ViewModels.BlueprintStateViewModel stateVm)
            {
                stateVm.Name = _previousName;
            }
        }

        ExitInlineEditMode();
    }

    private void ExitInlineEditMode()
    {
        if (Content is not Grid grid || _textBox == null)
            return;

        _textBox.KeyDown -= OnTextBoxKeyDown;
        _textBox.LostFocus -= OnTextBoxLostFocus;

        grid.Children.Remove(_textBox);
        grid.Children.Add(_textBlock!);
        _textBox = null;
        _previousName = null;
    }
}
