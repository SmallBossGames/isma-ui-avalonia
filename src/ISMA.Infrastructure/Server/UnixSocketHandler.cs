using System.Net;
using System.Net.Sockets;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace ISMA.Infrastructure.Server;

public interface IUnixSocketHandler
{
    GrpcChannel CreateGrpcChannel(string address, ILogger? logger = null);
    HttpClient CreateHttpClient(string address);
}

internal sealed class UnixSocketHttpHandler : DelegatingHandler
{
    private readonly string _socketPath;

    public UnixSocketHttpHandler(string socketPath)
    {
        _socketPath = socketPath;
        InnerHandler = new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectCallback = ConnectCallback,
        };
    }

    private async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(_socketPath), cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}

internal sealed class LinuxUnixSocketHandler : IUnixSocketHandler
{
    public GrpcChannel CreateGrpcChannel(string address, ILogger? logger = null)
    {
        var handler = new UnixSocketHttpHandler(address);
        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = handler });
    }

    public HttpClient CreateHttpClient(string address)
    {
        var handler = new UnixSocketHttpHandler(address);
        return new HttpClient(handler);
    }
}

internal sealed class WindowsNamedPipeHandler : IUnixSocketHandler
{
    private readonly string _pipeName;

    public WindowsNamedPipeHandler(string pipeName)
    {
        _pipeName = pipeName;
    }

    public GrpcChannel CreateGrpcChannel(string address, ILogger? logger = null)
    {
        var handler = new NamedPipeHttpHandler(_pipeName);
        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = handler });
    }

    public HttpClient CreateHttpClient(string address)
    {
        var handler = new NamedPipeHttpHandler(_pipeName);
        return new HttpClient(handler);
    }
}

internal sealed class NamedPipeHttpHandler : DelegatingHandler
{
    private readonly string _pipeName;

    public NamedPipeHttpHandler(string pipeName)
    {
        _pipeName = pipeName;
        InnerHandler = new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectCallback = ConnectCallback,
        };
    }

    private async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var pipe = new System.IO.Pipes.NamedPipeClientStream(".", _pipeName, System.IO.Pipes.PipeDirection.InOut,
            System.IO.Pipes.PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Identification);
        await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
        return pipe;
    }
}

internal sealed class MacUnixSocketHandler : IUnixSocketHandler
{
    public GrpcChannel CreateGrpcChannel(string address, ILogger? logger = null)
    {
        var handler = new UnixSocketHttpHandler(address);
        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = handler });
    }

    public HttpClient CreateHttpClient(string address)
    {
        var handler = new UnixSocketHttpHandler(address);
        return new HttpClient(handler);
    }
}

public static class UnixSocketHandlerFactory
{
    public static IUnixSocketHandler Create()
    {
        return Environment.OSVersion.Platform == PlatformID.Unix
            ? (OperatingSystem.IsLinux() ? (IUnixSocketHandler)new LinuxUnixSocketHandler() : new MacUnixSocketHandler())
            : new WindowsNamedPipeHandler("/tmp/isma-server-pipe");
    }
}
