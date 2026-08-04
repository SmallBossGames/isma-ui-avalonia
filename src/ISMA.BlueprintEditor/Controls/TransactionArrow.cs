using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.Utilities;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

public class TransactionArrow : Control
{
    private TransactionViewModel? _viewModel;
    private StateViewModel? _startState;
    private StateViewModel? _endState;
    private EditArrowPopOver? _popOver;
    private Action<TransactionViewModel>? _onClick;
    private ArrowGeometry? _geometry;

    public TransactionArrow()
    {
        AddHandler(PointerPressedEvent, TransactionArrowPointerPressedHandler);
    }

    public TransactionViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
        }
    }

    public Action<TransactionViewModel>? OnClick
    {
        get => _onClick;
        set => _onClick = value;
    }

    public void UpdateArrowPositions(StateViewModel startState, StateViewModel endState)
    {
        _startState = startState;
        _endState = endState;
        UpdateGeometry();
    }

    private void UpdateGeometry()
    {
        if (_startState == null || _endState == null)
        {
            return;
        }

        var startX = _startState.X + _startState.SquareWidth / 2.0;
        var startY = _startState.Y + _startState.SquareHeight / 2.0;
        var endX = _endState.X + _endState.SquareWidth / 2.0;
        var endY = _endState.Y + _endState.SquareHeight / 2.0;

        _geometry = ArrowGeometryExtensions.CalculateArrowGeometry(
            startX, startY, endX, endY,
            BlueprintEditorConstants.ArrowLineOffset,
            BlueprintEditorConstants.ArrowTextXOffset,
            BlueprintEditorConstants.ArrowTextYOffset);

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_geometry == null || _startState == null || _endState == null)
        {
            return;
        }

        var g = _geometry;

        // Draw line
        var start = new Point(g.LineStartX, g.LineStartY);
        var end = new Point(g.LineEndX, g.LineEndY);
        context.DrawLine(new Pen(Brushes.Black, BlueprintEditorConstants.ArrowLineStroke), start, end);

        // Draw arrowhead
        var halfWidth = BlueprintEditorConstants.ArrowheadWidth;
        var tip = new Point(g.ArrowheadTranslateX, g.ArrowheadTranslateY);
        var base1 = new Point(
            g.ArrowheadTranslateX + halfWidth * 0.7,
            g.ArrowheadTranslateY + halfWidth);
        var base2 = new Point(
            g.ArrowheadTranslateX - halfWidth * 0.7,
            g.ArrowheadTranslateY + halfWidth);

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

            var labelPos = new Point(
                g.LabelTextTranslateX - formattedText.Width / 2,
                g.LabelTextTranslateY - formattedText.Height / 2);

            context.DrawText(formattedText, labelPos);
        }
    }

    private void TransactionArrowPointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || _onClick == null || Parent == null)
        {
            e.Handled = true;
            return;
        }

        var position = e.GetPosition(this);

        // Check if click is near arrowhead
        if (_geometry != null)
        {
            var dx = Math.Abs(position.X - _geometry.ArrowheadTranslateX);
            var dy = Math.Abs(position.Y - _geometry.ArrowheadTranslateY);
            var halfWidth = BlueprintEditorConstants.ArrowheadWidth * 2;

            if (dx <= halfWidth && dy <= halfWidth)
            {
                var popPosition = e.GetPosition((Control)Parent!);
                _popOver = new EditArrowPopOver(_viewModel, (Control)Parent!, popPosition);
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
        RemoveHandler(PointerPressedEvent, TransactionArrowPointerPressedHandler);
        _popOver?.Dispose();
    }
}
