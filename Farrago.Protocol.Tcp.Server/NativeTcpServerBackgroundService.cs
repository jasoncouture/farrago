using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Farrago.Protocol.Tcp.Server;

public sealed class NativeTcpServerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<NativeTcpServerOptions> _options;
    private readonly ILogger<NativeTcpServerBackgroundService> _logger;

    public NativeTcpServerBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<NativeTcpServerOptions> options,
        ILogger<NativeTcpServerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        var servers = CreateServerObjects().ToList();

        var serverTasks = servers.Select(server => HandleServerConnections(server, stoppingToken)).ToList();

        if (serverTasks.Count > 0)
            await Task.WhenAll(serverTasks);
    }

    private IEnumerable<AsyncTcpServer> CreateServerObjects()
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Native TCP server is not enabled. Skipping TCP server startup.");
            yield break;
        }
        if (_options.Value.Addresses.Count == 0)
        { 
            var listener = new TcpListener(IPAddress.Any, _options.Value.Port);
            yield return new AsyncTcpServer(listener);
            yield break;
        }

        
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var host in _options.Value.Addresses)
        {
            var address = IPAddress.Parse(host);
            var listener = new TcpListener(address, _options.Value.Port);
            yield return new AsyncTcpServer(listener);
        }
    }


    private async Task HandleServerConnections(AsyncTcpServer tcpServer, CancellationToken stoppingToken)
    {
        using var _ = tcpServer; // Make sure this gets disposed when we leave this method.
        tcpServer.StartListening();
        _logger.LogInformation("TCP Server started: {endpoint}", tcpServer.EndPoint);
        var completedTaskQueue = new ConcurrentQueue<Task>();
        // HashSet is used here for rapid lookup for deletion
        // We want to blow away client tasks as soon as we can.
        var tasks = new HashSet<Task>();

        void EnqueueTaskForDeletion(Task task)
        {
            completedTaskQueue.Enqueue(task);
        }

        Action<Task> completionHandler = EnqueueTaskForDeletion;
        var acceptTask = tcpServer.AcceptNextAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var acceptedClient = await acceptTask;
            acceptTask =
                tcpServer.AcceptNextAsync(stoppingToken); // Begin accepting the next client now, we'll be back quickly.
            var task = HandleClientAsync(acceptedClient, stoppingToken);
            // This continuation should always execute.
            // ReSharper disable once MethodSupportsCancellation
#pragma warning disable CS4014
            task.ContinueWith(completionHandler);
#pragma warning restore CS4014
            tasks.Add(task);

            while (!completedTaskQueue.IsEmpty)
            {
                if (!completedTaskQueue.TryDequeue(out var next))
                    continue;
                await next;
                tasks.Remove(next);
            }
        }

        if (tasks.Count > 0)
        {
            // Block shutdown until all clients are disconnected (Or wait to be killed)
            await Task.WhenAll(tasks);
        }
    }

    private async Task HandleClientAsync(TcpClient acceptedClient, CancellationToken cancellationToken)
    {
        using var serviceScope = _serviceProvider.CreateScope();
        using var commandProcessor = new TcpServerCommandProcessorContext(acceptedClient, serviceScope);
        try
        {
            await commandProcessor.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Client disconnected due to an exception");
        }
    }
}