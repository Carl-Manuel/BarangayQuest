using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form19 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private Label? _lblDispatcherName;
        private Label? _lblDispatcherId;
        private Label? _lblLguRoleValue;
        private Label? _lblPhoneValue;
        private Label? _lblBottomStatus;
        private PictureBox? _picAvatar;

        public Form19()
        {
            InitializeComponent();
            BindProfileControls();
            _resizer = new FormResizer(this);
        }

        private async void Form19_Load(object sender, EventArgs e)
        {
            await LoadDispatcherProfileAsync();
        }

        private void BindProfileControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();
            var allPictureBoxes = allControls.OfType<PictureBox>().ToList();

            // 1. Bind NAME and DISPATCHER ID labels in the top maroon card
            _lblDispatcherName = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("NAME", StringComparison.OrdinalIgnoreCase));

            _lblDispatcherId = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("DISPATCHER ID", StringComparison.OrdinalIgnoreCase));

            // 2. Bind LGU DISPATCHER value label (below LGU NAME)
            _lblLguRoleValue = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("LGU DISPATCHER", StringComparison.OrdinalIgnoreCase));

            // 3. Bind the lower PHONE NUMBER value label (below the PHONE NUMBER header)
            var phoneLabels = allLabels
                .Where(l => l.Text.Trim().Equals("PHONE NUMBER", StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Top)
                .ToList();

            if (phoneLabels.Count >= 2)
                _lblPhoneValue = phoneLabels[1];
            else if (phoneLabels.Count == 1)
                _lblPhoneValue = phoneLabels[0];

            // 4. Bind Avatar PictureBox (allow clicking to update profile photo)
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

            // 5. Wire MANAGE ACCOUNT SECURITY button to open Form20
            var btnSecurity = allControls.FirstOrDefault(c =>
                c.Text.Trim().Equals("MANAGE ACCOUNT SECURITY", StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains("security", StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains("manage", StringComparison.OrdinalIgnoreCase));

            if (btnSecurity != null)
            {
                btnSecurity.Cursor = Cursors.Hand;
                btnSecurity.Click += (s, e) => NavigateTo(new Form20());
            }

            // 6. Wire the Left Scroll Navigation Icons (1st = Dashboard, 2nd = Profile, 3rd = Status of Users)
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

            // 7. Status label inside the bottom dark-maroon bar
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
                Text = "LGU DISPATCHER PROFILE OVERVIEW"
            };
            this.Controls.Add(_lblBottomStatus);
            _lblBottomStatus.BringToFront();
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
                string phoneNumber = reader["Phone_Number"]?.ToString() ?? "Not Set";
                string imagePath = reader["UserImagePath"]?.ToString() ?? "";

                // Synchronize DispatcherSession
                DispatcherSession.UserID = userId;
                if (reader["DispatcherID"] != DBNull.Value)
                    DispatcherSession.DispatcherID = Convert.ToInt32(reader["DispatcherID"]);
                DispatcherSession.BarangayIDNumber = barangayId;
                DispatcherSession.Username = username;
                DispatcherSession.FullName = fullName;
                DispatcherSession.DepartmentRole = deptRole;
                DispatcherSession.AssignedZone = assignedZone;

                if (_lblDispatcherName != null)
                    _lblDispatcherName.Text = fullName.ToUpperInvariant();

                if (_lblDispatcherId != null)
                    _lblDispatcherId.Text = $"DISPATCHER ID: {barangayId}   •   {assignedZone.ToUpperInvariant()}";

                if (_lblLguRoleValue != null)
                    _lblLguRoleValue.Text = deptRole.ToUpperInvariant();

                if (_lblPhoneValue != null)
                    _lblPhoneValue.Text = string.IsNullOrWhiteSpace(phoneNumber) ? "NOT SET" : phoneNumber;

                if (_lblBottomStatus != null)
                    _lblBottomStatus.Text = $"SIGNED IN AS @{username.ToUpperInvariant()}   •   {deptRole.ToUpperInvariant()} ({assignedZone.ToUpperInvariant()})   •   CLICK AVATAR TO CHANGE PHOTO";

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

        private void manageaccsecurity_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form20());
        }
    }
}