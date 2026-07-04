using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.Services;

public class SimulationParametersService
{
    private SimulationParameters _parameters = new();

    public SimulationParameters Parameters => _parameters;

    public SimulationParameters GetParameters() => _parameters;

    public void SetParameters(SimulationParameters parameters)
    {
        _parameters = parameters ?? new SimulationParameters();
    }

    public void LoadFromPreferences(IPreferencesProvider? preferencesProvider = null)
    {
        if (preferencesProvider == null) return;

        var prefs = preferencesProvider.Load();
        if (prefs?.SavedParameters != null)
        {
            _parameters = prefs.SavedParameters;
        }
    }

    public void SaveToPreferences(IPreferencesProvider? preferencesProvider = null)
    {
        if (preferencesProvider == null) return;

        var prefs = preferencesProvider.Load();
        prefs.SavedParameters = _parameters;
        preferencesProvider.Save(prefs);
    }
}
