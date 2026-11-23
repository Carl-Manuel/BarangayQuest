using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form6 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        public Form6()
        {
            InitializeComponent();
            this.Load += Form6_Load;
            WireProfileEvents();
        }

        private void WireProfileEvents()
        {
            if (this.Controls.Find("picAvatar", true).FirstOrDefault() is PictureBox avatarBox)
            {
                avatarBox.Cursor = Cursors.Hand;
                avatarBox.SizeMode = PictureBoxSizeMode.Zoom;
                ApplyCircularMask(avatarBox);
                avatarBox.Resize += (s, e) => ApplyCircularMask(avatarBox);
                avatarBox.Click += async (s, e) => await UploadProfilePictureAsync(avatarBox);
            }

            if (this.Controls.Find("btnManageAccount", true).FirstOrDefault() is Button manageBtn)
            {
                manageBtn.Cursor = Cursors.Hand;
                manageBtn.Click += (s, e) =>
                {
                    var securityForm = new Form7();
                    securityForm.FormClosed += (snd, args) => this.Close();
                    securityForm.Show();
                    this.Hide();
                };
            }
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

        private async void Form6_Load(object? sender, EventArgs e)
        {
            ResetProfileDefaults();
            await LoadCitizenProfileAsync();
        }

        private void logout_Click(object sender, EventArgs e)
        {
            UserSession.Clear();
            Form1 Form1 = new Form1();
            Form1.FormClosed += (s, args) => this.Close();
            Form1.Show();
            this.Hide();
        }

        private void backbutton_Click(object sender, EventArgs e)
        {
            Form5 Form5 = new Form5();
            Form5.FormClosed += (s, args) => this.Close();
            Form5.Show();
            this.Hide();
        }

        private void ResetProfileDefaults()
        {
            lblFullName.Text = "None"; // Username
            lblUserId.Text = UserSession.WorkerID > 0 ? $"WORKER ID: #{UserSession.WorkerID}" : "USER ID: None";
            lblWorkerRank.Text = "NEWBIE WORKER";
            lblTasksFinished.Text = "TASKS FINISHED: 0";
            lblLguName.Text = string.IsNullOrWhiteSpace(UserSession.FullName) ? "None" : UserSession.FullName.ToUpper(); // Full Name
            lblRole.Text = "CITIZEN WORKER"; // Role
            lblTotalPoints.Text = UserSession.Points.ToString();
            lblPhoneNumber.Text = "None";
        }

        private async Task LoadCitizenProfileAsync()
        {
            if (UserSession.UserID <= 0) return;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string profileSql = @"
                    SELECT u.*, cw.WorkerID
                    FROM Users u
                    INNER JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.UserID = @userId
                    LIMIT 1;";

                using (var cmd = new MySqlCommand(profileSql, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", UserSession.UserID);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string username = HasColumn(reader, "Username") && reader["Username"] != DBNull.Value
                            ? reader["Username"].ToString() ?? ""
                            : "";
                        string fullName = HasColumn(reader, "FullName") && reader["FullName"] != DBNull.Value
                            ? reader["FullName"].ToString() ?? ""
                            : "";
                        string role = HasColumn(reader, "Role") && reader["Role"] != DBNull.Value
                            ? reader["Role"].ToString() ?? ""
                            : "Citizen Worker";
                        string brgyId = HasColumn(reader, "BarangayIDNumber") && reader["BarangayIDNumber"] != DBNull.Value
                            ? reader["BarangayIDNumber"].ToString() ?? ""
                            : "";
                        string phone = HasColumn(reader, "Phone_Number") && reader["Phone_Number"] != DBNull.Value
                            ? reader["Phone_Number"].ToString() ?? ""
                            : "";
                        string imgPath = HasColumn(reader, "UserImagePath") && reader["UserImagePath"] != DBNull.Value
                            ? reader["UserImagePath"].ToString() ?? ""
                            : "";
                        int points = HasColumn(reader, "Points") && reader["Points"] != DBNull.Value
                            ? Convert.ToInt32(reader["Points"])
                            : 0;
                        int workerId = Convert.ToInt32(reader["WorkerID"]);

                        UserSession.FullName = fullName;
                        UserSession.WorkerID = workerId;
                        UserSession.BarangayIDNumber = brgyId;
                        UserSession.Points = points;

                        lblFullName.Text = string.IsNullOrWhiteSpace(username) ? "None" : username;
                        lblUserId.Text = string.IsNullOrWhiteSpace(brgyId)
                            ? $"WORKER ID: #{workerId}"
                            : $"ID: {brgyId}  |  WORKER #{workerId}";
                        lblLguName.Text = string.IsNullOrWhiteSpace(fullName) ? "None" : fullName.ToUpper();
                        lblRole.Text = string.IsNullOrWhiteSpace(role) ? "CITIZEN WORKER" : role.ToUpper();
                        lblTotalPoints.Text = points.ToString();
                        lblPhoneNumber.Text = string.IsNullOrWhiteSpace(phone) ? "None" : phone;

                        LoadAvatarFromDisk(imgPath);
                    }
                }

                int completedCount = await GetCompletedQuestCountAsync(conn);
                lblTasksFinished.Text = $"TASKS FINISHED: {completedCount}";
                lblWorkerRank.Text = completedCount switch
                {
                    >= 6 => "EXPERIENCED WORKER",
                    _ => "NEWBIE WORKER"
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load profile: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private async Task UploadProfilePictureAsync(PictureBox avatarBox)
        {
            if (UserSession.UserID <= 0) return;

            using var ofd = new OpenFileDialog
            {
                Title = "Select Profile Picture",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                string avatarDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");
                Directory.CreateDirectory(avatarDir);

                string ext = Path.GetExtension(ofd.FileName);
                string fileName = $"user_{UserSession.UserID}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                string destFull = Path.Combine(avatarDir, fileName);
                string relativePath = Path.Combine("Avatars", fileName);

                File.Copy(ofd.FileName, destFull, overwrite: true);

                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                try
                {
                    using (var imgCmd = new MySqlCommand(
                        "INSERT INTO Image_Registry (FilePath, EntityType) VALUES (@path, 'Avatar');", conn, tx))
                    {
                        imgCmd.Parameters.AddWithValue("@path", relativePath);
                        await imgCmd.ExecuteNonQueryAsync();
                    }

                    using (var userCmd = new MySqlCommand(
                        "UPDATE Users SET UserImagePath = @path WHERE UserID = @uid;", conn, tx))
                    {
                        userCmd.Parameters.AddWithValue("@path", relativePath);
                        userCmd.Parameters.AddWithValue("@uid", UserSession.UserID);
                        await userCmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                LoadAvatarFromDisk(relativePath);
                MessageBox.Show("Profile picture updated!", "Profile Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not update profile picture: {ex.Message}", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<int> GetCompletedQuestCountAsync(MySqlConnection conn)
        {
            try
            {
                string countSql = @"
                    SELECT COUNT(*)
                    FROM Quest_Memberships
                    WHERE WorkerID = @workerId
                      AND Status = 'Completed';";

                using var cmd = new MySqlCommand(countSql, conn);
                cmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);

                object? result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch (MySqlException)
            {
                return 0;
            }
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
    }
}