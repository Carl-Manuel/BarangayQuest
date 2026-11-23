using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form7 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        public Form7()
        {
            InitializeComponent();
            this.Load += Form7_Load;
            WireSecurityEvents();
        }

        private void WireSecurityEvents()
        {
            txtCurrentPassword.UseSystemPasswordChar = true;
            txtNewPassword.UseSystemPasswordChar = true;
            txtVerifyPassword.UseSystemPasswordChar = true;

            if (this.Controls.Find("picAvatar", true).FirstOrDefault() is PictureBox avatarBox)
            {
                avatarBox.SizeMode = PictureBoxSizeMode.Zoom;
                ApplyCircularMask(avatarBox);
                avatarBox.Resize += (s, e) => ApplyCircularMask(avatarBox);
            }

            if (this.Controls.Find("btnBack", true).FirstOrDefault() is Control backCtrl)
            {
                backCtrl.Cursor = Cursors.Hand;
                backCtrl.Click += (s, e) => ReturnToProfile();
            }

            if (this.Controls.Find("btnSavePassword", true).FirstOrDefault() is Control saveCtrl)
            {
                saveCtrl.Cursor = Cursors.Hand;
                saveCtrl.Click += async (s, e) => await ChangePasswordAsync();
            }

            // Optional left-side scroll navigation icons
            WireOptionalNav("picNavProfile", ReturnToProfile);
            WireOptionalNav("picNavMap", () =>
            {
                var map = new Form23();
                map.FormClosed += (s, a) => this.Close();
                map.Show();
                this.Hide();
            });
            WireOptionalNav("picNavQuests", () =>
            {
                var quests = new Form8();
                quests.FormClosed += (s, a) => this.Close();
                quests.Show();
                this.Hide();
            });
            WireOptionalNav("picNavShop", () =>
            {
                var dashboard = new Form5();
                dashboard.FormClosed += (s, a) => this.Close();
                dashboard.Show();
                this.Hide();
            });
        }

        private void WireOptionalNav(string controlName, Action navigateAction)
        {
            var ctrl = this.Controls.Find(controlName, true).FirstOrDefault();
            if (ctrl != null)
            {
                ctrl.Cursor = Cursors.Hand;
                ctrl.Click += (s, e) => navigateAction();
            }
        }

        private void ReturnToProfile()
        {
            var profileForm = new Form6();
            profileForm.FormClosed += (s, args) => this.Close();
            profileForm.Show();
            this.Hide();
        }

        private static void ApplyCircularMask(PictureBox pic)
        {
            if (pic.Width <= 0 || pic.Height <= 0) return;
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, pic.Width - 1, pic.Height - 1);
            var oldRegion = pic.Region;
            pic.Region = new Region(path);
            oldRegion?.Dispose();
        }

        private async void Form7_Load(object? sender, EventArgs e)
        {
            lblFullName.Text = "None";
            lblUserId.Text = UserSession.WorkerID > 0 ? $"WORKER ID: #{UserSession.WorkerID}" : "USER ID: None";
            lblWorkerRank.Text = "NEWBIE WORKER";
            lblTasksFinished.Text = "TASKS FINISHED: 0";

            await LoadProfileHeaderAsync();
        }

        private async Task LoadProfileHeaderAsync()
        {
            if (UserSession.UserID <= 0) return;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT u.Username, u.BarangayIDNumber, u.UserImagePath, cw.WorkerID
                    FROM Users u
                    INNER JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.UserID = @uid
                    LIMIT 1;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", UserSession.UserID);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string username = reader["Username"] != DBNull.Value ? reader["Username"].ToString() ?? "" : "";
                        string brgyId = reader["BarangayIDNumber"] != DBNull.Value ? reader["BarangayIDNumber"].ToString() ?? "" : "";
                        string imgPath = reader["UserImagePath"] != DBNull.Value ? reader["UserImagePath"].ToString() ?? "" : "";
                        int workerId = Convert.ToInt32(reader["WorkerID"]);

                        lblFullName.Text = string.IsNullOrWhiteSpace(username) ? "None" : username;
                        lblUserId.Text = string.IsNullOrWhiteSpace(brgyId)
                            ? $"WORKER ID: #{workerId}"
                            : $"ID: {brgyId}  |  WORKER #{workerId}";

                        LoadAvatarFromDisk(imgPath);
                    }
                }

                string countSql = @"
                    SELECT COUNT(*)
                    FROM Quest_Memberships
                    WHERE WorkerID = @workerId
                      AND Status = 'Completed';";

                using (var countCmd = new MySqlCommand(countSql, conn))
                {
                    countCmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);
                    object? res = await countCmd.ExecuteScalarAsync();
                    int completed = res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;

                    lblTasksFinished.Text = $"TASKS FINISHED: {completed}";
                    lblWorkerRank.Text = completed >= 6 ? "EXPERIENCED WORKER" : "NEWBIE WORKER";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load account header: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAvatarFromDisk(string relativeOrFullPath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrFullPath)) return;
            if (this.Controls.Find("picAvatar", true).FirstOrDefault() is not PictureBox avatarBox) return;

            string fullPath = Path.IsPathRooted(relativeOrFullPath)
                ? relativeOrFullPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeOrFullPath);

            if (!File.Exists(fullPath)) return;

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var oldImg = avatarBox.Image;
            avatarBox.Image = Image.FromStream(stream);
            oldImg?.Dispose();
            ApplyCircularMask(avatarBox);
        }

        private async Task ChangePasswordAsync()
        {
            if (UserSession.UserID <= 0)
            {
                MessageBox.Show("No active user session found. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentPass = txtCurrentPassword.Text.Trim();
            string newPass = txtNewPassword.Text.Trim();
            string verifyPass = txtVerifyPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(currentPass) ||
                string.IsNullOrWhiteSpace(newPass) ||
                string.IsNullOrWhiteSpace(verifyPass))
            {
                MessageBox.Show("Please fill in Current Password, New Password, and Verify New Password.", "Missing Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass.Length < 4)
            {
                MessageBox.Show("New password must be at least 4 characters long.", "Weak Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewPassword.Focus();
                return;
            }

            if (newPass != verifyPass)
            {
                MessageBox.Show("New Password and Verify New Password do not match.", "Password Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtVerifyPassword.Clear();
                txtVerifyPassword.Focus();
                return;
            }

            if (newPass == currentPass)
            {
                MessageBox.Show("Your new password must be different from your current password.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewPassword.Clear();
                txtVerifyPassword.Clear();
                txtNewPassword.Focus();
                return;
            }

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string targetColumn = "Password";
                string dbPass = "";

                using (var checkCmd = new MySqlCommand("SELECT * FROM Users WHERE UserID = @uid LIMIT 1;", conn))
                {
                    checkCmd.Parameters.AddWithValue("@uid", UserSession.UserID);
                    using var reader = await checkCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        MessageBox.Show("User account not found in database.", "Account Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (HasColumn(reader, "Password") && reader["Password"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["Password"].ToString()))
                    {
                        targetColumn = "Password";
                        dbPass = reader["Password"].ToString()!.Trim();
                    }
                    else if (HasColumn(reader, "PasswordHash") && reader["PasswordHash"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["PasswordHash"].ToString()))
                    {
                        targetColumn = "PasswordHash";
                        dbPass = reader["PasswordHash"].ToString()!.Trim();
                    }
                }

                // Auto-detect stored password format: SHA-256 Hex, SHA-256 Base64, MD5 Hex, or Plain Text
                bool isSha256Hex = dbPass.Length == 64 && currentPass.Length != 64 && dbPass.All(Uri.IsHexDigit);
                bool isSha256Base64 = dbPass.Length == 44 && dbPass.EndsWith("=") && currentPass.Length != 44;
                bool isMd5Hex = dbPass.Length == 32 && currentPass.Length != 32 && dbPass.All(Uri.IsHexDigit);

                bool isMatch;
                string passwordToStore;

                if (isSha256Hex)
                {
                    isMatch = string.Equals(dbPass, ComputeSha256Hex(currentPass), StringComparison.OrdinalIgnoreCase);
                    passwordToStore = ComputeSha256Hex(newPass);
                }
                else if (isSha256Base64)
                {
                    isMatch = string.Equals(dbPass, ComputeSha256Base64(currentPass), StringComparison.Ordinal);
                    passwordToStore = ComputeSha256Base64(newPass);
                }
                else if (isMd5Hex)
                {
                    isMatch = string.Equals(dbPass, ComputeMd5Hex(currentPass), StringComparison.OrdinalIgnoreCase);
                    passwordToStore = ComputeMd5Hex(newPass);
                }
                else
                {
                    isMatch = string.Equals(dbPass, currentPass, StringComparison.Ordinal);
                    passwordToStore = newPass;
                }

                if (!isMatch)
                {
                    MessageBox.Show("The current password you entered is incorrect.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtCurrentPassword.Clear();
                    txtCurrentPassword.Focus();
                    return;
                }

                string updateSql = $"UPDATE Users SET `{targetColumn}` = @newPass WHERE UserID = @uid;";
                using (var updateCmd = new MySqlCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@newPass", passwordToStore);
                    updateCmd.Parameters.AddWithValue("@uid", UserSession.UserID);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtVerifyPassword.Clear();

                MessageBox.Show("Your password has been updated successfully!", "Account Security Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not update password: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ComputeSha256Hex(string raw)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string ComputeSha256Base64(string raw)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }

        private static string ComputeMd5Hex(string raw)
        {
            byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // Puta di ko matanggal
        private void pictureBox7_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void citizenusernametxt_TextChanged(object sender, EventArgs e) { }
    }
}