using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using EmailApp.Configuration;
using EmailApp.Data;

namespace EmailApp.Services
{
    public class EmailService : IEmailService
    {
        private static readonly Guid DefaultSmtpSettingsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private readonly EmailOptions _options;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public EmailService(IOptions<EmailOptions> options, IDbContextFactory<AppDbContext> dbFactory)
        {
            _options = options.Value;
            _dbFactory = dbFactory;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            ValidateInput(to, subject, body);

            var settings = await GetEffectiveSettings();
            ValidateConfiguration(settings);

            using var message = CreateMessage(to, subject, body, settings);
            using var client = await CreateSmtpClient(settings);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body)
        {
            var recipientList = recipients
                .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!recipientList.Any())
                throw new ArgumentException("Recipients list cannot be empty", nameof(recipients));

            ValidateInput(subject, body);

            var settings = await GetEffectiveSettings();
            ValidateConfiguration(settings);

            using var message = CreateBulkMessage(recipientList, subject, body, settings);
            using var client = await CreateSmtpClient(settings);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private async Task<EmailOptions> GetEffectiveSettings()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var smtp = await db.SetSmtp
                .AsNoTracking()
                .FirstOrDefaultAsync(settings => settings.Id == DefaultSmtpSettingsId);

            if (smtp == null)
                return _options;

            return new EmailOptions
            {
                SmtpHost = string.IsNullOrWhiteSpace(smtp.Host) ? _options.SmtpHost : smtp.Host,
                SmtpPort = smtp.Port == 0 ? _options.SmtpPort : smtp.Port,
                SmtpUser = string.IsNullOrWhiteSpace(smtp.User) ? _options.SmtpUser : smtp.User,
                SmtpPass = string.IsNullOrWhiteSpace(smtp.Pass) ? _options.SmtpPass : smtp.Pass,
                FromEmail = string.IsNullOrWhiteSpace(smtp.FromEmail) ? _options.FromEmail : smtp.FromEmail
            };
        }

        private static void ValidateInput(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Recipient email address is required", nameof(to));
            
            ValidateInput(subject, body);
        }

        private static void ValidateInput(string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject is required", nameof(subject));
            
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body is required", nameof(body));
        }

        private static void ValidateConfiguration(EmailOptions settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SmtpHost))
                throw new InvalidOperationException("SMTP Host is not configured");

            if (settings.SmtpPort <= 0)
                throw new InvalidOperationException("SMTP Port is not configured");

            if (string.IsNullOrWhiteSpace(settings.SmtpUser))
                throw new InvalidOperationException("SMTP User is not configured");

            if (string.IsNullOrWhiteSpace(settings.SmtpPass))
                throw new InvalidOperationException("SMTP Password is not configured");

            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new InvalidOperationException("From Email is not configured");
        }

        private static MimeMessage CreateMessage(string to, string subject, string body, EmailOptions settings)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            return message;
        }

        private static MimeMessage CreateBulkMessage(IEnumerable<string> recipients, string subject, string body, EmailOptions settings)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.FromEmail));

            foreach (var recipient in recipients)
            {
                message.Bcc.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            return message;
        }

        private static async Task<SmtpClient> CreateSmtpClient(EmailOptions settings)
        {
            var client = new SmtpClient();
            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPass);
            return client;
        }
    }
}
