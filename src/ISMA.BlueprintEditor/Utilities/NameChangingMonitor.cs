using System.Text.RegularExpressions;

namespace ISMA.BlueprintEditor.Utilities;

/// <summary>
/// Tracks which default names ("&lt;prefix&gt; N") are in use and produces the next default name,
/// recovering the counter from already-registered names.
/// Ported verbatim from the original ISMA Kotlin/JavaFX editor.
/// </summary>
public class NameChangingMonitor
{
    private readonly string itemDefaultName;
    private readonly Regex defaultNameRegex;
    private readonly HashSet<string> existedNames = new();
    private int nextNameCounter = 1;

    /// <summary>Creates a monitor for default names of the form "&lt;itemDefaultName&gt; N".</summary>
    public NameChangingMonitor(string itemDefaultName)
    {
        this.itemDefaultName = itemDefaultName;
        defaultNameRegex = new Regex("^" + Regex.Escape(itemDefaultName) + @" (\d+)$");
    }

    /// <summary>Registers a name; returns false if it was already registered.</summary>
    public bool TryRegister(string name)
    {
        if (existedNames.Contains(name))
        {
            return false;
        }

        Match match = defaultNameRegex.Match(name);
        if (match.Success)
        {
            int digit = int.Parse(match.Groups[1].Value);
            nextNameCounter = Math.Max(nextNameCounter, digit + 1);
        }

        existedNames.Add(name);
        return true;
    }

    /// <summary>Unregisters a name; returns false if it was not registered.</summary>
    public bool TryUnregister(string name)
    {
        if (!existedNames.Contains(name))
        {
            return false;
        }

        existedNames.Remove(name);
        return true;
    }

    /// <summary>Whether the name is currently registered.</summary>
    public bool IsRegistered(string name) => existedNames.Contains(name);

    /// <summary>Clears all registered names and resets the counter.</summary>
    public void Reset()
    {
        existedNames.Clear();
        nextNameCounter = 1;
    }

    /// <summary>The next default name: "&lt;itemDefaultName&gt; N".</summary>
    public string CreateNextDefaultName() => $"{itemDefaultName} {nextNameCounter}";
}
