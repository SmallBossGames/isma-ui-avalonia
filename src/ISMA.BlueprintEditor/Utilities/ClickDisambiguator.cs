using Avalonia.Threading;

namespace ISMA.BlueprintEditor.Utilities;

/// <summary>
/// Disambiguates single and double clicks with a delay window: a single click fires
/// only if no second click arrives within the delay; a double click fires immediately.
/// A drag suppresses click handling.
/// Ported from the original ISMA Kotlin/JavaFX editor (Timeline/KeyFrame → DispatcherTimer).
/// </summary>
public class ClickDisambiguator
{
    private readonly Action onSingleClick;
    private readonly Action onDoubleClick;
    private readonly int clickDelayMs;

    private DispatcherTimer? pendingTimer;
    private bool isDragged;

    /// <summary>
    /// Creates a disambiguator. The <see cref="DispatcherTimer"/> is created lazily
    /// on the first click (on the UI thread dispatcher), so construction is safe
    /// without a running dispatcher.
    /// </summary>
    public ClickDisambiguator(Action onSingleClick, Action onDoubleClick, int clickDelayMs = 200)
    {
        this.onSingleClick = onSingleClick;
        this.onDoubleClick = onDoubleClick;
        this.clickDelayMs = clickDelayMs;
    }

    /// <summary>Resets the dragged flag (call on pointer press).</summary>
    public void OnKeyPress()
    {
        isDragged = false;
    }

    /// <summary>Marks the press as a drag, suppressing click handling.</summary>
    public void OnDragged()
    {
        isDragged = true;
    }

    /// <summary>
    /// Handles a click with the given count (1 = single, 2 = double).
    /// A single click is delayed by the click delay and cancelled by a second click.
    /// </summary>
    public void OnClick(int clickCount)
    {
        if (isDragged)
        {
            return;
        }

        switch (clickCount)
        {
            case 1:
                HandleSingleClick();
                break;
            case 2:
                HandleDoubleClick();
                break;
        }
    }

    /// <summary>Cancels any pending single click.</summary>
    public void Cancel()
    {
        StopPending();
    }

    private void HandleSingleClick()
    {
        StopPending();
        var timer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher.UIThread)
        {
            Interval = TimeSpan.FromMilliseconds(clickDelayMs),
        };
        timer.Tick += (_, _) =>
        {
            // Avalonia DispatcherTimer repeats by default; the single click must fire exactly once.
            timer.Stop();
            pendingTimer = null;
            if (!isDragged)
            {
                onSingleClick();
            }
        };
        timer.Start();
        pendingTimer = timer;
    }

    private void HandleDoubleClick()
    {
        StopPending();
        onDoubleClick();
    }

    private void StopPending()
    {
        pendingTimer?.Stop();
        pendingTimer = null;
    }
}
