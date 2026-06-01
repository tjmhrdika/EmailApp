using System.Text;
using EmailApp.Configuration;
using EmailApp.Data;
using EmailApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace EmailApp.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddRazorComponents()
                .AddInteractiveServerComponents();

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
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

            services.AddJwtAuthentication(configuration);
            services.AddAuthorization();
            services.AddCascadingAuthenticationState();
            services.AddHttpContextAccessor();
            services.AddServerApiClient(configuration);
            services.AddControllers();

            return services;
        }

        private static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtKey = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/";
                    options.Cookie.Name = "EmailApp.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                        ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            if (IsBrowserPageRequest(context.Request))
                            {
                                context.HandleResponse();
                                var returnUrl = Uri.EscapeDataString($"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");
                                context.Response.Redirect($"/login?returnUrl={returnUrl}");
                            }

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            if (IsBrowserPageRequest(context.Request))
                            {
                                context.Response.Redirect("/");
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        private static bool IsBrowserPageRequest(HttpRequest request)
        {
            return !request.Path.StartsWithSegments("/api") &&
                   !request.Path.StartsWithSegments("/login") &&
                   request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static IServiceCollection AddServerApiClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "http://localhost:5146");
            });

            services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

            return services;
        }
    }
}
