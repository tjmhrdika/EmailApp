using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace EmailApp.Controls
{
    public class RecipientControl : UserControl
    {
        private readonly TextBox txtConnectionString;
        private readonly TextBox txtEmail;
        private readonly TextBox txtGroup;
        private readonly ComboBox cmbGroup;
        private readonly DataGridView gridEmails;
        private readonly DataGridView gridGroups;
        private readonly Label lblStatus;

        public RecipientControl()
        {
            txtConnectionString = new TextBox();
            txtEmail = new TextBox();
            txtGroup = new TextBox();
            cmbGroup = new ComboBox();
            gridEmails = new DataGridView();
            gridGroups = new DataGridView();
            lblStatus = new Label();
            ConnectionString = "Server=localhost;Database=EmailDB;Trusted_Connection=True;TrustServerCertificate=True";
            InitializeComponent();
        }

        public string ConnectionString
        {
            get { return txtConnectionString.Text; }
            set { txtConnectionString.Text = value ?? string.Empty; }
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(16);

            var title = new Label
            {
                Text = "Recipients",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 27, 42),
                Location = new Point(16, 14),
                Size = new Size(220, 30)
            };

            var connectionLabel = CreateLabel("Connection String", 16, 56, 140);
            txtConnectionString.Location = new Point(160, 53);
            txtConnectionString.Size = new Size(620, 24);
            txtConnectionString.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var loadButton = CreateButton("Load", 790, 52, 80);
            loadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            loadButton.Click += delegate { LoadData(); };

            var emailLabel = CreateLabel("Email", 16, 100, 80);
            txtEmail.Location = new Point(90, 96);
            txtEmail.Size = new Size(250, 24);

            cmbGroup.Location = new Point(350, 96);
            cmbGroup.Size = new Size(210, 24);
            cmbGroup.DropDownStyle = ComboBoxStyle.DropDownList;

            var addEmailButton = CreateButton("Add Email", 570, 94, 100);
            addEmailButton.Click += delegate { AddEmail(); };

            var groupLabel = CreateLabel("Group", 16, 138, 80);
            txtGroup.Location = new Point(90, 134);
            txtGroup.Size = new Size(250, 24);

            var addGroupButton = CreateButton("Add Group", 350, 132, 100);
            addGroupButton.Click += delegate { AddGroup(); };

            ConfigureGrid(gridEmails);
            gridEmails.Location = new Point(16, 185);
            gridEmails.Size = new Size(610, 320);
            gridEmails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            ConfigureGrid(gridGroups);
            gridGroups.Location = new Point(640, 185);
            gridGroups.Size = new Size(230, 320);
            gridGroups.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;

            var deleteEmailButton = CreateButton("Delete Email", 16, 518, 110);
            deleteEmailButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            deleteEmailButton.Click += delegate { DeleteSelectedEmail(); };

            var deleteGroupButton = CreateButton("Delete Group", 640, 518, 110);
            deleteGroupButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            deleteGroupButton.Click += delegate { DeleteSelectedGroup(); };

            lblStatus.Location = new Point(140, 520);
            lblStatus.Size = new Size(490, 26);
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Font = new Font("Segoe UI", 9F);

            Controls.AddRange(new Control[]
            {
                title, connectionLabel, txtConnectionString, loadButton,
                emailLabel, txtEmail, cmbGroup, addEmailButton,
                groupLabel, txtGroup, addGroupButton,
                gridEmails, gridGroups, deleteEmailButton, deleteGroupButton, lblStatus
            });

            BindGroupOptions(new DataTable());
        }

        private void LoadData()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    var groups = Query(connection, "SELECT Id, Name FROM EmailGroups ORDER BY Name");
                    var emails = Query(connection,
                        "SELECT e.Id, e.Address, ISNULL(g.Name, '-') AS GroupName " +
                        "FROM Emails e LEFT JOIN EmailGroups g ON g.Id = e.EmailGroupId ORDER BY e.Address");

                    gridGroups.DataSource = groups;
                    gridEmails.DataSource = emails;
                    BindGroupOptions(groups);
                }

                SetStatus("Recipients loaded", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void AddEmail()
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                SetStatus("Email is required", false);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand("INSERT INTO Emails (Id, Address, EmailGroupId) VALUES (@Id, @Address, @GroupId)", connection))
                {
                    command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    command.Parameters.AddWithValue("@Address", txtEmail.Text.Trim());
                    command.Parameters.AddWithValue("@GroupId", cmbGroup.SelectedValue ?? (object)DBNull.Value);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                txtEmail.Clear();
                LoadData();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void AddGroup()
        {
            if (string.IsNullOrWhiteSpace(txtGroup.Text))
            {
                SetStatus("Group is required", false);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand("INSERT INTO EmailGroups (Id, Name) VALUES (@Id, @Name)", connection))
                {
                    command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    command.Parameters.AddWithValue("@Name", txtGroup.Text.Trim());
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                txtGroup.Clear();
                LoadData();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void DeleteSelectedEmail()
        {
            DeleteSelectedRow(gridEmails, "Emails");
        }

        private void DeleteSelectedGroup()
        {
            DeleteSelectedRow(gridGroups, "EmailGroups");
        }

        private void DeleteSelectedRow(DataGridView grid, string tableName)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Cells["Id"].Value == null)
            {
                SetStatus("Select a row first", false);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand("DELETE FROM " + tableName + " WHERE Id=@Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", grid.CurrentRow.Cells["Id"].Value);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                LoadData();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private static DataTable Query(SqlConnection connection, string sql)
        {
            using (var adapter = new SqlDataAdapter(sql, connection))
            {
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        private void BindGroupOptions(DataTable groups)
        {
            var options = groups.Copy();
            if (!options.Columns.Contains("Id"))
                options.Columns.Add("Id", typeof(Guid));
            if (!options.Columns.Contains("Name"))
                options.Columns.Add("Name", typeof(string));

            var row = options.NewRow();
            row["Id"] = DBNull.Value;
            row["Name"] = "-- No Group --";
            options.Rows.InsertAt(row, 0);

            cmbGroup.DataSource = options;
            cmbGroup.ValueMember = "Id";
            cmbGroup.DisplayMember = "Name";
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

        private static Button CreateButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 28),
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
