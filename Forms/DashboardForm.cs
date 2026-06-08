using EmailApp.Controls;
using EmailApp.Models;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Forms
{
    public class DashboardForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly User? _currentUser;

        public DashboardForm(IServiceProvider serviceProvider, User? currentUser = null)
        {
            _serviceProvider = serviceProvider;
            _currentUser = currentUser;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "CIP Station Alarm Notification";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 700);
            Size = new Size(1280, 760);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.White
            };

            var title = new Label
            {
                Text = "Email Alarm Notification",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(20, 13),
                Size = new Size(360, 28)
            };

            var userLabel = new Label
            {
                Text = _currentUser == null ? string.Empty : $"{_currentUser.Username} - {(_currentUser.IsAdmin ? "Administrator" : "User")}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(95, 99, 104),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(760, 17),
                Size = new Size(360, 22)
            };

            var logoutButton = new Button
            {
                Text = "Logout",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(1130, 13),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat
            };
            logoutButton.Click += (_, _) => Close();

            header.Controls.AddRange([title, userLabel, logoutButton]);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            var emailTab = new TabPage("Recipients");
            emailTab.Controls.Add(new RecipientControl(_serviceProvider) { Dock = DockStyle.Fill });

            var smtpTab = new TabPage("SMTP");
            smtpTab.Controls.Add(new SmtpControl(_serviceProvider) { Dock = DockStyle.Fill });

            var manualTab = new TabPage("Manual Email");
            manualTab.Controls.Add(new ManualEmailControl(_serviceProvider) { Dock = DockStyle.Fill });

            tabs.TabPages.AddRange([emailTab, smtpTab, manualTab]);

            Controls.Add(tabs);
            Controls.Add(header);
        }
    }
}
