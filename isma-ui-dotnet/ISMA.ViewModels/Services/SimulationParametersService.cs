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

    public void LoadFromPreferences()
    {
    }

    public void SaveToPreferences()
    {
    }
}
