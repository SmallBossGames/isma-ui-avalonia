namespace ISMA.ViewModels.Services;

/// <summary>
/// Service for automatic saving of blueprint projects.
/// </summary>
public interface IAutoSaveService
{
    /// <summary>
    /// Gets or sets the auto-save interval in milliseconds. Default is 60000 (1 minute).
    /// </summary>
    int IntervalMs { get; set; }

    /// <summary>
    /// Starts the auto-save timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the auto-save timer.
    /// </summary>
    void Stop();

    /// <summary>
    /// Triggers an immediate auto-save.
    /// </summary>
    void TriggerSave();

    /// <summary>
    /// Event fired when auto-save occurs.
    /// </summary>
    event Action? AutoSaved;
}

/// <summary>
/// Default implementation of <see cref="IAutoSaveService"/>.
/// </summary>
public class AutoSaveService : IAutoSaveService
{
    private System.Timers.Timer? _timer;
    private readonly Action _saveAction;
    private readonly object _lock = new();
    private bool _isSaving;

    /// <inheritdoc />
    public int IntervalMs { get; set; } = 60000;

    /// <inheritdoc />
    public event Action? AutoSaved;

    /// <summary>
    /// Creates a new auto-save service.
    /// </summary>
    /// <param name="saveAction">Action to perform when auto-saving.</param>
    public AutoSaveService(Action saveAction)
    {
        _saveAction = saveAction;
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = new System.Timers.Timer(IntervalMs)
            {
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += OnTimerElapsed;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <inheritdoc />
    public void TriggerSave()
    {
        PerformSave();
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var uiSynchronizationContext = SynchronizationContext.Current;
        if (uiSynchronizationContext != null)
        {
            uiSynchronizationContext.Post(_ => PerformSave(), null);
        }
        else
        {
            PerformSave();
        }
    }

    private void PerformSave()
    {
        if (_isSaving) return;

        lock (_lock)
        {
            _isSaving = true;
        }

        try
        {
            _saveAction();
            AutoSaved?.Invoke();
        }
        finally
        {
            lock (_lock)
            {
                _isSaving = false;
            }
        }
    }
}
