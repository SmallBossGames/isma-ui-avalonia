using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using ISMA.ExternalServices.Lsp;

namespace ISMA.Tests.Integration;

/// <summary>
/// In-memory <see cref="ILspTransport"/> emulating the LISMA language server.
/// Tracks the document text from didOpen/didChange and answers
/// semanticTokens/full with <see cref="SemanticTokensData"/> only while the
/// tracked text matches <see cref="ExpectedSource"/>.
/// </summary>
public sealed class FakeLspTransport : ILspTransport
{
    private readonly ConcurrentQueue<string> _serverToClient = new();
    private readonly object _gate = new();

    /// <summary>Document text for which <see cref="SemanticTokensData"/> is returned.</summary>
    public string? ExpectedSource { get; set; }

    /// <summary>Encoded LSP semantic token data (5 ints per token).</summary>
    public List<long> SemanticTokensData { get; set; } = [];

    /// <summary>Methods received from the client, in order.</summary>
    public List<string> Methods { get; } = [];

    /// <summary>The document text tracked from didOpen/didChange.</summary>
    public string? DocumentText { get; private set; }

    /// <summary>True after <see cref="Close"/> was called.</summary>
    public bool Closed { get; private set; }

    public void Send(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject message)
        {
            return;
        }

        var method = message["method"]?.GetValue<string>();
        if (method is not null)
        {
            lock (_gate)
            {
                Methods.Add(method);
            }
            switch (method)
            {
                case "textDocument/didOpen":
                    DocumentText = message["params"]?["textDocument"]?["text"]?.GetValue<string>();
                    break;
                case "textDocument/didChange":
                    var changes = message["params"]?["contentChanges"] as JsonArray;
                    DocumentText = changes is { Count: > 0 }
                        ? changes[^1]?["text"]?.GetValue<string>()
                        : DocumentText;
                    break;
            }
        }

        if (message["id"] is not JsonValue idValue || !idValue.TryGetValue<long>(out var id))
        {
            return;
        }

        JsonNode? result = method switch
        {
            "initialize" => new JsonObject
            {
                ["capabilities"] = new JsonObject
                {
                    ["textDocumentSync"] = 1,
                    ["semanticTokensProvider"] = new JsonObject
                    {
                        ["legend"] = new JsonObject
                        {
                            ["tokenTypes"] = new JsonArray("keyword", "comment", "number"),
                        },
                    },
                },
                ["serverInfo"] = new JsonObject
                {
                    ["name"] = "lisma-lsp",
                    ["version"] = "1.0.0",
                },
            },
            "textDocument/semanticTokens/full" => CreateTokensResult(),
            _ => null,
        };

        _serverToClient.Enqueue(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result,
        }.ToJsonString());
    }

    private JsonObject CreateTokensResult()
    {
        var dataNode = new JsonArray();
        if (DocumentText == ExpectedSource)
        {
            foreach (var v in SemanticTokensData)
            {
                dataNode.Add(v);
            }
        }
        return new JsonObject { ["data"] = dataNode };
    }

    public string? ReadNext()
    {
        while (true)
        {
            lock (_gate)
            {
                if (Closed)
                {
                    return null;
                }
            }
            if (_serverToClient.TryDequeue(out var next))
            {
                return next;
            }
            Thread.Sleep(20);
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            Closed = true;
        }
    }
}
