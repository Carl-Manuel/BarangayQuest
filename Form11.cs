using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form11 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        private Label? lblHeroName;
        private Label? lblAdminId;
        private Label? lblAdminNameVal;
        private Label? lblPhoneVal;

        private Button? btnManageSecurity;
        private Button? btnMakeAnnouncement;
        private Button? btnLogout;

        private int currentAdminUserId;
        private string currentBarangayId = "";
        private string currentUsername = "";
        private string currentFullName = "";
        private string currentPhone = "";

        public Form11()
        {
            InitializeComponent();
            BindAdminProfileControls();
            this.Load += async (s, e) => await LoadAdminProfileAsync();
        }

        private void BindAdminProfileControls()
        {
            var allLabels = GetAllControls(this).OfType<Label>().ToList();

            lblHeroName = allLabels
                .FirstOrDefault(l => l.Text.Trim().Equals("NAME", StringComparison.OrdinalIgnoreCase));

            lblAdminId = allLabels
                .FirstOrDefault(l => l.Text.Trim().Equals("ADMIN ID", StringComparison.OrdinalIgnoreCase));

            lblAdminNameVal = allLabels
                .FirstOrDefault(l => l.Text.Trim().Equals("ADMIN", StringComparison.OrdinalIgnoreCase));

            // Pick the lower of the two "PHONE NUMBER" labels so the header stays intact
            lblPhoneVal = allLabels
                .Where(l => l.Text.Trim().Equals("PHONE NUMBER", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.PointToScreen(Point.Empty).Y)
                .FirstOrDefault();

            var buttons = GetAllControls(this).OfType<Button>().ToList();
            btnManageSecurity = FindButton(buttons, "MANAGE ACCOUNT SECURITY");
            btnMakeAnnouncement = FindButton(buttons, "MAKE AN ANNOUNCEMENT");
            btnLogout = FindButton(buttons, "LOG OUT");

            if (btnManageSecurity != null)
            {
                btnManageSecurity.Cursor = Cursors.Hand;
                btnManageSecurity.Click += (s, e) => NavigateTo(new Form12());
            }

            if (btnMakeAnnouncement != null)
            {
                btnMakeAnnouncement.Cursor = Cursors.Hand;
                btnMakeAnnouncement.Click += (s, e) => NavigateTo(new Form13());
            }

            if (btnLogout != null)
            {
                btnLogout.Cursor = Cursors.Hand;
                btnLogout.Click += (s, e) => PerformLogout();
            }

            // Wire the left sidebar icons (1st = Dashboard, 2nd = Inventory, 3rd = User Management)
            var sidebarIcons = GetAllControls(this)
                .OfType<PictureBox>()
                .Where(p => p.Left < 110 && p.Top > 120 && p.Width < 90)
                .OrderBy(p => p.Top)
                .ToList();

            if (sidebarIcons.Count >= 1)
            {
                sidebarIcons[0].Cursor = Cursors.Hand;
                sidebarIcons[0].Click += (s, e) => NavigateTo(new X());
            }

            if (sidebarIcons.Count >= 2)
            {
                sidebarIcons[1].Cursor = Cursors.Hand;
                sidebarIcons[1].Click += (s, e) => NavigateTo(new Form15());
            }

            if (sidebarIcons.Count >= 3)
            {
                sidebarIcons[2].Cursor = Cursors.Hand;
                sidebarIcons[2].Click += (s, e) => NavigateTo(new Form14());
            }
        }

        private async Task LoadAdminProfileAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sessionName = UserSession.FullName?.Trim() ?? "";

                string sql = !string.IsNullOrEmpty(sessionName)
                    ? @"SELECT u.UserID, u.BarangayIDNumber, u.Username, u.FullName, u.Phone_Number
                        FROM Users u
                        WHERE u.Role = 'SuperAdmin' AND u.FullName = @fullName
                        LIMIT 1;"
                    : @"SELECT u.UserID, u.BarangayIDNumber, u.Username, u.FullName, u.Phone_Number
                        FROM Users u
                        WHERE u.Role = 'SuperAdmin'
                        ORDER BY u.UserID ASC
                        LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(sessionName))
                    cmd.Parameters.AddWithValue("@fullName", sessionName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;

                currentAdminUserId = Convert.ToInt32(reader["UserID"]);
                currentBarangayId = reader["BarangayIDNumber"]?.ToString() ?? "ADMIN-001";
                currentUsername = reader["Username"]?.ToString() ?? "superadmin";
                currentFullName = reader["FullName"]?.ToString() ?? "Super Admin";
                currentPhone = reader["Phone_Number"]?.ToString() ?? "N/A";

                RefreshProfileLabels();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Super Admin profile: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshProfileLabels()
        {
            if (lblHeroName != null)
                lblHeroName.Text = currentFullName.ToUpperInvariant();

            if (lblAdminId != null)
                lblAdminId.Text = $"ADMIN ID: {currentBarangayId}  (@{currentUsername})";

            if (lblAdminNameVal != null)
                lblAdminNameVal.Text = currentFullName;

            if (lblPhoneVal != null)
                lblPhoneVal.Text = string.IsNullOrWhiteSpace(currentPhone) ? "Not Set" : currentPhone;
        }

        private async Task OpenManageSecurityDialogAsync()
        {
            if (currentAdminUserId == 0)
            {
                MessageBox.Show("No active Super Admin profile was loaded.", "Profile Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new Form
            {
                Text = "Manage Super Admin Account & Security",
                Size = new Size(430, 340),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 25, 12),
                ForeColor = Color.White
            };

            int y = 22;
            TextBox AddField(string caption, string value, bool isPassword = false)
            {
                var lbl = new Label { Text = caption, Left = 20, Top = y, Width = 155, ForeColor = Color.Gold };
                var txt = new TextBox { Left = 180, Top = y - 3, Width = 215, Text = value, UseSystemPasswordChar = isPassword };
                dlg.Controls.Add(lbl);
                dlg.Controls.Add(txt);
                y += 38;
                return txt;
            }

            var txtFullName = AddField("Admin Full Name:", currentFullName);
            var txtUsername = AddField("Admin Username:", currentUsername);
            var txtPhone = AddField("Phone Number:", currentPhone);
            var txtNewPassword = AddField("New Password:", "", true);
            var txtConfirmPassword = AddField("Confirm Password:", "", true);

            var lblHint = new Label
            {
                Text = "Leave password fields blank to keep your current password.",
                Left = 20,
                Top = y - 6,
                Width = 375,
                ForeColor = Color.LightGray,
                Font = new Font(this.Font.FontFamily, 8f, FontStyle.Italic)
            };
            dlg.Controls.Add(lblHint);
            y += 26;

            var btnSave = new Button
            {
                Text = "SAVE SECURITY",
                Left = 180,
                Top = y,
                Width = 130,
                Height = 36,
                BackColor = Color.ForestGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnCancel = new Button
            {
                Text = "CANCEL",
                Left = 320,
                Top = y,
                Width = 75,
                Height = 36,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnCancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;
            btnSave.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Admin Full Name and Username cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool changePassword = !string.IsNullOrWhiteSpace(txtNewPassword.Text) || !string.IsNullOrWhiteSpace(txtConfirmPassword.Text);
                if (changePassword && txtNewPassword.Text != txtConfirmPassword.Text)
                {
                    MessageBox.Show("New Password and Confirm Password do not match.", "Password Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var conn = new MySqlConnection(connString);
                    await conn.OpenAsync();

                    string sql = changePassword
                        ? @"UPDATE Users
                            SET FullName = @name, Username = @user, Phone_Number = @phone, Password = @pass
                            WHERE UserID = @uid;"
                        : @"UPDATE Users
                            SET FullName = @name, Username = @user, Phone_Number = @phone
                            WHERE UserID = @uid;";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                    cmd.Parameters.AddWithValue("@user", txtUsername.Text.Trim());
                    cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                    if (changePassword)
                        cmd.Parameters.AddWithValue("@pass", HashPassword(txtNewPassword.Text));
                    cmd.Parameters.AddWithValue("@uid", currentAdminUserId);

                    await cmd.ExecuteNonQueryAsync();

                    currentFullName = txtFullName.Text.Trim();
                    currentUsername = txtUsername.Text.Trim();
                    currentPhone = txtPhone.Text.Trim();
                    UserSession.FullName = currentFullName;

                    RefreshProfileLabels();
                    MessageBox.Show("Super Admin credentials and security details updated.", "Profile Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update profile: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { btnSave, btnCancel });
            dlg.ShowDialog(this);
        }

        private async Task OpenAnnouncementManagerDialogAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string createSql = @"
                    CREATE TABLE IF NOT EXISTS Announcements (
                        AnnouncementID INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(120) NOT NULL,
                        Message TEXT NOT NULL,
                        Priority ENUM('Normal', 'Urgent') NOT NULL DEFAULT 'Normal',
                        PostedBy VARCHAR(100) NOT NULL,
                        IsActive TINYINT(1) NOT NULL DEFAULT 1,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var cmd = new MySqlCommand(createSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not initialize announcements table: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var dlg = new Form
            {
                Text = "Barangay System Announcements",
                Size = new Size(560, 470),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 25, 12),
                ForeColor = Color.White
            };

            var lblTitle = new Label { Text = "Headline:", Left = 20, Top = 18, Width = 90, ForeColor = Color.Gold };
            var txtTitle = new TextBox { Left = 115, Top = 15, Width = 270 };

            var lblPriority = new Label { Text = "Priority:", Left = 395, Top = 18, Width = 55, ForeColor = Color.Gold };
            var cmbPriority = new ComboBox { Left = 450, Top = 15, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPriority.Items.AddRange(new object[] { "Normal", "Urgent" });
            cmbPriority.SelectedIndex = 0;

            var lblMsg = new Label { Text = "Message:", Left = 20, Top = 52, Width = 90, ForeColor = Color.Gold };
            var txtMsg = new TextBox { Left = 115, Top = 49, Width = 415, Height = 65, Multiline = true };

            var btnPost = new Button
            {
                Text = "PUBLISH ANNOUNCEMENT",
                Left = 115,
                Top = 124,
                Width = 200,
                Height = 32,
                BackColor = Color.ForestGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnArchive = new Button
            {
                Text = "REMOVE SELECTED",
                Left = 325,
                Top = 124,
                Width = 205,
                Height = 32,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var dgvAnnouncements = new DataGridView
            {
                Left = 20,
                Top = 170,
                Width = 510,
                Height = 240,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(30, 18, 10)
            };
            dgvAnnouncements.Columns.Add("colTitle", "HEADLINE");
            dgvAnnouncements.Columns.Add("colPriority", "PRIORITY");
            dgvAnnouncements.Columns.Add("colMsg", "MESSAGE");
            dgvAnnouncements.Columns.Add("colDate", "POSTED");

            async Task ReloadAnnouncementsAsync()
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                string sql = @"
                    SELECT AnnouncementID, Title, Priority, Message, CreatedAt
                    FROM Announcements
                    WHERE IsActive = 1
                    ORDER BY CreatedAt DESC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                dgvAnnouncements.Rows.Clear();

                while (await reader.ReadAsync())
                {
                    int idx = dgvAnnouncements.Rows.Add(
                        reader["Title"]?.ToString() ?? "",
                        reader["Priority"]?.ToString() ?? "",
                        reader["Message"]?.ToString() ?? "",
                        Convert.ToDateTime(reader["CreatedAt"]).ToString("MMM dd, yyyy hh:mm tt")
                    );
                    dgvAnnouncements.Rows[idx].Tag = Convert.ToInt32(reader["AnnouncementID"]);
                }
            }

            btnPost.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtMsg.Text))
                {
                    MessageBox.Show("Enter both a Headline and Message before publishing.", "Missing Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var conn = new MySqlConnection(connString);
                    await conn.OpenAsync();

                    string insSql = @"
                        INSERT INTO Announcements (Title, Message, Priority, PostedBy, IsActive)
                        VALUES (@title, @msg, @prio, @by, 1);";

                    using var cmd = new MySqlCommand(insSql, conn);
                    cmd.Parameters.AddWithValue("@title", txtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@msg", txtMsg.Text.Trim());
                    cmd.Parameters.AddWithValue("@prio", cmbPriority.SelectedItem?.ToString() ?? "Normal");
                    cmd.Parameters.AddWithValue("@by", string.IsNullOrWhiteSpace(currentFullName) ? "Super Admin" : currentFullName);
                    await cmd.ExecuteNonQueryAsync();

                    txtTitle.Clear();
                    txtMsg.Clear();
                    await ReloadAnnouncementsAsync();
                    MessageBox.Show("Announcement published to BarangayQuest!", "Published", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error publishing announcement: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnArchive.Click += async (s, e) =>
            {
                if (dgvAnnouncements.SelectedRows.Count == 0 || dgvAnnouncements.SelectedRows[0].Tag is not int annId)
                {
                    MessageBox.Show("Select an announcement row first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var conn = new MySqlConnection(connString);
                    await conn.OpenAsync();

                    using var cmd = new MySqlCommand("UPDATE Announcements SET IsActive = 0 WHERE AnnouncementID = @id;", conn);
                    cmd.Parameters.AddWithValue("@id", annId);
                    await cmd.ExecuteNonQueryAsync();

                    await ReloadAnnouncementsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing announcement: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblTitle, txtTitle, lblPriority, cmbPriority, lblMsg, txtMsg, btnPost, btnArchive, dgvAnnouncements });
            dlg.Load += async (s, e) => await ReloadAnnouncementsAsync();
            dlg.ShowDialog(this);
        }

        private void PerformLogout()
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to log out of the Super Admin portal?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            UserSession.FullName = string.Empty;

            var roleSelection = new Form2();
            roleSelection.FormClosed += (s, e) => this.Close();
            roleSelection.Show();
            this.Hide();
        }

        private void NavigateTo(Form targetForm)
        {
            targetForm.FormClosed += (s, e) => this.Close();
            targetForm.Show();
            this.Hide();
        }

        private static string HashPassword(string rawPassword)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword));
            return Convert.ToHexString(bytes);
        }

        private static Button? FindButton(IEnumerable<Button> buttons, string text)
        {
            return buttons.FirstOrDefault(b => b.Text.Trim().Equals(text, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<Control> GetAllControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (Control child in GetAllControls(c))
                    yield return child;
            }
        }

        private void makeannouncement_Click(object sender, EventArgs e)
        {
            new Form13().Show();
            this.Hide();
        }

        private void logout1_Click(object sender, EventArgs e)
        {
            new Form2().Show();
            this.Hide();
        }
    }
}