using EmailApp.Models;
using EmailApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class SmtpControl : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TextBox _txtHost = new();
        private readonly NumericUpDown _numPort = new();
        private readonly TextBox _txtUser = new();
        private readonly TextBox _txtPassword = new();
        private readonly TextBox _txtFromEmail = new();
        private readonly Label _lblStatus = new();
        private SetSmtp _settings = new();

        public SmtpControl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            _ = LoadSettingsAsync();
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(24);

            var title = new Label
            {
                Text = "SMTP Server",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(24, 24),
                Size = new Size(300, 32)
            };

            var subtitle = new Label
            {
                Text = "Konfigurasi server pengirim email alarm",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(107, 122, 141),
                Location = new Point(24, 58),
                Size = new Size(420, 22)
            };

            var form = new TableLayoutPanel
            {
                Location = new Point(24, 105),
                Size = new Size(620, 260),
                ColumnCount = 2,
                RowCount = 5,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(form, 0, "Host", _txtHost);
            AddRow(form, 1, "Port", _numPort);
            AddRow(form, 2, "User", _txtUser);
            AddRow(form, 3, "Password", _txtPassword);
            AddRow(form, 4, "From Email", _txtFromEmail);

            _numPort.Maximum = 65535;
            _numPort.Minimum = 0;
            _txtPassword.UseSystemPasswordChar = true;

            var saveButton = new Button
            {
                Text = "Save",
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(24, 390),
                Size = new Size(110, 38)
            };
            saveButton.Click += async (_, _) => await SaveSettingsAsync();

            var reloadButton = new Button
            {
                Text = "Reload",
                FlatStyle = FlatStyle.Flat,
                Location = new Point(144, 390),
                Size = new Size(110, 38)
            };
            reloadButton.Click += async (_, _) => await LoadSettingsAsync();

            _lblStatus.Location = new Point(24, 445);
            _lblStatus.Size = new Size(620, 28);
            _lblStatus.Font = new Font("Segoe UI", 9);

            Controls.AddRange([title, subtitle, form, saveButton, reloadButton, _lblStatus]);
        }

        private static void AddRow(TableLayoutPanel form, int row, string labelText, Control input)
        {
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 4, 0, 8);
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            form.Controls.Add(label, 0, row);
            form.Controls.Add(input, 1, row);
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISmtpSettingsService>();
                _settings = await service.GetSettingsAsync();

                _txtHost.Text = _settings.Host;
                _numPort.Value = Math.Min(_numPort.Maximum, Math.Max(_numPort.Minimum, _settings.Port));
                _txtUser.Text = _settings.User;
                _txtPassword.Text = _settings.Pass;
                _txtFromEmail.Text = _settings.FromEmail;
                SetStatus("SMTP settings loaded", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _settings.Host = _txtHost.Text.Trim();
                _settings.Port = (int)_numPort.Value;
                _settings.User = _txtUser.Text.Trim();
                _settings.Pass = _txtPassword.Text;
                _settings.FromEmail = _txtFromEmail.Text.Trim();

                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISmtpSettingsService>();
                await service.SaveSettingsAsync(_settings);
                SetStatus("SMTP settings saved", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void SetStatus(string message, bool success)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = success ? Color.FromArgb(21, 128, 61) : Color.FromArgb(229, 57, 53);
        }
    }
}
