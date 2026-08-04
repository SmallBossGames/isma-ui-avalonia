namespace ISMA.BlueprintEditor.Utilities;

public class ClickDisambiguator
{
    private readonly long _clickDelayMs;
    private DateTimeOffset? _lastClickTime;
    private bool _isDragged;
    private readonly Action? _singleClickHandler;
    private readonly Action? _doubleClickHandler;

    public ClickDisambiguator(
        long clickDelayMs,
        Action? singleClickHandler,
        Action? doubleClickHandler)
    {
        _clickDelayMs = clickDelayMs;
        _singleClickHandler = singleClickHandler;
        _doubleClickHandler = doubleClickHandler;
    }

    public void OnPointerPressed()
    {
        _isDragged = false;
        var now = DateTimeOffset.UtcNow;

        if (_lastClickTime.HasValue && (now - _lastClickTime.Value).TotalMilliseconds <= _clickDelayMs)
        {
            // Double click
            _doubleClickHandler?.Invoke();
            _lastClickTime = null;
        }
        else
        {
            // First click - schedule single click
            _lastClickTime = now;
        }
    }

    public void OnPointerReleased()
    {
        if (_lastClickTime == null)
        {
            return;
        }

        var elapsed = (DateTimeOffset.UtcNow - _lastClickTime.Value).TotalMilliseconds;

        if (elapsed <= _clickDelayMs && !_isDragged)
        {
            _singleClickHandler?.Invoke();
        }

        _lastClickTime = null;
    }

    public void OnPointerMoved()
    {
        _isDragged = true;
        _lastClickTime = null;
    }

    public void OnKeyPressed()
    {
        _isDragged = false;
    }
}
