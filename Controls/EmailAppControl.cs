using EmailApp.Desktop;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class EmailAppControl : UserControl
    {
        private readonly IServiceProvider _serviceProvider;

        public EmailAppControl()
            : this(DesktopServiceProviderFactory.Services)
        {
        }

        public EmailAppControl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(248, 250, 252);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            var recipientsTab = new TabPage("Recipients");
            recipientsTab.Controls.Add(new RecipientControl(_serviceProvider) { Dock = DockStyle.Fill });

            var smtpTab = new TabPage("SMTP");
            smtpTab.Controls.Add(new SmtpControl(_serviceProvider) { Dock = DockStyle.Fill });

            var manualEmailTab = new TabPage("Manual Email");
            manualEmailTab.Controls.Add(new ManualEmailControl(_serviceProvider) { Dock = DockStyle.Fill });

            tabs.TabPages.AddRange([recipientsTab, smtpTab, manualEmailTab]);
            Controls.Add(tabs);
        }
    }
}
