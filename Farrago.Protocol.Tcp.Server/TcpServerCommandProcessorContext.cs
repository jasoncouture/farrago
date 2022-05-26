using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Farrago.Protocol.Tcp.Server;

public sealed class TcpServerCommandProcessorContext : IDisposable
{
    private readonly TcpClient _client;
    private readonly IServiceScope _serviceScope;

    public TcpServerCommandProcessorContext(TcpClient client, IServiceScope serviceScope)
    {
        _client = client;
        _serviceScope = serviceScope;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement the client loop here.
        await Task.Yield();
        try
        {
            _client.Close();
        }
        catch
        {
            // Ignored.
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _serviceScope.Dispose();
    }
}