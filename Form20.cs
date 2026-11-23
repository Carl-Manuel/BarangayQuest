using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form20 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private Label? _lblDispatcherName;
        private Label? _lblDispatcherId;
        private Label? _lblBottomStatus;
        private PictureBox? _picAvatar;

        private TextBox? _txtCurrentPassword;
        private TextBox? _txtNewPassword;
        private TextBox? _txtVerifyPassword;
        private Control? _btnBack;
        private Control? _btnUpdatePassword;

        private bool _isSaving = false;

        public Form20()
        {
            InitializeComponent();
            BindProfileControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await LoadDispatcherProfileAsync();
        }

        private void BindProfileControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();
            var allPictureBoxes = allControls.OfType<PictureBox>().ToList();

            // Bind NAME and DISPATCHER ID labels in the upper dark-maroon header card
            _lblDispatcherName = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("NAME", StringComparison.OrdinalIgnoreCase));

            _lblDispatcherId = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("DISPATCHER ID", StringComparison.OrdinalIgnoreCase));

            // Bind Avatar PictureBox inside the header card (allow clicking to update avatar photo)
            _picAvatar = allPictureBoxes
                .Where(p => p.Left >= 130 && p.Top >= 110 && p.Top <= 280 && p.Width >= 60 && p.Height >= 60)
                .OrderByDescending(p => p.Width * p.Height)
                .FirstOrDefault();

            if (_picAvatar != null)
            {
                _picAvatar.Cursor = Cursors.Hand;
                _picAvatar.SizeMode = PictureBoxSizeMode.Zoom;
                _picAvatar.Click += async (s, e) => await ChangeAvatarPhotoAsync();
            }

            // Bind the 3 Password Inputs (CURRENT PASSWORD, NEW PASSWORD, VERIFY NEW PASSWORD)
            var lblCurrent = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("CURRENT PASSWORD", StringComparison.OrdinalIgnoreCase));
            var lblNew = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("NEW PASSWORD", StringComparison.OrdinalIgnoreCase));
            var lblVerify = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("VERIFY NEW PASSWORD", StringComparison.OrdinalIgnoreCase));

            _txtCurrentPassword = FindOrCreatePasswordTextBox(allControls, lblCurrent);
            _txtNewPassword = FindOrCreatePasswordTextBox(allControls, lblNew);
            _txtVerifyPassword = FindOrCreatePasswordTextBox(allControls, lblVerify);

            if (_txtCurrentPassword != null)
            {
                _txtCurrentPassword.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        _txtNewPassword?.Focus();
                    }
                };
            }

            if (_txtNewPassword != null)
            {
                _txtNewPassword.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        _txtVerifyPassword?.Focus();
                    }
                };
            }

            if (_txtVerifyPassword != null)
            {
                _txtVerifyPassword.KeyDown += async (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        await UpdateDispatcherPasswordAsync();
                    }
                };
            }

            // Bind BACK button and create/bind UPDATE PASSWORD button aligned on the right
            _btnBack = allControls.FirstOrDefault(c =>
                c.Name.Equals("back", StringComparison.OrdinalIgnoreCase) ||
                c.Text.Trim().Equals("BACK", StringComparison.OrdinalIgnoreCase));

            if (_btnBack != null)
            {
                _btnBack.Cursor = Cursors.Hand;
                _btnBack.Click += (s, e) => NavigateTo(new Form17());
            }

            _btnUpdatePassword = allControls.FirstOrDefault(c =>
                c != _btnBack &&
                (c.Name.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                 c.Name.Contains("save", StringComparison.OrdinalIgnoreCase) ||
                 c.Text.Trim().Equals("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                 c.Text.Trim().Equals("SAVE", StringComparison.OrdinalIgnoreCase)));

            if (_btnUpdatePassword == null && lblVerify != null)
            {
                Control hostParent = lblVerify.Parent ?? this;
                int btnWidth = Math.Max(165, _btnBack?.Width ?? 165);
                int btnHeight = _btnBack?.Height ?? 34;

                int btnTop = lblVerify.Bottom + 44;
                if (_btnBack != null && _btnBack.Parent == hostParent)
                {
                    btnTop = _btnBack.Top;
                }

                int btnLeft = lblVerify.Left;
                if (_txtVerifyPassword != null && _txtVerifyPassword.Parent == hostParent)
                {
                    btnLeft = _txtVerifyPassword.Right - btnWidth;
                    btnTop = Math.Max(btnTop, _txtVerifyPassword.Bottom + 12);
                }

                var createdSaveBtn = new Button
                {
                    Text = "UPDATE PASSWORD",
                    Left = btnLeft,
                    Top = btnTop,
                    Width = btnWidth,
                    Height = btnHeight,
                    BackColor = Color.FromArgb(46, 125, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                createdSaveBtn.FlatAppearance.BorderColor = Color.Goldenrod;
                createdSaveBtn.FlatAppearance.BorderSize = 1;

                hostParent.Controls.Add(createdSaveBtn);
                createdSaveBtn.BringToFront();
                _btnUpdatePassword = createdSaveBtn;
            }

            if (_btnUpdatePassword != null)
            {
                _btnUpdatePassword.Cursor = Cursors.Hand;
                _btnUpdatePassword.Click += async (s, e) => await UpdateDispatcherPasswordAsync();
            }

            // Wire the Left Scroll Navigation Icons (1st = Dashboard, 2nd = Profile, 3rd = Status of Users)
            var sidebarIcons = allPictureBoxes
                .Where(p => p != _picAvatar && p.Left < 130 && p.Top > 120 && p.Width < 90)
                .OrderBy(p => p.Top)
                .ToList();

            if (sidebarIcons.Count >= 1)
            {
                sidebarIcons[0].Cursor = Cursors.Hand;
                sidebarIcons[0].Click += (s, e) => NavigateTo(new Form17());
            }

            if (sidebarIcons.Count >= 2)
            {
                sidebarIcons[1].Cursor = Cursors.Hand;
                sidebarIcons[1].Click += async (s, e) => await LoadDispatcherProfileAsync();
            }

            if (sidebarIcons.Count >= 3)
            {
                sidebarIcons[2].Cursor = Cursors.Hand;
                sidebarIcons[2].Click += (s, e) => NavigateTo(new Form18());
            }

            // Status label inside the bottom dark-maroon bar
            _lblBottomStatus = new Label
            {
                Left = 48,
                Top = Math.Max(10, this.ClientSize.Height - 48),
                Width = Math.Max(300, this.ClientSize.Width - 96),
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "LGU DISPATCHER PROFILE & CREDENTIAL SETTINGS"
            };
            this.Controls.Add(_lblBottomStatus);
            _lblBottomStatus.BringToFront();
        }

        private TextBox? FindOrCreatePasswordTextBox(List<Control> allControls, Label? anchorLabel)
        {
            if (anchorLabel == null) return null;

            Point lblScreen = anchorLabel.PointToScreen(Point.Empty);

            // Check if a TextBox already exists inside the same container or directly below the label
            var existingBox = allControls
                .OfType<TextBox>()
                .Select(t => new { Box = t, Pos = t.PointToScreen(Point.Empty) })
                .Where(x => Math.Abs(x.Pos.X - lblScreen.X) <= 45 &&
                            x.Pos.Y >= lblScreen.Y - 6 &&
                            x.Pos.Y <= lblScreen.Y + 75)
                .OrderBy(x => x.Pos.Y)
                .Select(x => x.Box)
                .FirstOrDefault();

            if (existingBox != null)
            {
                existingBox.ForeColor = Color.White;
                existingBox.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
                existingBox.UseSystemPasswordChar = true;
                return existingBox;
            }

            //  If the label sits inside a dark-maroon Panel box without a TextBox, place one inside it
            if (anchorLabel.Parent is Panel boxPanel && boxPanel.Height <= 110)
            {
                var injected = new TextBox
                {
                    Left = anchorLabel.Left,
                    Top = anchorLabel.Bottom + 4,
                    Width = Math.Max(160, boxPanel.Width - (anchorLabel.Left * 2)),
                    BackColor = boxPanel.BackColor,
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                    UseSystemPasswordChar = true
                };
                boxPanel.Controls.Add(injected);
                injected.BringToFront();
                return injected;
            }

            return null;
        }

        private async Task LoadDispatcherProfileAsync()
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        u.UserID,
                        u.BarangayIDNumber,
                        u.Username,
                        u.FullName,
                        u.Zone,
                        u.Phone_Number,
                        u.UserImagePath,
                        d.DispatcherID,
                        d.DepartmentRole,
                        d.AssignedZone
                    FROM Users u
                    LEFT JOIN LGU_Dispatcher d ON u.UserID = d.UserID
                    WHERE (@uid > 0 AND u.UserID = @uid)
                       OR (@uid <= 0 AND u.Role = 'LGUDispatcher')
                    ORDER BY u.UserID ASC
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@uid", DispatcherSession.UserID);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    if (_lblDispatcherName != null) _lblDispatcherName.Text = "LGU DISPATCHER";
                    if (_lblDispatcherId != null) _lblDispatcherId.Text = "NO ACTIVE SESSION";
                    return;
                }

                int userId = Convert.ToInt32(reader["UserID"]);
                string barangayId = reader["BarangayIDNumber"]?.ToString() ?? "N/A";
                string username = reader["Username"]?.ToString() ?? "";
                string fullName = reader["FullName"]?.ToString() ?? "LGU Dispatcher";
                string deptRole = reader["DepartmentRole"]?.ToString() ?? "Operations Officer";
                string assignedZone = reader["AssignedZone"]?.ToString() ?? (reader["Zone"]?.ToString() ?? "Zone 1");
                string imagePath = reader["UserImagePath"]?.ToString() ?? "";

                // Keep DispatcherSession synchronized
                DispatcherSession.UserID = userId;
                if (reader["DispatcherID"] != DBNull.Value)
                    DispatcherSession.DispatcherID = Convert.ToInt32(reader["DispatcherID"]);
                DispatcherSession.BarangayIDNumber = barangayId;
                DispatcherSession.Username = username;
                DispatcherSession.FullName = fullName;
                DispatcherSession.DepartmentRole = deptRole;
                DispatcherSession.AssignedZone = assignedZone;

                if (_lblDispatcherName != null)
                {
                    _lblDispatcherName.Text = fullName.ToUpperInvariant();
                }

                if (_lblDispatcherId != null)
                {
                    _lblDispatcherId.Text = $"DISPATCHER ID: {barangayId}   •   {deptRole.ToUpperInvariant()} ({assignedZone.ToUpperInvariant()})";
                }

                if (_lblBottomStatus != null)
                {
                    _lblBottomStatus.Text = $"SIGNED IN AS @{username.ToUpperInvariant()}   •   CLICK AVATAR TO UPDATE PHOTO";
                }

                LoadAvatarImage(imagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load dispatcher profile:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAvatarImage(string storedPath)
        {
            if (_picAvatar == null || string.IsNullOrWhiteSpace(storedPath)) return;

            string resolved = storedPath;
            if (!File.Exists(resolved))
            {
                string combined = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, storedPath.TrimStart('\\', '/'));
                if (File.Exists(combined)) resolved = combined;
            }

            if (!File.Exists(resolved)) return;

            try
            {
                byte[] bytes = File.ReadAllBytes(resolved);
                using var ms = new MemoryStream(bytes);
                using var img = Image.FromStream(ms);
                _picAvatar.Image = new Bitmap(img);
            }
            catch
            {
                // Keep default designer avatar if file cannot be decoded
            }
        }

        private async Task ChangeAvatarPhotoAsync()
        {
            if (DispatcherSession.UserID <= 0) return;

            using var ofd = new OpenFileDialog
            {
                Title = "Select Dispatcher Profile Photo",
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                string avatarDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");
                Directory.CreateDirectory(avatarDir);

                string fileName = $"disp_{DispatcherSession.UserID}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(ofd.FileName)}";
                string destFull = Path.Combine(avatarDir, fileName);
                File.Copy(ofd.FileName, destFull, overwrite: true);

                string relativePath = Path.Combine("Avatars", fileName);

                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                using (var regCmd = new MySqlCommand(
                    "INSERT INTO Image_Registry (FilePath, EntityType) VALUES (@path, 'Avatar');", conn))
                {
                    regCmd.Parameters.AddWithValue("@path", relativePath);
                    await regCmd.ExecuteNonQueryAsync();
                }

                using (var updCmd = new MySqlCommand(
                    "UPDATE Users SET UserImagePath = @path WHERE UserID = @uid;", conn))
                {
                    updCmd.Parameters.AddWithValue("@path", relativePath);
                    updCmd.Parameters.AddWithValue("@uid", DispatcherSession.UserID);
                    await updCmd.ExecuteNonQueryAsync();
                }

                LoadAvatarImage(destFull);
                MessageBox.Show("Dispatcher profile photo updated.", "Profile Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update profile photo:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateDispatcherPasswordAsync()
        {
            if (_isSaving) return;

            string currentPwd = _txtCurrentPassword?.Text ?? "";
            string newPwd = _txtNewPassword?.Text ?? "";
            string verifyPwd = _txtVerifyPassword?.Text ?? "";

            if (string.IsNullOrWhiteSpace(currentPwd) ||
                string.IsNullOrWhiteSpace(newPwd) ||
                string.IsNullOrWhiteSpace(verifyPwd))
            {
                MessageBox.Show(
                    "Please fill in Current Password, New Password, and Verify New Password.",
                    "Missing Fields",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (newPwd.Length < 4)
            {
                MessageBox.Show(
                    "New password must be at least 4 characters long.",
                    "Weak Password",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtNewPassword?.Focus();
                return;
            }

            if (newPwd != verifyPwd)
            {
                MessageBox.Show(
                    "New Password and Verify New Password do not match.",
                    "Password Mismatch",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtVerifyPassword?.Clear();
                _txtVerifyPassword?.Focus();
                return;
            }

            if (currentPwd == newPwd)
            {
                MessageBox.Show(
                    "Your new password must be different from your current password.",
                    "No Changes Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _isSaving = true;
            if (_btnUpdatePassword != null) _btnUpdatePassword.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string storedPassword = "";
                using (var getCmd = new MySqlCommand("SELECT Password FROM Users WHERE UserID = @uid LIMIT 1;", conn))
                {
                    getCmd.Parameters.AddWithValue("@uid", DispatcherSession.UserID);
                    object? val = await getCmd.ExecuteScalarAsync();
                    if (val == null || val == DBNull.Value)
                    {
                        MessageBox.Show("Active dispatcher account could not be found.", "Account Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    storedPassword = val.ToString() ?? "";
                }

                if (!VerifyPassword(currentPwd, storedPassword))
                {
                    MessageBox.Show(
                        "Current Password is incorrect.",
                        "Verification Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    _txtCurrentPassword?.Clear();
                    _txtCurrentPassword?.Focus();
                    return;
                }

                string newPasswordHash = HashPasswordForStorage(newPwd);

                using (var updCmd = new MySqlCommand("UPDATE Users SET Password = @pwd WHERE UserID = @uid;", conn))
                {
                    updCmd.Parameters.AddWithValue("@pwd", newPasswordHash);
                    updCmd.Parameters.AddWithValue("@uid", DispatcherSession.UserID);
                    await updCmd.ExecuteNonQueryAsync();
                }

                _txtCurrentPassword?.Clear();
                _txtNewPassword?.Clear();
                _txtVerifyPassword?.Clear();

                MessageBox.Show(
                    "Dispatcher password has been updated successfully!",
                    "Credentials Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update password:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isSaving = false;
                if (_btnUpdatePassword != null) _btnUpdatePassword.Enabled = true;
            }
        }

        private static bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            var helperType = typeof(Form20).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals("PasswordHelper", StringComparison.Ordinal));
            if (helperType != null)
            {
                var verifyMethod = helperType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.ReturnType == typeof(bool) &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[0].ParameterType == typeof(string) &&
                        m.GetParameters()[1].ParameterType == typeof(string));

                if (verifyMethod != null && verifyMethod.Invoke(null, new object[] { plainPassword, storedHash }) is bool isValid && isValid)
                {
                    return true;
                }
            }

            string sha256Hex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword)));
            if (storedHash.Equals(sha256Hex, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return storedHash == plainPassword;
        }

        private static string HashPasswordForStorage(string plainPassword)
        {
            var helperType = typeof(Form20).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals("PasswordHelper", StringComparison.Ordinal));
            if (helperType != null)
            {
                var hashMethod = helperType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.ReturnType == typeof(string) &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

                if (hashMethod != null && hashMethod.Invoke(null, new object[] { plainPassword }) is string hashed && !string.IsNullOrWhiteSpace(hashed))
                {
                    return hashed;
                }
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword)));
        }

        private void NavigateTo(Form nextForm)
        {
            nextForm.FormClosed += (s, args) => Application.Exit();
            nextForm.Show();
            this.Hide();
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