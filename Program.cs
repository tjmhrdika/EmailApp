using EmailApp.Desktop;
using EmailApp.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows.Forms;

namespace EmailApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.SetBasePath(AppContext.BaseDirectory);
                    configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDesktopApplicationServices(context.Configuration);
                    services.AddTransient<LoginForm>();
                    services.AddTransient<DashboardForm>();
                })
                .Build();

            host.Start();

            Application.ApplicationExit += (_, _) => host.StopAsync().GetAwaiter().GetResult();
            Application.Run(host.Services.GetRequiredService<LoginForm>());
        }
    }
}
