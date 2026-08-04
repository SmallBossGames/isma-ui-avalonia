using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.Utilities;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

public class StateBox : Control
{
    private readonly ClickDisambiguator _clickDisambiguator;
    private StateViewModel? _viewModel;
    private Action<StateViewModel>? _onClick;
    private bool _isDragging;
    private Point _dragStart;
    private Point _clickOffset;
    private Canvas? _inlineEditCanvas;
    private Canvas? _inlineEditOverlay;
    private TextBox? _inlineEditTextBox;
    private SolidColorBrush? _colorBrush;

    public StateBox()
    {
        _clickDisambiguator = new ClickDisambiguator(
            BlueprintEditorConstants.ClickDelayMs,
            () => OnSingleClick(),
            () => OnDoubleClick());

        AddHandler(PointerPressedEvent, PointerPressedHandler);
        AddHandler(PointerReleasedEvent, PointerReleasedHandler);
        AddHandler(PointerMovedEvent, PointerMovedHandler);
    }

    public StateViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            InvalidateVisual();
        }
    }

    public Action<StateViewModel>? OnClick
    {
        get => _onClick;
        set => _onClick = value;
    }

    public override void Render(DrawingContext context)
    {
        if (_viewModel == null || _colorBrush == null)
        {
            return;
        }

        var width = _viewModel.SquareWidth;
        var height = _viewModel.SquareHeight;
        var rect = new Rect(0, 0, width, height);

        var cornerRadius = (float)BlueprintEditorConstants.CornerRadius;
        context.FillRectangle(_colorBrush, rect, cornerRadius);
        context.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), rect, cornerRadius);

        // Draw state name
        var text = _viewModel.Name;
        if (!string.IsNullOrEmpty(text))
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                BlueprintEditorConstants.StateNameFontSize,
                Brushes.Black);

            var textRect = new Rect(0, 0, width, height);
            var textPosition = new Point(
                textRect.X + (textRect.Width - formattedText.Width) / 2,
                textRect.Y + (textRect.Height - formattedText.Height) / 2);

            context.DrawText(formattedText, textPosition);
        }
    }

    private void UpdateColorBrush()
    {
        if (_viewModel == null)
        {
            _colorBrush = null;
            return;
        }

        _colorBrush = GetBrushFromColor(_viewModel.Color);
    }

    private static SolidColorBrush GetBrushFromColor(string colorName)
    {
        return colorName.ToLowerInvariant() switch
        {
            "lightgreen" => new SolidColorBrush(Color.FromRgb(144, 238, 144)),
            "lightblue" => new SolidColorBrush(Color.FromRgb(173, 216, 230)),
            _ => new SolidColorBrush(Color.FromRgb(255, 127, 80))
        };
    }

    private void PointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        _clickDisambiguator.OnPointerPressed();
        _dragStart = e.GetPosition(this);

        // If already in edit mode, focus the textbox
        if (_inlineEditTextBox != null)
        {
            _inlineEditTextBox.Focus();
            _inlineEditTextBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (_viewModel != null && _viewModel.Editable)
        {
            _isDragging = true;
            var position = e.GetPosition(this);
            _clickOffset = new Point(position.X, position.Y);
        }

        e.Handled = true;
    }

    private void PointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
    {
        _clickDisambiguator.OnPointerReleased();
        _isDragging = false;
        e.Handled = true;
    }

    private void PointerMovedHandler(object? sender, PointerEventArgs e)
    {
        if (_isDragging && _viewModel != null && Parent is Panel parent)
        {
            _clickDisambiguator.OnPointerMoved();
            var position = e.GetPosition(parent);
            var newX = Math.Max(0, position.X - _clickOffset.X);
            var newY = Math.Max(0, position.Y - _clickOffset.Y);
            _viewModel.X = newX;
            _viewModel.Y = newY;
        }
    }

    private void OnSingleClick()
    {
        if (_viewModel == null)
        {
            return;
        }

        if (_viewModel.Editable)
        {
            EnterInlineEditMode();
        }

        _onClick?.Invoke(_viewModel);
    }

    private void OnDoubleClick()
    {
        if (_viewModel == null)
        {
            return;
        }

        _onClick?.Invoke(_viewModel);
    }

    private void EnterInlineEditMode()
    {
        var canvas = Parent;
        while (canvas is not null && canvas is not Canvas)
        {
            canvas = canvas.Parent;
        }

        if (canvas is not Canvas parentCanvas)
            return;

        var textBox = new TextBox
        {
            Text = _viewModel?.Name,
            FontSize = BlueprintEditorConstants.StateNameFontSize,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = _viewModel?.SquareWidth ?? BlueprintEditorConstants.DefaultStateWidth,
            MaxWidth = _viewModel?.SquareWidth ?? BlueprintEditorConstants.DefaultStateWidth
        };

        textBox.KeyDown += OnTextBoxKeyDown;
        textBox.LostFocus += OnTextBoxLostFocus;

        var overlay = new Canvas
        {
            Background = null
        };

        if (_viewModel != null)
        {
            Canvas.SetLeft(overlay, _viewModel.X);
            Canvas.SetTop(overlay, _viewModel.Y);
        }

        overlay.ZIndex = 10;
        overlay.Children.Add(textBox);

        _inlineEditCanvas = parentCanvas;
        _inlineEditOverlay = overlay;
        _inlineEditTextBox = textBox;
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
        var textBox = _inlineEditTextBox;
        if (textBox == null || _viewModel == null)
            return;

        var newName = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            newName = _viewModel.Name;
        }

        _viewModel.CommitEdit(newName);
        ExitInlineEditMode(textBox);
    }

    private void CancelName()
    {
        var textBox = _inlineEditTextBox;
        if (textBox == null || _viewModel == null)
            return;

        _viewModel.CancelEdit();
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
    }

    public void Cleanup()
    {
        if (_inlineEditTextBox != null)
        {
            ExitInlineEditMode(_inlineEditTextBox);
        }

        RemoveHandler(PointerPressedEvent, PointerPressedHandler);
        RemoveHandler(PointerReleasedEvent, PointerReleasedHandler);
        RemoveHandler(PointerMovedEvent, PointerMovedHandler);
    }
}
