using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.Services;

public class SimulationParametersService
{
    private SimulationParameters _parameters = new();
    private readonly IPreferencesProvider? _preferencesProvider;

    public SimulationParameters Parameters => _parameters;

    public SimulationParameters GetParameters() => _parameters;

    public void SetParameters(SimulationParameters parameters)
    {
        _parameters = parameters ?? new SimulationParameters();
    }

    public void LoadFromPreferences(IPreferencesProvider? preferencesProvider = null)
    {
        var provider = preferencesProvider ?? _preferencesProvider;
        if (provider == null) return;

        var prefs = provider.Load();
        if (prefs?.SavedParameters != null)
        {
            _parameters = prefs.SavedParameters;
        }
    }

    public void SaveToPreferences(IPreferencesProvider? preferencesProvider = null)
    {
        var provider = preferencesProvider ?? _preferencesProvider;
        if (provider == null) return;

        var prefs = provider.Load();
        prefs.SavedParameters = _parameters;
        provider.Save(prefs);
    }
}
