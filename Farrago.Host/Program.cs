using Farrago.Core;
using Farrago.Core.KeyValueStore;
using Orleans;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddFarrago();
#if DEBUG
builder.Services.AddHostedService<TestingService>();
#endif
builder.Services.AddMvc();
builder.Services.AddControllers();
var app = builder.Build();
app.UseFarrago();
app.UseRouting();
app.UseEndpoints(eb => eb.MapControllers());

app.MapGet("/readiness", () => "Ok");
app.MapGet("/liveness", () => "Ok");
await app.RunAsync();

public class TestingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public TestingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken);
        await Task.WhenAll(Enumerable.Range(0, 64).Select(i => ExecuteWorker(i, stoppingToken)));
    }

    private async Task ExecuteWorker(int workerNumber, CancellationToken stoppingToken)
    {
        var clusterClient = _serviceProvider.GetRequiredService<IClusterClient>();

        for (var x = 0; x < 100; x++)
        {
            await clusterClient.GetGrain<IStorageGrain>((long) x, "key1").SetBlobAsync(Array.Empty<byte>(), TimeSpan.FromSeconds(30), DateTimeOffset.Now.AddSeconds(15));
            await clusterClient.GetGrain<IStorageGrain>((long) x, "key2").SetBlobAsync(Array.Empty<byte>(), TimeSpan.FromSeconds(30), DateTimeOffset.Now.AddSeconds(15));
            await Task.Delay(TimeSpan.FromSeconds(0.20), stoppingToken);
            await clusterClient.GetGrain<IStorageGrain>((long) x, "key3").SetBlobAsync(Array.Empty<byte>(), TimeSpan.FromSeconds(30), DateTimeOffset.Now.AddSeconds(15));
            await Task.Delay(TimeSpan.FromSeconds(0.1), stoppingToken);
        }
    }
}