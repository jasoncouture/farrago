using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore;
using Farrago.Core.KeyValueStore.Commands;
using Farrago.Core.KeyValueStore.Commands.Batch;
using Farrago.Core.KeyValueStore.Commands.Batch.Handler;
using Farrago.Core.KeyValueStore.Commands.Blob;
using Farrago.Core.KeyValueStore.Commands.Blob.Handler;
using Farrago.Core.KeyValueStore.Commands.Processor;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Farrago.Core.KeyValueStore.Commands.Shared.Handler;
using Farrago.Core.KeyValueStore.Commands.String;
using Farrago.Core.KeyValueStore.Commands.String.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Clustering.Kubernetes;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Statistics;
using OrleansDashboard;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Farrago.Core;

public static class HostBuilderExtensions
{
    private static readonly DashboardOptions _dashboardOptions = new DashboardOptions()
        {BasePath = "/dashboard", HostSelf = false};

    public static IApplicationBuilder UseFarrago(this IApplicationBuilder webApplication)
    {
        webApplication.UseOrleansDashboard(_dashboardOptions);

        return webApplication;
    }

    public static IHostBuilder AddFarrago(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseOrleans(ConfigureSiloBuilder)
            .ConfigureServices(ConfigureFarragoServices);
        return hostBuilder;
    }

    private static void ConfigureFarragoServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    {
        services.PostConfigure<ClusterOptions>(o =>
        {
            // Set defaults if the cluster options were not configured.
            if (string.IsNullOrWhiteSpace(o.ClusterId))
                o.ClusterId = Environment.GetEnvironmentVariable("CLUSTER_ID") ?? DefaultClusterId;
            if (string.IsNullOrWhiteSpace(o.ServiceId))
                o.ServiceId = Environment.GetEnvironmentVariable("SERVICE_ID") ?? o.ClusterId ?? DefaultServiceId;
        });

        services.AddTransient<IStorageGrain, StorageGrain>();
        services.AddTransient<ICommandProcessor, CommandProcessor>();

        services.AddSingleton<ITypedFarragoCommandProcessor<GetBlobCommand>, GetBlobCommandHandler>();
        services.AddSingleton<ITypedFarragoCommandProcessor<SetBlobCommand>, SetBlobCommandHandler>();
        services.AddSingleton<ITypedFarragoCommandProcessor<DeleteCommand>, DeleteDataCommandHandler>();
        services.AddSingleton<ITypedFarragoCommandProcessor<SetStringCommand>, SetStringCommandHandler>();
        services.AddSingleton<ITypedFarragoCommandProcessor<GetStringCommand>, GetStringCommandHandler>();
        services.AddSingleton<ITypedFarragoCommandProcessor<ExpireCommand>, ExpireCommandHandler>();
        services.AddSingleton<ITypedFarragoCommandProcessor<BatchCommand>, BatchCommandHandler>();
    }

    private const string DefaultClusterId = "farrago-cluster";
    private const string DefaultServiceId = DefaultClusterId;

    private static void ConfigureSiloBuilder(HostBuilderContext hostBuilderContext, ISiloBuilder siloBuilder)
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                siloBuilder.UsePerfCounterEnvironmentStatistics();
                break;
            case PlatformID.Unix:
                siloBuilder.UseLinuxEnvironmentStatistics();
                break;
        }

        siloBuilder.ConfigureApplicationParts(c =>
            c.AddApplicationPart(typeof(HostBuilderExtensions).Assembly).WithReferences());
        siloBuilder.Configure<ClusterOptions>(hostBuilderContext.Configuration);
        ConfigureClusteringMethod(hostBuilderContext, siloBuilder);

        siloBuilder.UseDashboard(o =>
        {
            o.BasePath = _dashboardOptions.BasePath;
            o.HostSelf = _dashboardOptions.HostSelf;
        });
    }

    private static void ConfigureClusteringMethod(HostBuilderContext hostBuilderContext, ISiloBuilder siloBuilder)
    {
        var clusterMethod = hostBuilderContext.Configuration.GetValue("ClusterProvider", ClusterMethod.None);
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")) &&
            Environment.GetEnvironmentVariable("FARRAGO_KUBERNETES_HOSTING") == "1")
        {
            siloBuilder.UseKubernetesHosting();
        }
        else
        {
            siloBuilder.ConfigureEndpoints(IPAddress.Parse(
                    hostBuilderContext.Configuration.GetValue<string>("AdvertisedIPAddress") ??
                    GetDefaultIPAddress().ToString()),
                hostBuilderContext.Configuration.GetValue<int>("SiloPort", 11111),
                hostBuilderContext.Configuration.GetValue<int>("GatewayPort", 30000),
                listenOnAnyHostAddress: true
            );
        }

        switch (clusterMethod)
        {
            case ClusterMethod.SingleNode:
            case ClusterMethod.None:
                siloBuilder.UseLocalhostClustering();
                break;
            case ClusterMethod.Kubernetes:
                siloBuilder.UseKubeMembership();
                break;
            case ClusterMethod.Redis:
                var redisOptions = hostBuilderContext.Configuration.GetSection("Redis")
                    .Get<RedisClusterMethodConfiguration>();
                siloBuilder.UseRedisClustering(redisOptions.ConnectionString, redisOptions.Database);
                break;
            default:
                throw new InvalidOperationException($"Unsupported clustering method {clusterMethod}");
        }
    }

    private static IPAddress GetDefaultIPAddress()
    {
        var possibleIPAddresses = new HashSet<IPAddress>();
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var networkInterface in interfaces)
        {
            switch (networkInterface.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                case NetworkInterfaceType.GigabitEthernet:
                case NetworkInterfaceType.Wireless80211:
                case NetworkInterfaceType.Unknown:
                    break;
                default:
                    continue;
            }

            switch (networkInterface.OperationalStatus)
            {
                case OperationalStatus.Down:
                case OperationalStatus.NotPresent:
                case OperationalStatus.LowerLayerDown:
                case OperationalStatus.Dormant:
                    continue;
            }

            var ipProperties = networkInterface.GetIPProperties();

            foreach (var address in ipProperties.UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (Equals(address.Address, IPAddress.Loopback) ||
                    Equals(address.Address, IPAddress.Broadcast) ||
                    Equals(address.Address, IPAddress.Any))
                    continue;

                possibleIPAddresses.Add(address.Address);
            }
        }

        if (possibleIPAddresses.Count != 1)
        {
            throw new InvalidOperationException(
                "Unable to determine default advertise IP Address, please set the advertised IP Address in the configuration");
        }

        return possibleIPAddresses.Single();
    }
}