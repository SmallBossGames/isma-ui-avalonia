using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;

namespace ISMA.ExternalServices.Lsp;

/// <summary>
/// <see cref="ISyntaxHighlighter"/> backed by the LISMA language server over LSP.
/// </summary>
public sealed class LspSyntaxHighlighter : ISyntaxHighlighter
{
    private readonly LspClient _lspClient;

    public LspSyntaxHighlighter(LspClient lspClient)
    {
        _lspClient = lspClient;
    }

    public async Task<SyntaxTokenDto[]> Highlight(string documentId, string source)
    {
        var tokens = await _lspClient.SemanticTokensAsync(documentId, source).ConfigureAwait(false);
        return tokens
            .Select(t => new SyntaxTokenDto(t.Line, t.StartChar, t.Length, MapKind(t.Type)))
            .ToArray();
    }

    public Task CloseDocument(string documentId)
    {
        _lspClient.CloseDocument(documentId);
        return Task.CompletedTask;
    }

    private static SyntaxTokenKind MapKind(string type) => type switch
    {
        "keyword" => SyntaxTokenKind.Keyword,
        "comment" => SyntaxTokenKind.Comment,
        "number" => SyntaxTokenKind.Number,
        _ => SyntaxTokenKind.Text,
    };
}
