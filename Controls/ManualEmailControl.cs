using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class ManualEmailControl : UserControl
    {
        private readonly TextBox txtConnectionString;
        private readonly TextBox txtHost;
        private readonly NumericUpDown numPort;
        private readonly TextBox txtUser;
        private readonly TextBox txtPassword;
        private readonly TextBox txtFromEmail;
        private readonly TextBox txtSubject;
        private readonly TextBox txtBody;
        private readonly Label lblStatus;

        public ManualEmailControl()
        {
            txtConnectionString = new TextBox();
            txtHost = new TextBox();
            numPort = new NumericUpDown();
            txtUser = new TextBox();
            txtPassword = new TextBox();
            txtFromEmail = new TextBox();
            txtSubject = new TextBox();
            txtBody = new TextBox();
            lblStatus = new Label();

            ConnectionString = "Server=localhost;Database=EmailDB;Trusted_Connection=True;TrustServerCertificate=True";
            SmtpHost = "smtp.gmail.com";
            txtSubject.Text = "Alarm Notification";
            InitializeComponent();
            SmtpPort = 587;
        }

        public string ConnectionString
        {
            get { return txtConnectionString.Text; }
            set { txtConnectionString.Text = value ?? string.Empty; }
        }

        public string SmtpHost
        {
            get { return txtHost.Text; }
            set { txtHost.Text = value ?? string.Empty; }
        }

        public int SmtpPort
        {
            get { return (int)numPort.Value; }
            set { numPort.Value = Math.Max(numPort.Minimum, Math.Min(numPort.Maximum, value)); }
        }

        public string SmtpUser
        {
            get { return txtUser.Text; }
            set { txtUser.Text = value ?? string.Empty; }
        }

        public string SmtpPassword
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value ?? string.Empty; }
        }

        public string FromEmail
        {
            get { return txtFromEmail.Text; }
            set { txtFromEmail.Text = value ?? string.Empty; }
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(16);

            var title = new Label
            {
                Text = "Manual Email",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(16, 14),
                Size = new Size(260, 30)
            };

            numPort.Maximum = 65535;
            numPort.Minimum = 0;
            txtPassword.UseSystemPasswordChar = true;

            var table = new TableLayoutPanel
            {
                Location = new Point(16, 58),
                Size = new Size(760, 245),
                ColumnCount = 2,
                RowCount = 6,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddRow(table, 0, "Connection String", txtConnectionString);
            AddRow(table, 1, "SMTP Host", txtHost);
            AddRow(table, 2, "SMTP Port", numPort);
            AddRow(table, 3, "SMTP User", txtUser);
            AddRow(table, 4, "SMTP Password", txtPassword);
            AddRow(table, 5, "From Email", txtFromEmail);

            var subjectLabel = CreateLabel("Subject", 16, 325, 100);
            txtSubject.Location = new Point(120, 322);
            txtSubject.Size = new Size(656, 24);
            txtSubject.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var bodyLabel = CreateLabel("Message", 16, 360, 100);
            txtBody.Location = new Point(120, 358);
            txtBody.Size = new Size(656, 135);
            txtBody.Multiline = true;
            txtBody.ScrollBars = ScrollBars.Vertical;
            txtBody.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            var sendButton = new Button
            {
                Text = "Send to all",
                Location = new Point(120, 510),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            sendButton.Click += delegate { SendEmail(); };

            lblStatus.Location = new Point(260, 514);
            lblStatus.Size = new Size(516, 32);
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Font = new Font("Segoe UI", 9F);

            Controls.AddRange(new Control[] { title, table, subjectLabel, txtSubject, bodyLabel, txtBody, sendButton, lblStatus });
        }

        private void SendEmail()
        {
            if (string.IsNullOrWhiteSpace(txtBody.Text))
            {
                SetStatus("Message is required", false);
                return;
            }

            try
            {
                var recipients = LoadRecipients();
                if (recipients.Count == 0)
                {
                    SetStatus("No recipients configured", false);
                    return;
                }

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(FromEmail);
                    foreach (var recipient in recipients)
                        message.To.Add(recipient);
                    message.Subject = txtSubject.Text;
                    message.Body = txtBody.Text;

                    using (var client = new SmtpClient(SmtpHost, SmtpPort))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(SmtpUser, SmtpPassword);
                        client.Send(message);
                    }
                }

                txtBody.Clear();
                SetStatus("Email sent to " + recipients.Count + " recipient(s)", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private List<string> LoadRecipients()
        {
            var recipients = new List<string>();

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand("SELECT Address FROM Emails WHERE Address IS NOT NULL AND Address <> '' ORDER BY Address", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        recipients.Add(Convert.ToString(reader["Address"]));
                }
            }

            return recipients;
        }

        private static void AddRow(TableLayoutPanel table, int row, string labelText, Control input)
        {
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 7);
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(input, 1, row);
        }

        private static Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        private void SetStatus(string message, bool success)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = success ? Color.FromArgb(21, 128, 61) : Color.FromArgb(229, 57, 53);
        }
    }
}
