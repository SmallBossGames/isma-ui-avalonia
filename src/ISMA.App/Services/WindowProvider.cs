using Avalonia.Controls;

namespace ISMA.App.Services;

/// <summary>
/// Holds the current main window. Set by App after the window is created, since
/// DI-constructed services are resolved before the window exists.
/// </summary>
public sealed class WindowProvider
{
    public Window? Current { get; set; }
}
