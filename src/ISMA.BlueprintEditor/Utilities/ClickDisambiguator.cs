using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ISMA.BlueprintEditor.Utilities;

/// <summary>
/// Manages a pending timer to defer single-click handling.
/// If a double-click occurs within the delay window, the single-click is cancelled.
/// Also tracks drag state to ignore clicks that were actually drags.
/// </summary>
public sealed class ClickDisambiguator : IDisposable
{
    private const double SingleClickDelayMs = 200.0;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(SingleClickDelayMs) };
    private bool _isSingleClickPending;
    private bool _isDragging;
    private Point? _pointerDownPosition;
    private readonly Action _onDoubleClick;
    private readonly Action _onSingleClick;
    private readonly Control _owner;

    public ClickDisambiguator(Control owner, Action onSingleClick, Action onDoubleClick)
    {
        _owner = owner;
        _onSingleClick = onSingleClick;
        _onDoubleClick = onDoubleClick;

        _timer.Tick += OnTimerTick;

        _owner.PointerPressed += OnPointerPressed;
        _owner.PointerReleased += OnPointerReleased;
        _owner.PointerMoved += OnPointerMoved;
        _owner.PointerCaptureLost += OnPointerCaptureLost;
    }

    public void Dispose()
    {
        _owner.PointerPressed -= OnPointerPressed;
        _owner.PointerReleased -= OnPointerReleased;
        _owner.PointerMoved -= OnPointerMoved;
        _owner.PointerCaptureLost -= OnPointerCaptureLost;
        _timer.Stop();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _pointerDownPosition = e.GetPosition(_owner);
        _timer.Stop();
        _isSingleClickPending = true;
        _timer.Start();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _timer.Stop();
        if (_isDragging)
        {
            _isDragging = false;
            _isSingleClickPending = false;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_pointerDownPosition.HasValue || _isDragging)
            return;

        var currentPos = e.GetPosition(_owner);
        var dx = Math.Abs(currentPos.X - _pointerDownPosition.Value.X);
        var dy = Math.Abs(currentPos.Y - _pointerDownPosition.Value.Y);

        // Drag threshold: 3px
        if (dx > 3.0 || dy > 3.0)
        {
            _isDragging = true;
            _timer.Stop();
            _isSingleClickPending = false;
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _timer.Stop();
        _isSingleClickPending = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_isSingleClickPending && !_isDragging)
        {
            _onSingleClick();
        }
        _isSingleClickPending = false;
    }

    public void CancelSingleClick()
    {
        _timer.Stop();
        _isSingleClickPending = false;
    }

    public void Reset()
    {
        _timer.Stop();
        _isSingleClickPending = false;
        _isDragging = false;
        _pointerDownPosition = null;
    }
}
