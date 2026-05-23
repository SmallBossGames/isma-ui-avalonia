using System.Collections.Immutable;
using ISMA.Domain.Contracts;
using ISMA.Domain.Results;

namespace ISMA.ViewModels.Services;

public sealed class LismaPdeService
{
    private readonly ISimulationServerFacade _serverFacade;

    public LismaPdeService(ISimulationServerFacade serverFacade)
    {
        _serverFacade = serverFacade;
    }

    public async Task<LismaPdeTranslationResult> ValidateAsync(string source)
    {
        var result = await _serverFacade.ValidateModel(source);
        if (result.Errors.Length == 0 && result.Warnings.Length == 0)
        {
            return LismaPdeTranslationResult.Success(
                new ISMA.Domain.Models.LismaTextModel(source, Array.Empty<ISMA.Domain.Models.CodeRegion>()));
        }

        var errors = result.Errors.Select(e => e.Message).ToImmutableArray();
        return LismaPdeTranslationResult.Failed(errors);
    }

    public async Task<LismaPdeTranslationResult> CompileAsync(string source)
    {
        var result = await _serverFacade.CompileModel(source);
        if (result.Errors.Length == 0)
        {
            return LismaPdeTranslationResult.Success(
                new ISMA.Domain.Models.LismaTextModel(source, Array.Empty<ISMA.Domain.Models.CodeRegion>()));
        }

        var errors = result.Errors.Select(e => e.Message).ToImmutableArray();
        return LismaPdeTranslationResult.Failed(errors);
    }
}
