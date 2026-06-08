using EmailApp.Configuration;
using EmailApp.Data;
using EmailApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EmailApp.Desktop
{
    public static class DesktopServiceCollectionExtensions
    {
        public static IServiceCollection AddDesktopApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContextFactory<AlarmDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("AlarmDatabase")));

            services.Configure<EmailOptions>(configuration.GetSection("Email"));
            services.Configure<MonitoringOptions>(configuration.GetSection("Monitoring"));
            services.Configure<HostOptions>(options =>
            {
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISmtpSettingsService, SmtpSettingsService>();
            services.AddScoped<IEmailRecipientService, EmailRecipientService>();
            services.AddHostedService<AlarmMonitoringService>();

            return services;
        }
    }
}
