using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace EmailApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            ValidateInput(to, subject, body);
            ValidateConfiguration();

            using var message = CreateMessage(to, subject, body);
            using var client = await CreateSmtpClient();

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body)
        {
            if (recipients == null || !recipients.Any())
                throw new ArgumentException("Recipients list cannot be empty", nameof(recipients));

            ValidateInput(subject, body);
            ValidateConfiguration();

            using var message = CreateBulkMessage(recipients, subject, body);
            using var client = await CreateSmtpClient();

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
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

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
                throw new InvalidOperationException("SMTP Host is not configured");
            
            if (_settings.SmtpPort <= 0)
                throw new InvalidOperationException("SMTP Port is not configured");
            
            if (string.IsNullOrWhiteSpace(_settings.SmtpUser))
                throw new InvalidOperationException("SMTP User is not configured");
            
            if (string.IsNullOrWhiteSpace(_settings.SmtpPass))
                throw new InvalidOperationException("SMTP Password is not configured");
            
            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                throw new InvalidOperationException("From Email is not configured");
        }

        private MimeMessage CreateMessage(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };
            
            return message;
        }

        private MimeMessage CreateBulkMessage(List<string> recipients, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.FromEmail));
            
            foreach (var recipient in recipients)
            {
                message.Bcc.Add(MailboxAddress.Parse(recipient));
            }
            
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };
            
            return message;
        }

        private async Task<SmtpClient> CreateSmtpClient()
        {
            var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPass);
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