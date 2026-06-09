using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class EmailAppControl : UserControl
    {
        private readonly RecipientControl recipientControl;
        private readonly SmtpControl smtpControl;
        private readonly ManualEmailControl manualEmailControl;

        public EmailAppControl()
        {
            recipientControl = new RecipientControl();
            smtpControl = new SmtpControl();
            manualEmailControl = new ManualEmailControl();
            InitializeComponent();
        }

        public string ConnectionString
        {
            get { return recipientControl.ConnectionString; }
            set
            {
                recipientControl.ConnectionString = value;
                smtpControl.ConnectionString = value;
                manualEmailControl.ConnectionString = value;
            }
        }

        public string SmtpHost
        {
            get { return manualEmailControl.SmtpHost; }
            set
            {
                manualEmailControl.SmtpHost = value;
                smtpControl.SmtpHost = value;
            }
        }

        public int SmtpPort
        {
            get { return manualEmailControl.SmtpPort; }
            set
            {
                manualEmailControl.SmtpPort = value;
                smtpControl.SmtpPort = value;
            }
        }

        public string SmtpUser
        {
            get { return manualEmailControl.SmtpUser; }
            set
            {
                manualEmailControl.SmtpUser = value;
                smtpControl.SmtpUser = value;
            }
        }

        public string SmtpPassword
        {
            get { return manualEmailControl.SmtpPassword; }
            set
            {
                manualEmailControl.SmtpPassword = value;
                smtpControl.SmtpPassword = value;
            }
        }

        public string FromEmail
        {
            get { return manualEmailControl.FromEmail; }
            set
            {
                manualEmailControl.FromEmail = value;
                smtpControl.FromEmail = value;
            }
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(248, 250, 252);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };

            var recipientTab = new TabPage("Recipients");
            recipientTab.Controls.Add(recipientControl);
            recipientControl.Dock = DockStyle.Fill;

            var smtpTab = new TabPage("SMTP");
            smtpTab.Controls.Add(smtpControl);
            smtpControl.Dock = DockStyle.Fill;

            var manualTab = new TabPage("Manual Email");
            manualTab.Controls.Add(manualEmailControl);
            manualEmailControl.Dock = DockStyle.Fill;

            tabs.TabPages.Add(recipientTab);
            tabs.TabPages.Add(smtpTab);
            tabs.TabPages.Add(manualTab);
            Controls.Add(tabs);
        }
    }
}
