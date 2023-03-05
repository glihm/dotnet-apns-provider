using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Glihm.Networking.PushNotifications.APNs;

namespace PushNotificationProvider;

/// <summary>
/// Host application to have DI context.
/// </summary>
public class HostApp
{
    /// <summary>
    /// IHost to access services.
    /// </summary>
    public IHost Host { get; }

    /// <summary>
    /// Host application.
    /// </summary>
    public HostApp()
    {
        this.Host = this._initializeHostBuilder();
    }

    /// <summary>
    /// Configures host for DI.
    /// </summary>
    /// <returns></returns>
    private IHost
    _initializeHostBuilder()
    {
        IHost host = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: false)
                  .AddEnvironmentVariables()
                  .Build();

                services.AddHttpClient();

                services.AddAPNsProvider(configuration, "APNs");

                services.AddLogging(c => {
                    c.AddFilter(null, LogLevel.Debug);
                    c.AddDebug();
                    c.AddConsole();
                    //c.AddConfiguration(configuration);
                });
            })
            .UseConsoleLifetime()
            .Build();

        return host;
    }
}
