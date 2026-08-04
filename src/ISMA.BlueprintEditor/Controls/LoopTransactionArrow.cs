using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

public class LoopTransactionArrow : Control
{
    private LoopTransactionViewModel? _viewModel;
    private StateViewModel? _state;
    private EditArrowPopOver? _popOver;
    private Action<LoopTransactionViewModel>? _onClick;
    private Rect? _circleBounds;
    private Point? _arrowheadPosition;

    public LoopTransactionArrow()
    {
        AddHandler(PointerPressedEvent, LoopTransactionArrowPointerPressedHandler);
    }

    public LoopTransactionViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
        }
    }

    public Action<LoopTransactionViewModel>? OnClick
    {
        get => _onClick;
        set => _onClick = value;
    }

    public void UpdateLoopPosition(StateViewModel state)
    {
        _state = state;
        UpdateGeometry();
    }

    private void UpdateGeometry()
    {
        if (_state == null)
        {
            return;
        }

        var stateCenterX = _state.X + _state.SquareWidth / 2.0;
        var stateCenterY = _state.Y + _state.SquareHeight / 2.0;

        var radius = BlueprintEditorConstants.LoopCircleRadius;
        var circleCenterX = stateCenterX + BlueprintEditorConstants.LoopCircleCenterX;
        var circleCenterY = stateCenterY;

        _circleBounds = new Rect(
            circleCenterX - radius,
            circleCenterY - radius,
            radius * 2,
            radius * 2);

        _arrowheadPosition = new Point(circleCenterX, circleCenterY - radius);

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_circleBounds == null || _arrowheadPosition == null)
        {
            return;
        }

        var circle = _circleBounds.Value;
        var center = new Point(circle.X + circle.Width / 2, circle.Y + circle.Height / 2);
        var radius = circle.Width / 2;

        // Draw circle
        context.DrawEllipse(null, new Pen(Brushes.Black, BlueprintEditorConstants.ArrowLineStroke),
            center, radius, radius);

        // Draw arrowhead (downward triangle at top of circle)
        var halfWidth = BlueprintEditorConstants.ArrowheadWidth;
        var tip = _arrowheadPosition.Value;
        var base1 = new Point(tip.X + halfWidth, tip.Y + halfWidth * 2);
        var base2 = new Point(tip.X - halfWidth, tip.Y + halfWidth * 2);

        var arrowhead = new StreamGeometry();
        using var ctx = arrowhead.Open();
        ctx.BeginFigure(tip, false);
        ctx.LineTo(base1, true);
        ctx.LineTo(base2, true);
        ctx.EndFigure(true);

        context.DrawGeometry(Brushes.Black, new Pen(Brushes.Black, BlueprintEditorConstants.ArrowheadStroke), arrowhead);

        // Draw label
        var displayText = _viewModel?.DisplayText;
        if (!string.IsNullOrEmpty(displayText))
        {
            var formattedText = new FormattedText(
                displayText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                BlueprintEditorConstants.ArrowLabelFontSize,
                Brushes.Black);

            var labelX = center.X;
            var labelY = center.Y - radius - BlueprintEditorConstants.LoopLabelYOffset;
            var labelPos = new Point(labelX - formattedText.Width / 2, labelY);

            context.DrawText(formattedText, labelPos);
        }
    }

    private void LoopTransactionArrowPointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || _onClick == null || Parent == null)
        {
            e.Handled = true;
            return;
        }

        var position = e.GetPosition(this);

        // Check if click is near arrowhead
        if (_arrowheadPosition != null)
        {
            var dx = Math.Abs(position.X - _arrowheadPosition.Value.X);
            var dy = Math.Abs(position.Y - _arrowheadPosition.Value.Y);
            var halfWidth = BlueprintEditorConstants.ArrowheadWidth * 2;

            if (dx <= halfWidth && dy <= halfWidth)
            {
                var popPosition = e.GetPosition((Control)Parent!);
                var loopViewModel = _viewModel;
                _popOver = new EditArrowPopOver(new TransactionViewModel(loopViewModel!.StateName, loopViewModel.StateName, loopViewModel.Predicate, loopViewModel.Alias), (Control)Parent!, popPosition);
                ((Panel)Parent!).Children.Add(_popOver);

                _popOver.Closed += (_, _) =>
                {
                    if (Parent != null && ((Panel)Parent!).Children.Contains(_popOver!))
                    {
                        ((Panel)Parent!).Children.Remove(_popOver!);
                    }
                };

                e.Handled = true;
                return;
            }
        }

        e.Handled = true;
    }

    public void Cleanup()
    {
        RemoveHandler(PointerPressedEvent, LoopTransactionArrowPointerPressedHandler);
        _popOver?.Dispose();
    }
}
