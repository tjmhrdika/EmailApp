using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class SmtpControl : UserControl
    {
        private readonly TextBox txtConnectionString;
        private readonly TextBox txtHost;
        private readonly NumericUpDown numPort;
        private readonly TextBox txtUser;
        private readonly TextBox txtPassword;
        private readonly TextBox txtFromEmail;
        private readonly Label lblStatus;
        private static readonly Guid SettingsId = new Guid("11111111-1111-1111-1111-111111111111");

        public SmtpControl()
        {
            txtConnectionString = new TextBox();
            txtHost = new TextBox();
            numPort = new NumericUpDown();
            txtUser = new TextBox();
            txtPassword = new TextBox();
            txtFromEmail = new TextBox();
            lblStatus = new Label();

            ConnectionString = "Server=localhost;Database=EmailDB;Trusted_Connection=True;TrustServerCertificate=True";
            SmtpHost = "smtp.gmail.com";
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
                Text = "SMTP Server",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(16, 14),
                Size = new Size(300, 30)
            };

            numPort.Maximum = 65535;
            numPort.Minimum = 0;
            txtPassword.UseSystemPasswordChar = true;

            var table = new TableLayoutPanel
            {
                Location = new Point(16, 58),
                Size = new Size(700, 245),
                ColumnCount = 2,
                RowCount = 6,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddRow(table, 0, "Connection String", txtConnectionString);
            AddRow(table, 1, "Host", txtHost);
            AddRow(table, 2, "Port", numPort);
            AddRow(table, 3, "User", txtUser);
            AddRow(table, 4, "Password", txtPassword);
            AddRow(table, 5, "From Email", txtFromEmail);

            var loadButton = CreateButton("Load", 16, 325);
            loadButton.Click += delegate { LoadSettings(); };

            var saveButton = CreateButton("Save", 126, 325);
            saveButton.BackColor = Color.FromArgb(26, 115, 232);
            saveButton.ForeColor = Color.White;
            saveButton.Click += delegate { SaveSettings(); };

            lblStatus.Location = new Point(16, 372);
            lblStatus.Size = new Size(700, 30);
            lblStatus.Font = new Font("Segoe UI", 9F);

            Controls.AddRange(new Control[] { title, table, loadButton, saveButton, lblStatus });
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

        private void LoadSettings()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand("SELECT TOP 1 Host, Port, [User], Pass, FromEmail FROM SetSmtp WHERE Id=@Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", SettingsId);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            SmtpHost = Convert.ToString(reader["Host"]);
                            SmtpPort = Convert.ToInt32(reader["Port"]);
                            SmtpUser = Convert.ToString(reader["User"]);
                            SmtpPassword = Convert.ToString(reader["Pass"]);
                            FromEmail = Convert.ToString(reader["FromEmail"]);
                        }
                    }
                }

                SetStatus("SMTP settings loaded", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand(
                    "IF EXISTS (SELECT 1 FROM SetSmtp WHERE Id=@Id) " +
                    "UPDATE SetSmtp SET Host=@Host, Port=@Port, [User]=@User, Pass=@Pass, FromEmail=@FromEmail WHERE Id=@Id " +
                    "ELSE INSERT INTO SetSmtp (Id, Host, Port, [User], Pass, FromEmail) VALUES (@Id, @Host, @Port, @User, @Pass, @FromEmail)",
                    connection))
                {
                    command.Parameters.AddWithValue("@Id", SettingsId);
                    command.Parameters.AddWithValue("@Host", SmtpHost);
                    command.Parameters.AddWithValue("@Port", SmtpPort);
                    command.Parameters.AddWithValue("@User", SmtpUser);
                    command.Parameters.AddWithValue("@Pass", SmtpPassword);
                    command.Parameters.AddWithValue("@FromEmail", FromEmail);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                SetStatus("SMTP settings saved", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private static Button CreateButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat
            };
        }

        private void SetStatus(string message, bool success)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = success ? Color.FromArgb(21, 128, 61) : Color.FromArgb(229, 57, 53);
        }
    }
}
