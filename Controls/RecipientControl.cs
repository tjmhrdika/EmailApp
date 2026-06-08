using EmailApp.Models;
using EmailApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class RecipientControl : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DataGridView _gridEmails = new();
        private readonly DataGridView _gridGroups = new();
        private readonly TextBox _txtEmail = new();
        private readonly ComboBox _cmbGroup = new();
        private readonly TextBox _txtGroup = new();
        private readonly Label _lblStatus = new();
        private List<EmailGroup> _groups = new();
        private List<Email> _emails = new();

        public RecipientControl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(24);

            var title = new Label
            {
                Text = "Recipients",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(24, 24),
                Size = new Size(300, 32)
            };

            var emailPanel = new Panel
            {
                Location = new Point(24, 90),
                Size = new Size(820, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _txtEmail.Location = new Point(0, 28);
            _txtEmail.Size = new Size(300, 31);
            _txtEmail.PlaceholderText = "Email address";

            _cmbGroup.Location = new Point(315, 28);
            _cmbGroup.Size = new Size(220, 31);
            _cmbGroup.DropDownStyle = ComboBoxStyle.DropDownList;

            var addEmailButton = new Button
            {
                Text = "Add Email",
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(550, 28),
                Size = new Size(110, 31)
            };
            addEmailButton.Click += async (_, _) => await AddEmailAsync();

            emailPanel.Controls.AddRange([CreateLabel("Add Email", 0, 0), _txtEmail, _cmbGroup, addEmailButton]);

            var groupPanel = new Panel
            {
                Location = new Point(24, 175),
                Size = new Size(820, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _txtGroup.Location = new Point(0, 28);
            _txtGroup.Size = new Size(300, 31);
            _txtGroup.PlaceholderText = "Group name";

            var addGroupButton = new Button
            {
                Text = "Add Group",
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(315, 28),
                Size = new Size(110, 31)
            };
            addGroupButton.Click += async (_, _) => await AddGroupAsync();

            groupPanel.Controls.AddRange([CreateLabel("Add Group", 0, 0), _txtGroup, addGroupButton]);

            ConfigureGrid(_gridEmails);
            _gridEmails.Location = new Point(24, 270);
            _gridEmails.Size = new Size(640, 330);
            _gridEmails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _gridEmails.Columns.Add("Address", "Email");
            _gridEmails.Columns.Add("Group", "Group");
            _gridEmails.Columns.Add(CreateButtonColumn("DeleteEmail", "Delete"));
            _gridEmails.CellContentClick += async (_, e) => await DeleteEmailFromGridAsync(e);

            ConfigureGrid(_gridGroups);
            _gridGroups.Location = new Point(690, 270);
            _gridGroups.Size = new Size(300, 330);
            _gridGroups.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _gridGroups.Columns.Add("Name", "Group");
            _gridGroups.Columns.Add(CreateButtonColumn("DeleteGroup", "Delete"));
            _gridGroups.CellContentClick += async (_, e) => await DeleteGroupFromGridAsync(e);

            _lblStatus.Location = new Point(24, 615);
            _lblStatus.Size = new Size(680, 32);
            _lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = new Font("Segoe UI", 9);

            Controls.AddRange([title, emailPanel, groupPanel, _gridEmails, _gridGroups, _lblStatus]);
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        private static DataGridViewButtonColumn CreateButtonColumn(string name, string text)
        {
            return new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = string.Empty,
                Text = text,
                UseColumnTextForButtonValue = true,
                Width = 80
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IEmailRecipientService>();
                _emails = (await service.GetEmailsAsync()).ToList();
                _groups = (await service.GetGroupsAsync()).ToList();
                BindData();
                SetStatus("Recipients loaded", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void BindData()
        {
            _cmbGroup.Items.Clear();
            _cmbGroup.Items.Add(new GroupOption(null, "-- No Group --"));
            foreach (var group in _groups)
                _cmbGroup.Items.Add(new GroupOption(group.Id, group.Name));
            _cmbGroup.SelectedIndex = 0;

            _gridEmails.Rows.Clear();
            foreach (var email in _emails)
            {
                var groupName = _groups.FirstOrDefault(group => group.Id == email.EmailGroupId)?.Name ?? "-";
                var index = _gridEmails.Rows.Add(email.Address, groupName);
                _gridEmails.Rows[index].Tag = email;
            }

            _gridGroups.Rows.Clear();
            foreach (var group in _groups)
            {
                var index = _gridGroups.Rows.Add(group.Name);
                _gridGroups.Rows[index].Tag = group;
            }
        }

        private async Task AddEmailAsync()
        {
            var selectedGroup = _cmbGroup.SelectedItem as GroupOption;
            var email = new Email
            {
                Address = _txtEmail.Text,
                EmailGroupId = selectedGroup?.Id
            };

            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEmailRecipientService>();
            var result = await service.AddEmailAsync(email);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to add email", false);
                return;
            }

            _txtEmail.Clear();
            await LoadDataAsync();
        }

        private async Task AddGroupAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEmailRecipientService>();
            var result = await service.AddGroupAsync(new EmailGroup { Name = _txtGroup.Text });

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to add group", false);
                return;
            }

            _txtGroup.Clear();
            await LoadDataAsync();
        }

        private async Task DeleteEmailFromGridAsync(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _gridEmails.Columns[e.ColumnIndex].Name != "DeleteEmail")
                return;

            if (_gridEmails.Rows[e.RowIndex].Tag is not Email email)
                return;

            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEmailRecipientService>();
            await service.DeleteEmailAsync(email.Id);
            await LoadDataAsync();
        }

        private async Task DeleteGroupFromGridAsync(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _gridGroups.Columns[e.ColumnIndex].Name != "DeleteGroup")
                return;

            if (_gridGroups.Rows[e.RowIndex].Tag is not EmailGroup group)
                return;

            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEmailRecipientService>();
            await service.DeleteGroupAsync(group.Id);
            await LoadDataAsync();
        }

        private static Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(180, 22),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
        }

        private void SetStatus(string message, bool success)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = success ? Color.FromArgb(21, 128, 61) : Color.FromArgb(229, 57, 53);
        }

        private sealed record GroupOption(Guid? Id, string Name)
        {
            public override string ToString() => Name;
        }
    }
}
