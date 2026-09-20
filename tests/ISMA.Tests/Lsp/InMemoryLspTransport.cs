using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using ISMA.ExternalServices.Lsp;

namespace ISMA.Tests.Lsp;

/// <summary>
/// In-memory <see cref="ILspTransport"/> for unit-testing <see cref="LspClient"/>.
/// Records every method received and answers initialize and
/// textDocument/semanticTokens/full synchronously.
/// </summary>
public sealed class InMemoryLspTransport : ILspTransport
{
    private readonly ConcurrentQueue<string> _serverToClient = new();
    private readonly object _gate = new();

    /// <summary>Methods received from the client, in order.</summary>
    public List<string> Methods { get; } = [];

    /// <summary>Document text tracked from didOpen/didChange, per URI.</summary>
    public Dictionary<string, string> Documents { get; } = new();

    /// <summary>Version numbers observed in didChange notifications.</summary>
    public List<int> DidChangeVersions { get; } = [];

    /// <summary>Document text for which <see cref="SemanticTokensData"/> is returned.</summary>
    public string? ExpectedSource { get; set; }

    /// <summary>Encoded LSP semantic token data (5 ints per token).</summary>
    public List<long> SemanticTokensData { get; set; } = [];

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
                    var openUri = message["params"]?["textDocument"]?["uri"]?.GetValue<string>();
                    var openText = message["params"]?["textDocument"]?["text"]?.GetValue<string>();
                    if (openUri is not null && openText is not null)
                    {
                        lock (_gate)
                        {
                            Documents[openUri] = openText;
                        }
                    }
                    break;
                case "textDocument/didChange":
                    var changeUri = message["params"]?["textDocument"]?["uri"]?.GetValue<string>();
                    var version = message["params"]?["textDocument"]?["version"]?.GetValue<int>();
                    var changes = message["params"]?["contentChanges"] as JsonArray;
                    var text = changes is { Count: > 0 } ? changes[^1]?["text"]?.GetValue<string>() : null;
                    if (changeUri is not null)
                    {
                        lock (_gate)
                        {
                            if (text is not null)
                            {
                                Documents[changeUri] = text;
                            }
                            if (version.HasValue)
                            {
                                DidChangeVersions.Add(version.Value);
                            }
                        }
                    }
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
        if (Documents.Values.Count > 0 && Documents.Values.Last() == ExpectedSource)
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
            Thread.Sleep(10);
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
