using EmailApp.Data;
using EmailApp.Desktop;
using EmailApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class LoginControl : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TextBox _txtUsername = new();
        private readonly TextBox _txtPassword = new();
        private readonly Label _lblMessage = new();
        private readonly Button _btnLogin = new();
        private bool _showPassword;

        public LoginControl()
            : this(DesktopServiceProviderFactory.Services)
        {
        }

        public LoginControl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        public event EventHandler? LoginSucceeded;

        public string Username
        {
            get => _txtUsername.Text;
            set => _txtUsername.Text = value;
        }

        public string Password
        {
            get => _txtPassword.Text;
            set => _txtPassword.Text = value;
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(241, 243, 246);

            var card = new Panel
            {
                BackColor = Color.White,
                Size = new Size(400, 350),
                Anchor = AnchorStyles.None,
                Location = new Point((Width - 400) / 2, (Height - 350) / 2)
            };
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Resize += (_, _) => CenterCard(card);
            Resize += (_, _) => CenterCard(card);

            var title = new Label
            {
                Text = "Login",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 26, 46),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(32, 32),
                Size = new Size(336, 42)
            };

            var subtitle = new Label
            {
                Text = "Masuk ke CIP Station",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(95, 99, 104),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(32, 74),
                Size = new Size(336, 24)
            };

            _txtUsername.Location = new Point(48, 130);
            _txtUsername.Size = new Size(304, 31);
            _txtUsername.Font = new Font("Segoe UI", 11);
            _txtUsername.PlaceholderText = "Username";

            _txtPassword.Location = new Point(48, 180);
            _txtPassword.Size = new Size(260, 31);
            _txtPassword.Font = new Font("Segoe UI", 11);
            _txtPassword.PlaceholderText = "Password";
            _txtPassword.UseSystemPasswordChar = true;

            var btnShowPassword = new Button
            {
                Text = "Show",
                Location = new Point(314, 180),
                Size = new Size(38, 31),
                FlatStyle = FlatStyle.Flat
            };
            btnShowPassword.Click += (_, _) => TogglePassword();

            _btnLogin.Text = "Login";
            _btnLogin.Location = new Point(48, 238);
            _btnLogin.Size = new Size(304, 42);
            _btnLogin.BackColor = Color.FromArgb(26, 115, 232);
            _btnLogin.ForeColor = Color.White;
            _btnLogin.FlatStyle = FlatStyle.Flat;
            _btnLogin.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            _btnLogin.Click += async (_, _) => await LoginAsync();

            _lblMessage.Location = new Point(48, 292);
            _lblMessage.Size = new Size(304, 40);
            _lblMessage.ForeColor = Color.FromArgb(229, 57, 53);
            _lblMessage.Font = new Font("Segoe UI", 9);
            _lblMessage.TextAlign = ContentAlignment.MiddleCenter;

            card.Controls.AddRange([title, subtitle, _txtUsername, _txtPassword, btnShowPassword, _btnLogin, _lblMessage]);
            Controls.Add(card);
            CenterCard(card);
        }

        private async Task LoginAsync()
        {
            _lblMessage.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _lblMessage.Text = "Username dan password wajib diisi";
                return;
            }

            _btnLogin.Enabled = false;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var db = await dbFactory.CreateDbContextAsync();

                var user = await db.Users.FirstOrDefaultAsync(item => item.Username == Username.Trim());

                if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
                {
                    _lblMessage.Text = "Username atau password salah";
                    return;
                }

                CurrentUser = user;
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _lblMessage.Text = ex.Message;
            }
            finally
            {
                _btnLogin.Enabled = true;
            }
        }

        private void TogglePassword()
        {
            _showPassword = !_showPassword;
            _txtPassword.UseSystemPasswordChar = !_showPassword;
        }

        private void CenterCard(Control card)
        {
            card.Left = Math.Max(0, (ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(0, (ClientSize.Height - card.Height) / 2);
        }

        public User? CurrentUser { get; private set; }
    }
}
