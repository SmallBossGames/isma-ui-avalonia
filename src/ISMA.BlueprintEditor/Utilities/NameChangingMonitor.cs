namespace ISMA.BlueprintEditor.Utilities;

public class NameChangingMonitor
{
    private readonly string _defaultName;
    private readonly HashSet<string> _registeredNames;
    private int _counter;

    public NameChangingMonitor(string defaultName)
    {
        _defaultName = defaultName;
        _registeredNames = new HashSet<string>();
        _counter = 1;
    }

    public bool TryRegister(string name)
    {
        if (_registeredNames.Contains(name))
        {
            return false;
        }

        _registeredNames.Add(name);

        // Parse name for pattern "{defaultName} {N}" to update counter
        if (name.StartsWith(_defaultName, StringComparison.Ordinal) &&
            name.Length > _defaultName.Length &&
            name[_defaultName.Length] == ' ')
        {
            var numPart = name.Substring(_defaultName.Length + 1);
            if (int.TryParse(numPart, out var num) && num >= _counter)
            {
                _counter = num + 1;
            }
        }

        return true;
    }

    public bool TryUnregister(string name)
    {
        return _registeredNames.Remove(name);
    }

    public string CreateNextDefaultName()
    {
        var name = $"{_defaultName} {_counter}";
        while (_registeredNames.Contains(name))
        {
            _counter++;
            name = $"{_defaultName} {_counter}";
        }
        return name;
    }

    public void Clear()
    {
        _registeredNames.Clear();
        _counter = 1;
    }
}
