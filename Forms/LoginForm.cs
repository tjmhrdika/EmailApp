using EmailApp.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Forms
{
    public class LoginForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LoginControl _loginControl;

        public LoginForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _loginControl = new LoginControl(serviceProvider);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "CIP Station Alarm Notification - Login";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            Size = new Size(1024, 680);
            Controls.Add(_loginControl);

            _loginControl.LoginSucceeded += (_, _) =>
            {
                var dashboard = ActivatorUtilities.CreateInstance<DashboardForm>(_serviceProvider, _loginControl.CurrentUser);
                Hide();
                dashboard.FormClosed += (_, _) => Close();
                dashboard.Show();
            };
        }
    }
}
