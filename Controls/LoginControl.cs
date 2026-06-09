using System;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class LoginControl : UserControl
    {
        private readonly TextBox txtUsername;
        private readonly TextBox txtPassword;
        private readonly Label lblMessage;
        private bool showPassword;

        public LoginControl()
        {
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            lblMessage = new Label();
            InitializeComponent();
        }

        public event EventHandler LoginSucceeded;

        public string Username
        {
            get { return txtUsername.Text; }
            set { txtUsername.Text = value ?? string.Empty; }
        }

        public string Password
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value ?? string.Empty; }
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(241, 243, 246);

            var card = new Panel
            {
                BackColor = Color.White,
                Size = new Size(400, 320),
                Anchor = AnchorStyles.None,
                BorderStyle = BorderStyle.FixedSingle
            };
            Resize += delegate { CenterCard(card); };

            var title = new Label
            {
                Text = "Login",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 26, 46),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(32, 32),
                Size = new Size(336, 42)
            };

            txtUsername.Location = new Point(48, 115);
            txtUsername.Size = new Size(304, 26);
            txtUsername.Font = new Font("Segoe UI", 10F);

            txtPassword.Location = new Point(48, 160);
            txtPassword.Size = new Size(240, 26);
            txtPassword.Font = new Font("Segoe UI", 10F);
            txtPassword.UseSystemPasswordChar = true;

            var showButton = new Button
            {
                Text = "Show",
                Location = new Point(296, 159),
                Size = new Size(56, 28),
                FlatStyle = FlatStyle.Flat
            };
            showButton.Click += delegate { TogglePassword(); };

            var loginButton = new Button
            {
                Text = "Login",
                Location = new Point(48, 215),
                Size = new Size(304, 40),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            loginButton.Click += delegate { RaiseLoginSucceeded(); };

            lblMessage.Location = new Point(48, 265);
            lblMessage.Size = new Size(304, 28);
            lblMessage.ForeColor = Color.FromArgb(95, 99, 104);
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblMessage.Text = "Client control login placeholder";

            card.Controls.AddRange(new Control[] { title, txtUsername, txtPassword, showButton, loginButton, lblMessage });
            Controls.Add(card);
            CenterCard(card);
        }

        private void RaiseLoginSucceeded()
        {
            if (LoginSucceeded != null)
                LoginSucceeded(this, EventArgs.Empty);
        }

        private void TogglePassword()
        {
            showPassword = !showPassword;
            txtPassword.UseSystemPasswordChar = !showPassword;
        }

        private void CenterCard(Control card)
        {
            card.Left = Math.Max(0, (ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(0, (ClientSize.Height - card.Height) / 2);
        }
    }
}
