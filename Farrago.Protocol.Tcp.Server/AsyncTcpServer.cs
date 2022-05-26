using System.Net;
using System.Net.Sockets;

namespace Farrago.Protocol.Tcp.Server;

public sealed class AsyncTcpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _backlog;
    public EndPoint EndPoint => _listener.LocalEndpoint;

    public AsyncTcpServer(TcpListener listener, int backlog = 512)
    {
        _listener = listener;
        _backlog = backlog;
    }

    public void StartListening()
    {
        _listener.Start(_backlog);
    }

    public async Task<TcpClient> AcceptNextAsync(CancellationToken cancellationToken)
    {
        return await _listener.AcceptTcpClientAsync(cancellationToken);
    }

    public void Dispose()
    {
        try
        {
            _listener.Stop();
        }
        catch
        {
            // Ignored.
        }
    }
}