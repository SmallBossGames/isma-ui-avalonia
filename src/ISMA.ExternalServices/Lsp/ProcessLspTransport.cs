using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Lsp;

/// <summary>
/// <see cref="ILspTransport"/> backed by a child process speaking LSP over stdio.
/// stdout carries the protocol (Content-Length framing); stderr is drained and
/// logged so it cannot corrupt the protocol stream.
/// </summary>
public sealed class ProcessLspTransport : ILspTransport
{
    private const string HeaderDelimiter = "\r\n\r\n";
    private const string ContentLengthPrefix = "Content-Length:";

    private readonly Process _process;
    private readonly Stream _output;
    private readonly Stream _input;
    private readonly ILogger? _logger;

    public ProcessLspTransport(string scriptPath, ILogger? logger = null)
    {
        _logger = logger;
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };
        _process.Start();
        _output = _process.StandardInput.BaseStream;
        _input = _process.StandardOutput.BaseStream;
        _ = Task.Run(DrainStderrAsync);
    }

    public void Send(string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var header = Encoding.ASCII.GetBytes($"{ContentLengthPrefix} {body.Length}\r\n\r\n");
        _output.Write(header, 0, header.Length);
        _output.Write(body, 0, body.Length);
        _output.Flush();
    }

    public string? ReadNext()
    {
        var header = ReadUntil(_input, HeaderDelimiter);
        if (header is null)
        {
            return null;
        }

        var contentLength = header
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith(ContentLengthPrefix))
            ?.Substring(ContentLengthPrefix.Length)
            ?.Trim();
        if (contentLength is null || !int.TryParse(contentLength, out var length))
        {
            throw new InvalidOperationException($"Malformed LSP header: {header}");
        }

        var body = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var read = _input.Read(body, offset, length - offset);
            if (read < 0)
            {
                return null;
            }
            offset += read;
        }
        return Encoding.UTF8.GetString(body);
    }

    public void Close()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        _process.Dispose();
    }

    private async Task DrainStderrAsync()
    {
        try
        {
            using var reader = new StreamReader(_process.StandardError.BaseStream);
            while (await reader.ReadLineAsync() is { } line)
            {
                _logger?.LogDebug("[lsp] {Line}", line);
            }
        }
        catch
        {
            // Stream closed on process exit
        }
    }

    private static string? ReadUntil(Stream stream, string delimiter)
    {
        var buffer = new StringBuilder();
        while (true)
        {
            var b = stream.ReadByte();
            if (b < 0)
            {
                return buffer.Length == 0 ? null : buffer.ToString();
            }
            buffer.Append((char)b);
            if (buffer.Length >= delimiter.Length &&
                buffer.ToString(buffer.Length - delimiter.Length, delimiter.Length) == delimiter)
            {
                return buffer.ToString();
            }
        }
    }
}
