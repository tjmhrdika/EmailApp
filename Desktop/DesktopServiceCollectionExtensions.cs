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
            var defaultConnection = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=EmailDB;Trusted_Connection=True;TrustServerCertificate=True";
            var alarmConnection = configuration.GetConnectionString("AlarmDatabase")
                ?? "Server=localhost;Database=WWALMDB;Trusted_Connection=True;TrustServerCertificate=True";

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(defaultConnection));

            services.AddDbContextFactory<AlarmDbContext>(options =>
                options.UseSqlServer(alarmConnection));

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
