using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glihm.Networking.PushNotifications.APNs;

/// <summary>
/// Services extension to register the provider and it's options.
/// </summary>
public static class APNsProviderServiceExtensions
{
    /// <summary>
    /// Actual extension to add the service and it's options.
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection
    AddAPNsProvider(this IServiceCollection services, IConfiguration configuration, String configKey)
    {
        services.AddSingleton<APNsProvider>();
        services.Configure<APNsOptions>(configuration.GetSection(configKey));
        return services;
    }

}
