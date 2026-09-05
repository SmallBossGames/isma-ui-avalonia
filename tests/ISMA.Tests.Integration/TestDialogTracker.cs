using ISMA.App.Views;

namespace ISMA.Tests.Integration;

/// <summary>
/// Records the select-variables dialog windows created by the test app's
/// dialog window factory, so tests can interact with them (Avalonia headless
/// exposes no global window enumeration).
/// </summary>
public sealed class TestDialogTracker
{
    public List<SelectVariablesDialogWindow> Dialogs { get; } = [];

    public SelectVariablesDialogWindow? LastDialog => Dialogs.Count > 0 ? Dialogs[^1] : null;
}
