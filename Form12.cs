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
    public partial class Form12 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        private Label? lblHeroName;
        private Label? lblAdminId;

        private TextBox? txtCurrentPassword;
        private TextBox? txtNewPassword;
        private TextBox? txtVerifyPassword;

        private Button? btnBack;
        private Button? btnUpdate;

        private int currentAdminUserId;
        private string storedPasswordHash = "";

        public Form12()
        {
            InitializeComponent();
            BindSecurityControls();
            this.Load += async (s, e) => await LoadAdminSecurityContextAsync();
        }

        private void BindSecurityControls()
        {
            var allLabels = GetAllControls(this).OfType<Label>().ToList();

            lblHeroName = allLabels
                .FirstOrDefault(l => l.Text.Trim().Equals("NAME", StringComparison.OrdinalIgnoreCase));

            lblAdminId = allLabels
                .FirstOrDefault(l => l.Text.Trim().Equals("ADMIN ID", StringComparison.OrdinalIgnoreCase));

            // Map existing TextBoxes if placed in the Designer, or inject masked inputs into the password panels
            var existingTextBoxes = GetAllControls(this).OfType<TextBox>().ToList();
            if (existingTextBoxes.Count >= 3)
            {
                var orderedByX = existingTextBoxes
                    .OrderBy(t => t.PointToScreen(Point.Empty).X)
                    .ToList();

                txtCurrentPassword = orderedByX[0];

                var rightBoxes = orderedByX
                    .Skip(1)
                    .OrderBy(t => t.PointToScreen(Point.Empty).Y)
                    .ToList();

                txtNewPassword = rightBoxes[0];
                txtVerifyPassword = rightBoxes[1];
            }
            else
            {
                txtCurrentPassword = ResolveOrCreatePasswordBox(allLabels, "CURRENT PASSWORD");
                txtNewPassword = ResolveOrCreatePasswordBox(allLabels, "NEW PASSWORD");
                txtVerifyPassword = ResolveOrCreatePasswordBox(allLabels, "VERIFY NEW PASSWORD");
            }

            if (txtCurrentPassword != null) txtCurrentPassword.UseSystemPasswordChar = true;
            if (txtNewPassword != null) txtNewPassword.UseSystemPasswordChar = true;
            if (txtVerifyPassword != null) txtVerifyPassword.UseSystemPasswordChar = true;

            var buttons = GetAllControls(this).OfType<Button>().ToList();
            btnBack = FindButton(buttons, "BACK");
            btnUpdate = FindButton(buttons, "UPDATE");

            if (btnBack != null)
            {
                btnBack.Cursor = Cursors.Hand;
                btnBack.Click += (s, e) => NavigateTo(new Form11());
            }

            if (btnUpdate != null)
            {
                btnUpdate.Cursor = Cursors.Hand;
                btnUpdate.Click += async (s, e) => await UpdateAdminPasswordAsync();
                this.AcceptButton = btnUpdate;
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

        private TextBox? ResolveOrCreatePasswordBox(List<Label> labels, string captionText)
        {
            var lbl = labels.FirstOrDefault(l => l.Text.Trim().Equals(captionText, StringComparison.OrdinalIgnoreCase));
            if (lbl == null || lbl.Parent == null) return null;

            // Check if a TextBox already exists inside the same container
            var existing = lbl.Parent.Controls.OfType<TextBox>().FirstOrDefault();
            if (existing != null)
            {
                existing.UseSystemPasswordChar = true;
                return existing;
            }

            // Otherwise create a borderless masked TextBox directly beneath the label inside its panel
            Control container = lbl.Parent;
            int inputTop = lbl.Bottom + 6;
            int inputWidth = Math.Max(160, container.ClientSize.Width - (lbl.Left * 2));

            var txt = new TextBox
            {
                Left = lbl.Left,
                Top = inputTop,
                Width = inputWidth,
                BackColor = container == this ? Color.FromArgb(68, 14, 14) : container.BackColor,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                UseSystemPasswordChar = true
            };

            container.Controls.Add(txt);
            txt.BringToFront();

            if (container != this)
            {
                container.Cursor = Cursors.IBeam;
                container.Click += (s, e) => txt.Focus();
            }

            return txt;
        }

        private async Task LoadAdminSecurityContextAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sessionName = UserSession.FullName?.Trim() ?? "";

                string sql = !string.IsNullOrEmpty(sessionName)
                    ? @"SELECT UserID, BarangayIDNumber, Username, FullName, Password
                        FROM Users
                        WHERE Role = 'SuperAdmin' AND FullName = @fullName
                        LIMIT 1;"
                    : @"SELECT UserID, BarangayIDNumber, Username, FullName, Password
                        FROM Users
                        WHERE Role = 'SuperAdmin'
                        ORDER BY UserID ASC
                        LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(sessionName))
                    cmd.Parameters.AddWithValue("@fullName", sessionName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;

                currentAdminUserId = Convert.ToInt32(reader["UserID"]);
                storedPasswordHash = reader["Password"]?.ToString() ?? "";

                string barangayId = reader["BarangayIDNumber"]?.ToString() ?? "ADMIN-001";
                string username = reader["Username"]?.ToString() ?? "superadmin";
                string fullName = reader["FullName"]?.ToString() ?? "Super Admin";

                if (lblHeroName != null)
                    lblHeroName.Text = fullName.ToUpperInvariant();

                if (lblAdminId != null)
                    lblAdminId.Text = $"ADMIN ID: {barangayId}  (@{username})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Super Admin credentials: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateAdminPasswordAsync()
        {
            if (currentAdminUserId == 0)
            {
                MessageBox.Show("No active Super Admin account found.", "Account Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentPass = txtCurrentPassword?.Text ?? "";
            string newPass = txtNewPassword?.Text ?? "";
            string verifyPass = txtVerifyPassword?.Text ?? "";

            if (string.IsNullOrWhiteSpace(currentPass) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(verifyPass))
            {
                MessageBox.Show("Please fill in Current Password, New Password, and Verify New Password.", "Missing Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass.Length < 4)
            {
                MessageBox.Show("New Password must be at least 4 characters long.", "Weak Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewPassword?.Focus();
                return;
            }

            if (!string.Equals(newPass, verifyPass, StringComparison.Ordinal))
            {
                MessageBox.Show("New Password and Verify New Password do not match.", "Password Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtVerifyPassword?.Clear();
                txtVerifyPassword?.Focus();
                return;
            }

            // Verify current password (supports SHA-256 hex hashes and plain-text test accounts)
            string currentHashed = HashPassword(currentPass);
            bool currentValid =
                string.Equals(storedPasswordHash, currentHashed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(storedPasswordHash, currentPass, StringComparison.Ordinal);

            if (!currentValid)
            {
                MessageBox.Show("The Current Password you entered is incorrect.", "Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                txtCurrentPassword?.Clear();
                txtCurrentPassword?.Focus();
                return;
            }

            if (btnUpdate != null) btnUpdate.Enabled = false;

            try
            {
                string newHashed = HashPassword(newPass);

                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sql = "UPDATE Users SET Password = @pass WHERE UserID = @uid;";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pass", newHashed);
                cmd.Parameters.AddWithValue("@uid", currentAdminUserId);
                await cmd.ExecuteNonQueryAsync();

                storedPasswordHash = newHashed;
                txtCurrentPassword?.Clear();
                txtNewPassword?.Clear();
                txtVerifyPassword?.Clear();

                MessageBox.Show("Super Admin password has been updated successfully.", "Security Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update password: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btnUpdate != null) btnUpdate.Enabled = true;
            }
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
    }
}