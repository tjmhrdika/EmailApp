using EmailApp.Desktop;
using EmailApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class ManualEmailControl : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TextBox _txtSubject = new();
        private readonly TextBox _txtBody = new();
        private readonly Label _lblStatus = new();
        private readonly Button _btnSend = new();

        public ManualEmailControl()
            : this(DesktopServiceProviderFactory.Services)
        {
        }

        public ManualEmailControl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(24);

            var title = new Label
            {
                Text = "Manual Email",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(24, 24),
                Size = new Size(300, 32)
            };

            var subtitle = new Label
            {
                Text = "Kirim pesan manual ke semua recipient aktif",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(107, 122, 141),
                Location = new Point(24, 58),
                Size = new Size(420, 22)
            };

            var subjectLabel = CreateLabel("Subject", 24, 105);
            _txtSubject.Location = new Point(24, 130);
            _txtSubject.Size = new Size(620, 31);
            _txtSubject.Font = new Font("Segoe UI", 10);
            _txtSubject.Text = "Alarm Notification";

            var bodyLabel = CreateLabel("Message", 24, 180);
            _txtBody.Location = new Point(24, 205);
            _txtBody.Size = new Size(620, 220);
            _txtBody.Font = new Font("Segoe UI", 10);
            _txtBody.Multiline = true;
            _txtBody.ScrollBars = ScrollBars.Vertical;

            _btnSend.Text = "Send to all";
            _btnSend.BackColor = Color.FromArgb(26, 115, 232);
            _btnSend.ForeColor = Color.White;
            _btnSend.FlatStyle = FlatStyle.Flat;
            _btnSend.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _btnSend.Location = new Point(24, 450);
            _btnSend.Size = new Size(130, 38);
            _btnSend.Click += async (_, _) => await SendAsync();

            _lblStatus.Location = new Point(24, 505);
            _lblStatus.Size = new Size(620, 46);
            _lblStatus.Font = new Font("Segoe UI", 9);

            Controls.AddRange([title, subtitle, subjectLabel, _txtSubject, bodyLabel, _txtBody, _btnSend, _lblStatus]);
        }

        private async Task SendAsync()
        {
            if (string.IsNullOrWhiteSpace(_txtBody.Text))
            {
                SetStatus("Message cannot be empty", false);
                return;
            }

            _btnSend.Enabled = false;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var recipientService = scope.ServiceProvider.GetRequiredService<IEmailRecipientService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var recipients = (await recipientService.GetEmailsAsync())
                    .Select(email => email.Address)
                    .Where(address => !string.IsNullOrWhiteSpace(address))
                    .ToList();

                if (recipients.Count == 0)
                {
                    SetStatus("No email recipients configured", false);
                    return;
                }

                await emailService.SendBulkEmailAsync(recipients, _txtSubject.Text, _txtBody.Text);
                _txtBody.Clear();
                SetStatus($"Email sent to {recipients.Count} recipient(s)", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
            finally
            {
                _btnSend.Enabled = true;
            }
        }

        private static Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(160, 22),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
        }

        private void SetStatus(string message, bool success)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = success ? Color.FromArgb(21, 128, 61) : Color.FromArgb(229, 57, 53);
        }
    }
}
