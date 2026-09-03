using ISMA.Domain.Models;

namespace ISMA.App.Services;

/// <summary>
/// Interface for simulation parameters store/load operations.
/// Implemented in App layer to access FileDialog.
/// </summary>
public interface ISimulationParametersStoreService
{
    Task<bool> StoreAsync();
    Task<bool> LoadAsync();
}
