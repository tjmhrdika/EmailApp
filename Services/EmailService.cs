using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using EmailApp.Data;

namespace EmailApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public EmailService(IOptions<EmailSettings> settings, IDbContextFactory<AppDbContext> dbFactory)
        {
            _settings = settings.Value;
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

        public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body)
        {
            if (recipients == null || !recipients.Any())
                throw new ArgumentException("Recipients list cannot be empty", nameof(recipients));

            ValidateInput(subject, body);

            var settings = await GetEffectiveSettings();
            ValidateConfiguration(settings);

            using var message = CreateBulkMessage(recipients, subject, body, settings);
            using var client = await CreateSmtpClient(settings);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private async Task<EmailSettings> GetEffectiveSettings()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var smtp = await db.SetSmtp.FirstOrDefaultAsync();

            if (smtp == null)
                return _settings;

            return new EmailSettings
            {
                SmtpHost = string.IsNullOrWhiteSpace(smtp.Host) ? _settings.SmtpHost : smtp.Host,
                SmtpPort = smtp.Port == 0 ? _settings.SmtpPort : smtp.Port,
                SmtpUser = string.IsNullOrWhiteSpace(smtp.User) ? _settings.SmtpUser : smtp.User,
                SmtpPass = string.IsNullOrWhiteSpace(smtp.Pass) ? _settings.SmtpPass : smtp.Pass,
                FromEmail = string.IsNullOrWhiteSpace(smtp.FromEmail) ? _settings.FromEmail : smtp.FromEmail
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

        private void ValidateConfiguration(EmailSettings settings)
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

        private MimeMessage CreateMessage(string to, string subject, string body, EmailSettings settings)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            return message;
        }

        private MimeMessage CreateBulkMessage(List<string> recipients, string subject, string body, EmailSettings settings)
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

        private async Task<SmtpClient> CreateSmtpClient(EmailSettings settings)
        {
            var client = new SmtpClient();
            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPass);
            return client;
        }
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
    }
}