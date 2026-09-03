using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;

namespace ISMA.App.Services;

public sealed class SyntaxHighlighterService : ISyntaxHighlighter
{
    private readonly ISimulationServerFacade _serverFacade;

    public SyntaxHighlighterService(ISimulationServerFacade serverFacade)
    {
        _serverFacade = serverFacade;
    }

    public async Task<SyntaxTokenDto[]> Highlight(string source)
    {
        return await _serverFacade.HighlightSource(source);
    }
}
