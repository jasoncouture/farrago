using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Farrago.Protocol.Tcp.Server;
public static class ServiceProviderExtensions
{
    public static IServiceCollection AddNativeTcpServer(this IServiceCollection services, IConfigurationSection configuration)
    {
        services.Configure<NativeTcpServerOptions>(configuration);
        services.AddHostedService<NativeTcpServerBackgroundService>();
        return services;
    }
}
