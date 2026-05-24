using ISMA.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ISMA.Infrastructure.FileStorage;

public sealed class PreferencesProvider : IDisposable
{
    private readonly string _settingsPath;
    private readonly ILogger<PreferencesProvider>? _logger;
    private readonly object _lock = new();

    public PreferencesProvider(string? settingsPath = null, ILogger<PreferencesProvider>? logger = null)
    {
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(settingsPath))
        {
            _settingsPath = settingsPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsPath = Path.Combine(appData, "isma", "preferences.json");
        }

        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public Preferences Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_settingsPath))
            {
                _logger?.LogDebug("Preferences file not found at {Path}, returning defaults", _settingsPath);
                return new Preferences();
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                };
                var preferences = System.Text.Json.JsonSerializer.Deserialize<Preferences>(json, options);
                return preferences ?? new Preferences();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading preferences from {Path}", _settingsPath);
                return new Preferences();
            }
        }
    }

    public void Save(Preferences preferences)
    {
        lock (_lock)
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                };
                var json = System.Text.Json.JsonSerializer.Serialize(preferences, options);
                File.WriteAllText(_settingsPath, json);
                _logger?.LogDebug("Preferences saved to {Path}", _settingsPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error writing preferences to {Path}", _settingsPath);
                throw;
            }
        }
    }

    public void CommitWindow(WindowPreferences windowPreferences)
    {
        var preferences = Load();
        preferences.WindowPreferences = windowPreferences;
        Save(preferences);
    }

    public void CommitFiles(DefaultFilesPreferences defaultFilesPreferences)
    {
        var preferences = Load();
        preferences.DefaultFilesPreferences = defaultFilesPreferences;
        Save(preferences);
    }

    public void Dispose()
    {
    }
}
