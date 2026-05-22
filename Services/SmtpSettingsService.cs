using EmailApp.Configuration;
using EmailApp.Data;
using EmailApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmailApp.Services
{
    public class SmtpSettingsService : ISmtpSettingsService
    {
        private static readonly Guid DefaultSettingsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly EmailOptions _emailOptions;

        public SmtpSettingsService(IDbContextFactory<AppDbContext> dbFactory, IOptions<EmailOptions> emailOptions)
        {
            _dbFactory = dbFactory;
            _emailOptions = emailOptions.Value;
        }

        public async Task<SetSmtp> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var settings = await db.SetSmtp
                .AsNoTracking()
                .FirstOrDefaultAsync(setting => setting.Id == DefaultSettingsId, cancellationToken);

            if (settings != null)
                return ApplyDefaults(settings);

            settings = CreateDefaultSettings();
            db.SetSmtp.Add(settings);
            await db.SaveChangesAsync(cancellationToken);

            return settings;
        }

        public async Task SaveSettingsAsync(SetSmtp settings, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var currentSettings = await db.SetSmtp.FirstOrDefaultAsync(s => s.Id == settings.Id, cancellationToken);

            if (currentSettings == null)
            {
                db.SetSmtp.Add(settings);
            }
            else
            {
                currentSettings.Host = settings.Host.Trim();
                currentSettings.Port = settings.Port;
                currentSettings.User = settings.User.Trim();
                currentSettings.Pass = settings.Pass;
                currentSettings.FromEmail = settings.FromEmail.Trim();
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private SetSmtp CreateDefaultSettings()
        {
            return new SetSmtp
            {
                Id = DefaultSettingsId,
                Host = _emailOptions.SmtpHost,
                Port = _emailOptions.SmtpPort,
                User = _emailOptions.SmtpUser,
                Pass = _emailOptions.SmtpPass,
                FromEmail = _emailOptions.FromEmail
            };
        }

        private SetSmtp ApplyDefaults(SetSmtp settings)
        {
            settings.Host = string.IsNullOrWhiteSpace(settings.Host) ? _emailOptions.SmtpHost : settings.Host;
            settings.Port = settings.Port == 0 ? _emailOptions.SmtpPort : settings.Port;
            settings.User = string.IsNullOrWhiteSpace(settings.User) ? _emailOptions.SmtpUser : settings.User;
            settings.Pass = string.IsNullOrWhiteSpace(settings.Pass) ? _emailOptions.SmtpPass : settings.Pass;
            settings.FromEmail = string.IsNullOrWhiteSpace(settings.FromEmail) ? _emailOptions.FromEmail : settings.FromEmail;

            return settings;
        }
    }
}
