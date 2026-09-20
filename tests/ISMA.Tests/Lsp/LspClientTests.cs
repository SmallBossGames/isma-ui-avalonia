using FluentAssertions;
using ISMA.ExternalServices.Lsp;
using Xunit;

namespace ISMA.Tests.Lsp;

/// <summary>
/// Unit tests for <see cref="LspClient"/> against an in-memory transport.
/// </summary>
public class LspClientTests
{
    private const string Source = "const a = 1.5; // note";
    // const -> (0,0,5,keyword), 1.5 -> (0,10,3,number), // note -> (0,14,7,comment)
    private static readonly long[] WireData = [0, 0, 5, 0, 0, 0, 10, 3, 2, 0, 0, 4, 7, 1, 0];

    [Fact]
    public async Task SemanticTokens_DecodesWireFormat()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = Source,
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        var tokens = await client.SemanticTokensAsync("doc1", Source);

        tokens.Should().HaveCount(3);
        tokens[0].Should().Be(new LspToken(0, 0, 5, "keyword"));
        tokens[1].Should().Be(new LspToken(0, 10, 3, "number"));
        tokens[2].Should().Be(new LspToken(0, 14, 7, "comment"));
    }

    [Fact]
    public async Task SemanticTokens_ReturnsEmpty_WhenServerHasNoMatchingText()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = "other text",
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        var tokens = await client.SemanticTokensAsync("doc1", Source);

        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Initialize_IsPerformedOnce()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = Source,
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        await client.SemanticTokensAsync("doc1", Source);
        await client.SemanticTokensAsync("doc1", Source);

        transport.Methods.Count(m => m == "initialize").Should().Be(1);
        transport.Methods.Count(m => m == "initialized").Should().Be(1);
    }

    [Fact]
    public async Task Document_IsOpenedOnce_ThenUpdatedWithIncrementingVersions()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = Source,
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        await client.SemanticTokensAsync("doc1", "const a = 1;");
        await client.SemanticTokensAsync("doc1", Source);

        transport.Methods.Count(m => m == "textDocument/didOpen").Should().Be(1);
        transport.Methods.Count(m => m == "textDocument/didChange").Should().Be(1);
        transport.DidChangeVersions.Should().BeEquivalentTo([2]);
        transport.Documents.Values.Single().Should().Be(Source);
    }

    [Fact]
    public async Task CloseDocument_IsIdempotent()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = Source,
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        await client.SemanticTokensAsync("doc1", Source);
        client.CloseDocument("doc1");
        client.CloseDocument("doc1");
        client.CloseDocument("never-opened");

        transport.Methods.Count(m => m == "textDocument/didClose").Should().Be(1);
    }

    [Fact]
    public async Task IndependentDocuments_AreOpenedSeparately()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = Source,
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        await client.SemanticTokensAsync("docA", Source);
        await client.SemanticTokensAsync("docB", Source);

        transport.Methods.Count(m => m == "textDocument/didOpen").Should().Be(2);
        transport.Methods.Count(m => m == "textDocument/didChange").Should().Be(0);
        transport.Documents.Should().HaveCount(2);
    }

    [Fact]
    public async Task Shutdown_SendsShutdownAndExit_ClosesTransport()
    {
        var transport = new InMemoryLspTransport
        {
            ExpectedSource = Source,
            SemanticTokensData = WireData.ToList(),
        };
        using var client = new LspClient(transport);

        await client.SemanticTokensAsync("doc1", Source);
        client.Shutdown();

        transport.Methods.Should().Contain("shutdown");
        transport.Methods.Should().Contain("exit");
        transport.Closed.Should().BeTrue();
    }
}
