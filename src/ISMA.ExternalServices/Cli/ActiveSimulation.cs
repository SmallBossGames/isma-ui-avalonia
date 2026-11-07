using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading.Channels;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;

namespace ISMA.ExternalServices.Cli;

/// <summary>
/// <see cref="IActiveSimulation"/> over a running simulation runner process.
/// Reads the runner's stdout (the ISMA exchange binary format) on a background
/// thread to accumulate the result in a file and to expose the TIME column of
/// each streamed row for progress tracking.
/// </summary>
public sealed class ActiveSimulation : IActiveSimulation
{
    private readonly CliProcess _process;
    private readonly Channel<double> _times = Channel.CreateUnbounded<double>();
    private readonly string _resultFile;
    private readonly Task _streamTask;

    private ImmutableArray<string> _columnNames = ImmutableArray<string>.Empty;

    public ActiveSimulation(CliProcess process)
    {
        _process = process;
        _resultFile = Path.Combine(CliWorkDir.Root, $"isma-simulation-{Guid.NewGuid():N}.bin");
        _streamTask = Task.Run(() => StreamResultAsync());
    }

    public IAsyncEnumerable<double> Times => _times.Reader.ReadAllAsync();

    public async Task<SimulationResult> GetResultAsync(CancellationToken cancellationToken = default)
    {
        var exitCode = await _process.WaitExitCodeAsync(cancellationToken).ConfigureAwait(false);
        // wait for the stdout stream to drain so the result file is complete
        await _streamTask.ConfigureAwait(false);

        if (exitCode != 0)
        {
            var stderr = (await _process.Stderr.ConfigureAwait(false)).Trim();
            TryDelete(_resultFile);
            throw new CliToolException("simulation-runner-cli", exitCode, stderr);
        }

        return new SimulationResult(_resultFile, _columnNames);
    }

    public Task CancelAsync() => _process.TerminateAsync();

    public async ValueTask DisposeAsync() => await _process.DisposeAsync().ConfigureAwait(false);

    private async Task StreamResultAsync()
    {
        using var result = File.Create(_resultFile);
        using var source = new BufferedStream(_process.Stdout, 32 * 1024);
        using var tee = new TeeStream(source, result);
        try
        {
            var numColumns = ReadUInt16BE(tee);
            var columnNames = new string[numColumns];
            for (var i = 0; i < numColumns; i++)
            {
                var nameLength = ReadUInt16BE(tee);
                columnNames[i] = Encoding.UTF8.GetString(ReadFully(tee, nameLength));
            }
            _columnNames = columnNames.ToImmutableArray();

            while (true)
            {
                var time = ReadDoubleBE(tee);
                _times.Writer.TryWrite(time);
                for (var i = 1; i < numColumns; i++)
                {
                    ReadDoubleBE(tee);
                }
            }
        }
        catch (EndOfStreamException)
        {
            // stream exhausted — the runner has finished writing
        }
        catch (Exception)
        {
            // the process was terminated or the stream broke mid-run
        }
        finally
        {
            _times.Writer.TryComplete();
        }
    }

    private static byte[] ReadFully(Stream stream, int count)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var n = stream.Read(buffer, offset, count - offset);
            if (n <= 0)
            {
                throw new EndOfStreamException();
            }

            offset += n;
        }

        return buffer;
    }

    private static ushort ReadUInt16BE(Stream stream)
    {
        var hi = stream.ReadByte();
        if (hi < 0)
        {
            throw new EndOfStreamException();
        }

        var lo = stream.ReadByte();
        if (lo < 0)
        {
            throw new EndOfStreamException();
        }

        return (ushort)((hi << 8) | lo);
    }

    private static double ReadDoubleBE(Stream stream)
    {
        var bytes = ReadFully(stream, 8);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToDouble(bytes, 0);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort
        }
    }

    private sealed class TeeStream(Stream source, Stream sink) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = source.Read(buffer, offset, count);
            if (n > 0)
            {
                sink.Write(buffer, offset, n);
            }

            return n;
        }

        public override void Flush() => source.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
