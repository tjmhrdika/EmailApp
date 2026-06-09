using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EmailApp.Desktop
{
    public static class DesktopServiceProviderFactory
    {
        private static readonly Lazy<IHost> Host = new(CreateHost);

        public static IServiceProvider Services => Host.Value.Services;

        private static IHost CreateHost()
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.SetBasePath(AppContext.BaseDirectory);
                    configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDesktopApplicationServices(context.Configuration);
                })
                .Build();

            host.Start();
            return host;
        }
    }
}
