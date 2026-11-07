namespace ISMA.ExternalServices.Cli;

/// <summary>
/// Stream helpers for CLI tool I/O.
/// </summary>
internal static class CliStreams
{
    /// <summary>Reads the stream to completion.</summary>
    public static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Writes the whole buffer to the stream.</summary>
    public static Task WriteAllAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        stream.WriteAsync(buffer, cancellationToken).AsTask();
}
