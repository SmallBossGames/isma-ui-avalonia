namespace ISMA.ExternalServices.Lsp;

/// <summary>
/// Byte-level transport for the LSP protocol.
/// Implementations deliver complete LSP messages (JSON-RPC 2.0 documents,
/// without Content-Length framing) one at a time.
/// </summary>
public interface ILspTransport
{
    /// <summary>Sends a JSON-RPC message to the server.</summary>
    void Send(string json);

    /// <summary>Blocks until the next message is available; returns null on end of stream.</summary>
    string? ReadNext();

    /// <summary>Closes the transport and releases the underlying process.</summary>
    void Close();
}
