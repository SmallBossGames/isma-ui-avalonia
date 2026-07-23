using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ISMA.ViewModels.Services;

public class NameChangingMonitor
{
    private readonly HashSet<string> _existedNames = new();
    private int _nextNameCounter = 1;
    private static readonly Regex DefaultNameRegex = new(@"^New state (\d+)$", RegexOptions.Compiled);

    public bool TryRegister(string name)
    {
        if (_existedNames.Contains(name))
            return false;

        var match = DefaultNameRegex.Match(name);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var digit))
            {
                _nextNameCounter = System.Math.Max(_nextNameCounter, digit + 1);
            }
        }

        _existedNames.Add(name);
        return true;
    }

    public bool TryUnregister(string name)
    {
        return _existedNames.Remove(name);
    }

    public string CreateNextDefaultName()
    {
        return $"New state {_nextNameCounter++}";
    }

    public void Clear()
    {
        _existedNames.Clear();
    }

    public bool Contains(string name)
    {
        return _existedNames.Contains(name);
    }

    public IEnumerable<string> ExposedNames => _existedNames;

    public void AddNames(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _existedNames.Add(name);
        }
    }

    public void RemoveNames(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _existedNames.Remove(name);
        }
    }

    public void CopyFrom(NameChangingMonitor source)
    {
        Clear();
        foreach (var name in source.ExposedNames)
        {
            _existedNames.Add(name);
        }
        _nextNameCounter = source._nextNameCounter;
    }
}
