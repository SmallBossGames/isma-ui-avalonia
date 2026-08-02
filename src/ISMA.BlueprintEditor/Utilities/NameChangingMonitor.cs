using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ISMA.BlueprintEditor.Utilities;

/// <summary>
/// Tracks registered state names and auto-increments a counter to generate unique default names.
/// Supports regex-based counter extraction from existing names (e.g., "New state 1", "New state 2").
/// </summary>
public class NameChangingMonitor
{
    private readonly HashSet<string> _registeredNames = new();
    private readonly Regex _counterRegex = new(@"^.*(\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// Gets the set of currently registered names.
    /// </summary>
    public IReadOnlySet<string> RegisteredNames => _registeredNames;

    /// <summary>
    /// Registers a name. Returns true if the name was successfully registered (not a duplicate).
    /// </summary>
    public bool TryRegister(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!_registeredNames.Add(name))
            return false;

        ExtractCounter(name);
        return true;
    }

    /// <summary>
    /// Unregisters a name. Returns true if the name was found and removed.
    /// </summary>
    public bool TryUnregister(string name)
    {
        return _registeredNames.Remove(name);
    }

    /// <summary>
    /// Clears all registered names.
    /// </summary>
    public void Clear()
    {
        _registeredNames.Clear();
    }

    /// <summary>
    /// Generates the next unique default name by appending an auto-incrementing counter.
    /// E.g., "New state 1", "New state 2", etc.
    /// </summary>
    /// <param name="prefix">The name prefix (e.g., "New state").</param>
    /// <returns>A unique name with counter suffix.</returns>
    public string CreateNextDefaultName(string prefix = "New state")
    {
        var candidate = $"{prefix} {GetNextCounter()}";
        while (!_registeredNames.Add(candidate))
        {
            candidate = $"{prefix} {GetNextCounter()}";
        }
        return candidate;
    }

    private int GetNextCounter()
    {
        int maxCounter = 0;
        foreach (var name in _registeredNames)
        {
            var match = _counterRegex.Match(name);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var counter))
            {
                maxCounter = Math.Max(maxCounter, counter);
            }
        }
        return maxCounter + 1;
    }

    private void ExtractCounter(string name)
    {
        var match = _counterRegex.Match(name);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var counter))
        {
            // Counter extracted but we don't need to store it separately
            // The TryRegister already ensures uniqueness
        }
    }
}
