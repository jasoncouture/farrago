// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Net;

if (args.Length == 0)
{
    Console.WriteLine("Usage: Farrago.Benchmark host1 host2 ... hostN");
    Console.WriteLine("Hosts must be in a URI format, and if TLS is used, it must be a valid, trusted certificate.");
    return 1;
}

var socketHandler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = 1000,
    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
};
var clientPool = args.Select(i => new HttpClient(socketHandler)
{
    BaseAddress = new Uri(i),
    Timeout = TimeSpan.FromMinutes(2)
}).ToImmutableArray();

var tasks = clientPool.Select(StartWorkers);
Console.WriteLine("Workers have been started, please observe the Farrago cluster dashboard to see performance.");
await Task.WhenAll(tasks);

return 0;

async Task StartWorkers(HttpClient client)
{
    var workerId = Guid.NewGuid();
    var workerTasks = Enumerable.Range(0, 10).Select(i => StartWorker(client, workerId, i)).ToArray();
    await Task.WhenAll(workerTasks);
}

async Task StartWorker(HttpClient client, Guid workerId, int shard)
{
    for (var x = 0; x < 1000; x++)
    {
        var keyTarget = $"{workerId:n}:{shard}:{x}".ToLower();
        await CreateValueAsync(client, keyTarget, Guid.NewGuid().ToString("n"), TimeSpan.FromSeconds(120),
            DateTimeOffset.Now.AddMinutes(30), shard);
        for (var y = 0; y < 100; y++)
        {
            if (y % 10 == 0)
            {
                await CreateValueAsync(client, keyTarget, Guid.NewGuid().ToString("n"), TimeSpan.FromSeconds(120),
                    DateTimeOffset.Now.AddMinutes(30), shard);
            }
            else
            {
                await ReadValueAsync(client, keyTarget, shard);
            }
        }

        await DeleteValueAsync(client, keyTarget, shard);
    }
}

async Task DeleteValueAsync(HttpClient client, string key, int shard = 0)
{
    var uriString = $"key={Uri.EscapeDataString(key)}&shard={shard}";
    try
    {
        var response = await client.DeleteAsync($"/api/data/text?{uriString}");
    }
    catch
    {
    }
}

async Task<string?> ReadValueAsync(HttpClient client, string key, int shard = 0)
{
    var uriString = $"key={Uri.EscapeDataString(key)}&shard={shard}";
    try
    {
        var response = await client.GetAsync($"/api/data/text?{uriString}");
        if (response.StatusCode == HttpStatusCode.OK)
            return await response.Content.ReadAsStringAsync();

        return null;
    }
    catch
    {
        return null;
    }
}

async Task CreateValueAsync(HttpClient client, string key, string value, TimeSpan? slidingExpiration = null,
    DateTimeOffset? absoluteExpiration = null, int shard = 0)
{
    var dictionary = new Dictionary<string, string>()
    {
        ["key"] = key,
        ["text"] = value,
        ["shard"] = shard.ToString()
    };
    if (slidingExpiration != null)
    {
        dictionary["slidingExpiration"] = ((int) slidingExpiration.Value.TotalSeconds).ToString();
    }

    if (absoluteExpiration != null)
    {
        dictionary["absoluteExpiration"] = absoluteExpiration.Value.ToUnixTimeSeconds().ToString();
    }

    var content = new FormUrlEncodedContent(dictionary);

    try
    {
        var response = await client.PostAsync("/api/data/text", content);
        await response.Content.ReadAsStringAsync();
    }
    catch
    {
        // Ignored
    }
}