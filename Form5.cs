using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form5 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private Label? _lblAnnouncementBody;
        private Label? _lblAnnouncementCounter;
        private Label? _lblPointsEarned;
        private Label? _lblActiveQuestTitle;
        private Label? _lblActiveQuestDesc;
        private Label? _lblTaskRewardPoints;
        private Label? _lblTaskRewardSupply;
        private Label? _lblRedeemedTools;
        private Label? _lblBottomTicker;

        private readonly List<AnnouncementItem> _announcements = new();
        private int _currentAnnouncementIndex = 0;
        private bool _isRefreshing = false;

        private class AnnouncementItem
        {
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Priority { get; set; } = "Normal";
            public string PostedBy { get; set; } = "Barangay Admin";
            public DateTime CreatedAt { get; set; }
        }

        public Form5()
        {
            InitializeComponent();
            BindDashboardControls();
            SetupCardCursors();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await RefreshDashboardDataAsync();
            this.VisibleChanged += async (s, e) =>
            {
                if (this.Visible)
                {
                    await RefreshDashboardDataAsync();
                }
            };
        }

        private void SetupCardCursors()
        {
            Control?[] navControls = { citizenprofile, questmap, questlist, pictureBox3 };
            foreach (var ctrl in navControls)
            {
                if (ctrl != null)
                {
                    ctrl.Cursor = Cursors.Hand;
                }
            }
        }

        private void BindDashboardControls()
        {
            var allLabels = GetAllControls(this).OfType<Label>().ToList();

            // Bind directly to your existing Designer labels
            _lblPointsEarned = this.lblPointsEarned;
            _lblActiveQuestTitle = this.lblActiveQuestTitle;
            _lblActiveQuestDesc = this.lblActiveQuestDesc;
            _lblRedeemedTools = this.lblRedeemedTools;

            // Bind remaining labels by text
            _lblTaskRewardPoints = allLabels.FirstOrDefault(l =>
                l.Text.Equals("REWARD POINTS", StringComparison.OrdinalIgnoreCase));

            _lblTaskRewardSupply = allLabels.FirstOrDefault(l =>
                l.Text.Equals("REWARD SUPPLY", StringComparison.OrdinalIgnoreCase));

            // Confine Announcement Box strictly to the bottom-left container
            _lblAnnouncementBody = allLabels.FirstOrDefault(l =>
                l.Text.Contains("THIS SHOWS THE ANNOUNCEMENT", StringComparison.OrdinalIgnoreCase));

            if (_lblAnnouncementBody != null && _lblAnnouncementBody.Parent is Control container)
            {
                Rectangle orig = _lblAnnouncementBody.Bounds;
                bool hasDedicatedPanel = container != this && container.Width < (this.ClientSize.Width * 0.6);

                int boxLeft = hasDedicatedPanel ? 14 : Math.Max(24, orig.Left + (orig.Width / 2) - 155);
                int boxWidth = hasDedicatedPanel ? Math.Max(200, container.Width - 28) : 310;
                int bodyTop = hasDedicatedPanel ? 44 : orig.Top;
                int bodyHeight = hasDedicatedPanel ? Math.Max(60, container.Height - 54) : Math.Max(80, orig.Height + 24);
                int headerBtnTop = hasDedicatedPanel ? 10 : Math.Max(10, orig.Top - 28);

                _lblAnnouncementBody.AutoSize = false;
                _lblAnnouncementBody.Left = boxLeft;
                _lblAnnouncementBody.Top = bodyTop;
                _lblAnnouncementBody.Width = boxWidth;
                _lblAnnouncementBody.Height = bodyHeight;
                _lblAnnouncementBody.TextAlign = ContentAlignment.TopLeft;
                _lblAnnouncementBody.BackColor = Color.FromArgb(66, 44, 14);
                _lblAnnouncementBody.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

                var btnNextAnn = new Button
                {
                    Text = ">",
                    Width = 24,
                    Height = 20,
                    Top = headerBtnTop,
                    Left = boxLeft + boxWidth - 24,
                    BackColor = Color.FromArgb(165, 62, 26),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnNextAnn.Click += (s, e) => CycleAnnouncement(1);

                var btnPrevAnn = new Button
                {
                    Text = "<",
                    Width = 24,
                    Height = 20,
                    Top = headerBtnTop,
                    Left = btnNextAnn.Left - 27,
                    BackColor = Color.FromArgb(165, 62, 26),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnPrevAnn.Click += (s, e) => CycleAnnouncement(-1);

                _lblAnnouncementCounter = new Label
                {
                    Text = "",
                    Width = 42,
                    Height = 18,
                    Top = headerBtnTop + 1,
                    Left = btnPrevAnn.Left - 45,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.Khaki,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    BackColor = Color.Transparent
                };

                container.Controls.Add(btnNextAnn);
                container.Controls.Add(btnPrevAnn);
                container.Controls.Add(_lblAnnouncementCounter);
                btnNextAnn.BringToFront();
                btnPrevAnn.BringToFront();
                _lblAnnouncementCounter.BringToFront();
            }

            // Bottom Dark-Red Bar Status Label
            _lblBottomTicker = new Label
            {
                Left = 48,
                Top = Math.Max(10, this.ClientSize.Height - 52),
                Width = Math.Max(300, this.ClientSize.Width - 96),
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = string.IsNullOrWhiteSpace(UserSession.FullName)
                    ? "CITIZEN WORKER PORTAL • ACTIVE"
                    : $"WELCOME, {UserSession.FullName.ToUpperInvariant()} • BARANGAY QUEST DISPATCH"
            };
            this.Controls.Add(_lblBottomTicker);
            _lblBottomTicker.BringToFront();
        }

        private async Task RefreshDashboardDataAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            try
            {
                await LoadAnnouncementsAsync();
                await LoadWorkerPointsAndActiveQuestAsync();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private async Task LoadAnnouncementsAsync()
        {
            _announcements.Clear();
            _currentAnnouncementIndex = 0;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string ensureSql = @"
                    CREATE TABLE IF NOT EXISTS Announcements (
                        AnnouncementID INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(120) NOT NULL,
                        Message TEXT NOT NULL,
                        Priority ENUM('Normal', 'Urgent') NOT NULL DEFAULT 'Normal',
                        PostedBy VARCHAR(100) NOT NULL,
                        IsActive TINYINT(1) NOT NULL DEFAULT 1,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";
                using (var ensureCmd = new MySqlCommand(ensureSql, conn))
                {
                    await ensureCmd.ExecuteNonQueryAsync();
                }

                string sql = @"
                    SELECT Title, Message, Priority, PostedBy, CreatedAt
                    FROM Announcements
                    WHERE IsActive = 1
                    ORDER BY FIELD(Priority, 'Urgent', 'Normal'), CreatedAt DESC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    _announcements.Add(new AnnouncementItem
                    {
                        Title = reader["Title"]?.ToString() ?? "Barangay Notice",
                        Message = reader["Message"]?.ToString() ?? "",
                        Priority = reader["Priority"]?.ToString() ?? "Normal",
                        PostedBy = reader["PostedBy"]?.ToString() ?? "Barangay Admin",
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }

                DisplayCurrentAnnouncement();
            }
            catch (Exception ex)
            {
                if (_lblAnnouncementBody != null)
                    _lblAnnouncementBody.Text = $"Unable to load announcements: {ex.Message}";
            }
        }

        private void CycleAnnouncement(int step)
        {
            if (_announcements.Count <= 1) return;
            _currentAnnouncementIndex = (_currentAnnouncementIndex + step + _announcements.Count) % _announcements.Count;
            DisplayCurrentAnnouncement();
        }

        private void DisplayCurrentAnnouncement()
        {
            if (_lblAnnouncementBody == null) return;

            if (_announcements.Count == 0)
            {
                _lblAnnouncementBody.ForeColor = Color.Silver;
                _lblAnnouncementBody.Text = "No active announcements from Barangay Admin at this time.";
                if (_lblAnnouncementCounter != null) _lblAnnouncementCounter.Text = "";
                return;
            }

            var item = _announcements[_currentAnnouncementIndex];
            bool isUrgent = item.Priority.Equals("Urgent", StringComparison.OrdinalIgnoreCase);

            _lblAnnouncementBody.ForeColor = isUrgent ? Color.LightCoral : Color.WhiteSmoke;
            _lblAnnouncementBody.Text =
                $"{(isUrgent ? "[URGENT] " : "[NOTICE] ")}{item.Title.ToUpperInvariant()}\n" +
                $"{item.Message}\n\n" +
                $"Posted by {item.PostedBy} • {item.CreatedAt:MMM dd, yyyy hh:mm tt}";

            if (_lblAnnouncementCounter != null)
                _lblAnnouncementCounter.Text = $"{_currentAnnouncementIndex + 1}/{_announcements.Count}";
        }

        private async Task LoadWorkerPointsAndActiveQuestAsync()
        {
            if (UserSession.UserID <= 0 && UserSession.WorkerID <= 0) return;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                // Query Users.Points using UserSession.UserID (with Citizen_Worker fallback)
                string ptsSql = UserSession.UserID > 0
                    ? "SELECT COALESCE(Points, 0) FROM Users WHERE UserID = @uid LIMIT 1;"
                    : @"SELECT COALESCE(u.Points, 0)
                        FROM Users u
                        INNER JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                        WHERE cw.WorkerID = @wid
                        LIMIT 1;";

                using (var ptsCmd = new MySqlCommand(ptsSql, conn))
                {
                    ptsCmd.Parameters.AddWithValue("@uid", UserSession.UserID);
                    ptsCmd.Parameters.AddWithValue("@wid", UserSession.WorkerID);
                    object? ptsVal = await ptsCmd.ExecuteScalarAsync();
                    int points = ptsVal != null && ptsVal != DBNull.Value ? Convert.ToInt32(ptsVal) : 0;

                    UserSession.Points = points;
                    if (_lblPointsEarned != null)
                    {
                        _lblPointsEarned.Text = $"{points:N0} PTS";
                    }
                }

                if (UserSession.WorkerID <= 0) return;

                string questSql = @"
                    SELECT q.QuestID, q.Title, q.Category, q.Difficulty, q.BasePay, q.PointsReward, q.EndDate,
                           (SELECT GROUP_CONCAT(t.ToolName SEPARATOR ', ')
                            FROM Tool_Items t
                            WHERE t.AssignedQuestID = q.QuestID) AS AssignedTools
                    FROM Quest_Memberships m
                    INNER JOIN Quests q ON q.QuestID = m.QuestID
                    WHERE m.WorkerID = @wid
                      AND m.Status = 'Joined'
                      AND q.Status IN ('Upcoming', 'Active')
                    ORDER BY m.JoinedAt DESC
                    LIMIT 1;";

                using var qCmd = new MySqlCommand(questSql, conn);
                qCmd.Parameters.AddWithValue("@wid", UserSession.WorkerID);
                using var reader = await qCmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string title = reader["Title"]?.ToString() ?? "Active Quest";
                    string category = reader["Category"]?.ToString() ?? "";
                    string difficulty = reader["Difficulty"]?.ToString() ?? "";
                    DateTime deadline = Convert.ToDateTime(reader["EndDate"]);
                    int ptsReward = Convert.ToInt32(reader["PointsReward"]);
                    string rawPay = reader["BasePay"]?.ToString() ?? "None";
                    string supply = decimal.TryParse(rawPay, out decimal amt) ? $"PHP {amt:N2}" : rawPay;
                    string tools = reader["AssignedTools"] == DBNull.Value || string.IsNullOrWhiteSpace(reader["AssignedTools"]?.ToString())
                        ? "No Tools Assigned"
                        : reader["AssignedTools"].ToString()!;

                    if (_lblActiveQuestTitle != null) _lblActiveQuestTitle.Text = title.ToUpperInvariant();
                    if (_lblActiveQuestDesc != null) _lblActiveQuestDesc.Text = $"{category} • {difficulty} • Deadline: {deadline:MMM dd, yyyy hh:mm tt}";
                    if (_lblTaskRewardPoints != null) _lblTaskRewardPoints.Text = $"+{ptsReward} PTS";
                    if (_lblTaskRewardSupply != null) _lblTaskRewardSupply.Text = supply;
                    if (_lblRedeemedTools != null) _lblRedeemedTools.Text = tools;
                }
                else
                {
                    if (_lblActiveQuestTitle != null) _lblActiveQuestTitle.Text = "NO ACTIVE QUEST";
                    if (_lblActiveQuestDesc != null) _lblActiveQuestDesc.Text = "Open Quest Map to accept an available barangay task.";
                    if (_lblTaskRewardPoints != null) _lblTaskRewardPoints.Text = "0 PTS";
                    if (_lblTaskRewardSupply != null) _lblTaskRewardSupply.Text = "NONE";
                    if (_lblRedeemedTools != null) _lblRedeemedTools.Text = "NONE";
                }
            }
            catch
            {
                // Keep Designer fallback text if database is temporarily unreachable
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

        // Navigation card handlers bound in Form5.Designer.cs
        private void questmap_Click(object? sender, EventArgs e)
        {
            NavigateTo(new Form23());
        }

        private void citizenprofile_Click(object? sender, EventArgs e)
        {
            NavigateTo(new Form6());
        }

        private void questlist_Click_1(object? sender, EventArgs e)
        {
            NavigateTo(new Form8());
        }

        private void pictureBox3_Click(object? sender, EventArgs e)
        {
            // Link to your Points Shop & Redemption form (e.g., NavigateTo(new Form11());)
            MessageBox.Show(
                "Points Shop & Redemption screen is ready to be linked here.",
                "Points Shop & Redemption",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void Form5_Load(object? sender, EventArgs e) { }
    }
}