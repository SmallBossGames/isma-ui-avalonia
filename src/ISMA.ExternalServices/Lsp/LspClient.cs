using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ISMA.ExternalServices.Lsp;

/// <summary>A decoded semantic token with line/column offsets.</summary>
public sealed record LspToken(int Line, int StartChar, int Length, string Type);

/// <summary>Raised when the server responds to a request with an error object.</summary>
public sealed class LspRequestException(string message) : Exception(message);

/// <summary>
/// Minimal LSP client for the LISMA language server.
/// Speaks JSON-RPC 2.0 over an <see cref="ILspTransport"/>: performs the
/// initialize/initialized handshake lazily, keeps full-document state on the
/// server side (didOpen/didChange), and exposes pull-model semantic tokens.
/// </summary>
public sealed class LspClient : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(10_000);
    private const string LanguageId = "lisma";

    private readonly ILspTransport _transport;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonNode?>> _pending = new();
    private readonly ConcurrentDictionary<string, int> _documentVersions = new();
    private readonly ConcurrentDictionary<string, byte> _openedDocuments = new();
    private long _nextId;
    private string[] _legend = [];
    private volatile bool _initialized;
    private volatile bool _closed;

    public LspClient(ILspTransport transport)
    {
        _transport = transport;
        var readerThread = new Thread(ReaderLoop)
        {
            IsBackground = true,
            Name = "lsp-client-reader",
        };
        readerThread.Start();
    }

    /// <summary>
    /// Requests full-document semantic tokens, opening or updating the
    /// document on the server first.
    /// </summary>
    public async Task<LspToken[]> SemanticTokensAsync(string documentId, string source)
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        var uri = DocumentUri(documentId);
        if (_openedDocuments.TryAdd(uri, 0))
        {
            SendNotification("textDocument/didOpen", new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = uri,
                    ["languageId"] = LanguageId,
                    ["version"] = 1,
                    ["text"] = source,
                }
            });
        }
        else
        {
            var version = _documentVersions.AddOrUpdate(uri, 2, (_, v) => v + 1);
            SendNotification("textDocument/didChange", new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = uri,
                    ["version"] = version,
                },
                ["contentChanges"] = new JsonArray
                {
                    new JsonObject { ["text"] = source },
                },
            });
        }

        var result = await RequestAsync(
            "textDocument/semanticTokens/full",
            new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = uri } }
        ).ConfigureAwait(false);
        if (result?["data"] is not JsonArray data)
        {
            return [];
        }
        return DecodeTokens(data);
    }

    /// <summary>Notifies the server that the document was closed.</summary>
    public void CloseDocument(string documentId)
    {
        var uri = DocumentUri(documentId);
        if (!_openedDocuments.TryRemove(uri, out _))
        {
            return;
        }
        _documentVersions.TryRemove(uri, out _);
        SendNotification("textDocument/didClose",
            new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = uri } });
    }

    /// <summary>
    /// Best-effort graceful shutdown: shutdown request, exit notification,
    /// transport close. Never throws.
    /// </summary>
    public void Shutdown()
    {
        if (_closed)
        {
            return;
        }
        _closed = true;
        try
        {
            if (_initialized)
            {
                try
                {
                    RequestAsync("shutdown", new JsonObject()).GetAwaiter().GetResult();
                    SendNotification("exit", new JsonObject());
                }
                catch
                {
                    // Server already gone
                }
            }
        }
        finally
        {
            _transport.Close();
        }
    }

    public void Dispose()
    {
        Shutdown();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }
        var result = await RequestAsync("initialize", new JsonObject
        {
            ["processId"] = null,
            ["rootUri"] = null,
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject
            {
                ["name"] = "isma-ui",
                ["version"] = "1.0",
            },
        }).ConfigureAwait(false);
        _legend = result?["capabilities"]?["semanticTokensProvider"]?["legend"]?["tokenTypes"] is JsonArray types
            ? types.Select(t => t?.GetValue<string>() ?? "unknown").ToArray()
            : [];
        SendNotification("initialized", new JsonObject());
        _initialized = true;
    }

    private Task<JsonNode?> RequestAsync(string method, JsonObject @params)
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<JsonNode?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;
        SendRequest(id, method, @params);
        return AwaitWithTimeoutAsync(tcs.Task);
    }

    private static async Task<JsonNode?> AwaitWithTimeoutAsync(Task<JsonNode?> task)
    {
        try
        {
            return await task.WaitAsync(RequestTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException("LSP request timed out");
        }
    }

    private void SendRequest(long id, string method, JsonObject @params)
    {
        _transport.Send(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = @params,
        }.ToJsonString());
    }

    private void SendNotification(string method, JsonObject @params)
    {
        _transport.Send(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params,
        }.ToJsonString());
    }

    private void ReaderLoop()
    {
        while (!_closed)
        {
            string? json;
            try
            {
                json = _transport.ReadNext();
            }
            catch
            {
                break;
            }
            if (json is null)
            {
                break;
            }

            if (JsonNode.Parse(json) is not JsonObject message)
            {
                continue;
            }
            if (message["id"] is not JsonValue idValue || !idValue.TryGetValue<long>(out var id))
            {
                continue;
            }
            if (!_pending.TryRemove(id, out var tcs))
            {
                continue;
            }
            if (message["error"] is { } error)
            {
                tcs.TrySetException(new LspRequestException(error.ToJsonString()));
            }
            else
            {
                tcs.TrySetResult(message["result"]);
            }
        }
    }

    private LspToken[] DecodeTokens(JsonArray data)
    {
        var values = data.Select(v => v?.GetValue<long>() ?? 0).ToArray();
        var tokens = new List<LspToken>(values.Length / 5);
        var line = 0;
        var start = 0;
        for (var i = 0; i + 4 < values.Length; i += 5)
        {
            var deltaLine = values[i];
            var deltaStart = values[i + 1];
            var length = values[i + 2];
            var typeIndex = values[i + 3];
            if (deltaLine == 0)
            {
                start += (int)deltaStart;
            }
            else
            {
                line += (int)deltaLine;
                start = (int)deltaStart;
            }
            var type = typeIndex >= 0 && typeIndex < _legend.Length
                ? _legend[(int)typeIndex]
                : "unknown";
            tokens.Add(new LspToken(line, start, (int)length, type));
        }
        return tokens.ToArray();
    }

    private static string DocumentUri(string documentId) => $"file:///isma/{documentId}.lisma";
}
