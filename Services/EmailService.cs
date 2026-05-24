using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using EmailApp.Configuration;
using EmailApp.Data;
using System.Net;
using System.Text;

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
            message.Body = CreateBody(subject, body);

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
            message.Body = CreateBody(subject, body);

            return message;
        }

        private static MimeEntity CreateBody(string subject, string body)
        {
            var normalizedBody = NormalizeBody(body);
            var builder = new BodyBuilder
            {
                TextBody = normalizedBody,
                HtmlBody = CreateHtmlBody(subject, normalizedBody)
            };

            return builder.ToMessageBody();
        }

        private static string NormalizeBody(string body)
        {
            return body
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Trim();
        }

        private static string CreateHtmlBody(string subject, string body)
        {
            var content = CreateHtmlContent(body);
            var encodedSubject = HtmlEncode(subject);
            var sentAt = DateTime.Now.ToString("dd MMM yyyy HH:mm");

            return $"""
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
</head>
<body style="margin:0;padding:0;background:#f3f6fb;font-family:Arial,Helvetica,sans-serif;color:#0f172a;">
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f3f6fb;padding:28px 12px;">
        <tr>
            <td align="center">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:680px;background:#ffffff;border:1px solid #dbe4f0;border-radius:14px;overflow:hidden;">
                    <tr>
                        <td style="background:#0f62fe;padding:22px 28px;">
                            <div style="font-size:12px;line-height:18px;text-transform:uppercase;letter-spacing:.08em;color:#dbeafe;font-weight:700;">Alarm Notification</div>
                            <div style="margin-top:6px;font-size:22px;line-height:30px;color:#ffffff;font-weight:700;">{encodedSubject}</div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:26px 28px;">
                            {content}
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin-top:24px;border-top:1px solid #e2e8f0;">
                                <tr>
                                    <td style="padding-top:16px;font-size:12px;line-height:18px;color:#64748b;">Sent by CIP Station Alarm Notification at {HtmlEncode(sentAt)}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
        }

        private static string CreateHtmlContent(string body)
        {
            var lines = body
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.All(character => character == '=' || character == '-' || character == '_'))
                .ToList();

            var detailRows = new StringBuilder();
            var paragraphs = new List<string>();

            foreach (var line in lines)
            {
                if (line.Contains("notification", StringComparison.OrdinalIgnoreCase) && line.Length <= 40)
                    continue;

                var separatorIndex = line.IndexOf(':');

                if (separatorIndex > 0 && separatorIndex <= 32)
                {
                    var label = HtmlEncode(line[..separatorIndex].Trim());
                    var value = HtmlEncode(line[(separatorIndex + 1)..].Trim());

                    detailRows.Append($"""
                                <tr>
                                    <td style="width:34%;padding:12px 14px;border-bottom:1px solid #e2e8f0;background:#f8fafc;font-size:13px;line-height:18px;color:#475569;font-weight:700;">{label}</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #e2e8f0;font-size:14px;line-height:20px;color:#0f172a;">{value}</td>
                                </tr>
""");
                    continue;
                }

                paragraphs.Add(HtmlEncode(line));
            }

            var content = new StringBuilder();

            if (detailRows.Length > 0)
            {
                content.Append($"""
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border:1px solid #e2e8f0;border-radius:10px;border-collapse:separate;border-spacing:0;overflow:hidden;">
{detailRows}
                            </table>
""");
            }

            if (paragraphs.Any())
            {
                var paragraphBody = string.Join("<br>", paragraphs);

                content.Append($"""
                            <div style="margin-top:18px;padding:16px 18px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;font-size:14px;line-height:22px;color:#334155;">{paragraphBody}</div>
""");
            }

            if (content.Length == 0)
                return """<div style="font-size:14px;line-height:22px;color:#334155;">No message content.</div>""";

            return content.ToString();
        }

        private static string HtmlEncode(string value)
        {
            return WebUtility.HtmlEncode(value);
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
