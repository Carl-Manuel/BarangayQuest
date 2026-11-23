using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form17 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private Panel? _pnlAnnouncementScroll;
        private Label? _lblAnnouncementBody;
        private Label? _lblAnnouncementCounter;

        private Panel? _pnlSubmissionScroll;
        private Label? _lblSubmissionUpdates;
        private Label? _lblBottomTicker;

        private readonly List<AnnouncementItem> _announcements = new();
        private int _currentAnnouncementIndex = 0;

        private sealed class AnnouncementItem
        {
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Priority { get; set; } = "Normal";
            public string PostedBy { get; set; } = "Barangay Admin";
            public DateTime CreatedAt { get; set; }
        }

        public Form17()
        {
            InitializeComponent();
            BindDashboardControls();
            SetupCardCursors();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await RefreshDashboardDataAsync();
        }

        private void SetupCardCursors()
        {
            Control?[] cards = { dispatcherprofile, worksubmissions, statusofusers };
            foreach (var card in cards)
            {
                if (card == null) continue;
                card.Cursor = Cursors.Hand;
                foreach (Control child in GetAllControls(card))
                {
                    child.Cursor = Cursors.Hand;
                }
            }
        }

        private void BindDashboardControls()
        {
            var allLabels = GetAllControls(this).OfType<Label>().ToList();

            // Scrollable Announcement Box in the bottom-left container
            var origAnnLabel = allLabels.FirstOrDefault(l =>
                l.Text.Contains("THIS SHOWS THE ANNOUNCEMENT", StringComparison.OrdinalIgnoreCase));

            if (origAnnLabel != null && origAnnLabel.Parent is Control annContainer)
            {
                Rectangle orig = origAnnLabel.Bounds;
                bool hasDedicatedPanel = annContainer != this && annContainer.Width < (this.ClientSize.Width * 0.6);

                int boxLeft = hasDedicatedPanel ? 16 : Math.Max(36, orig.Left + (orig.Width / 2) - 210);
                int boxWidth = hasDedicatedPanel ? Math.Max(220, annContainer.Width - 32) : 420;
                int bodyTop = hasDedicatedPanel ? 52 : orig.Top;
                int bodyHeight = hasDedicatedPanel ? Math.Max(80, annContainer.Height - 64) : Math.Max(110, orig.Height + 45);
                int headerBtnTop = hasDedicatedPanel ? 14 : Math.Max(14, orig.Top - 34);

                origAnnLabel.Visible = false;

                _pnlAnnouncementScroll = new Panel
                {
                    Left = boxLeft,
                    Top = bodyTop,
                    Width = boxWidth,
                    Height = bodyHeight,
                    BackColor = Color.FromArgb(66, 44, 14),
                    AutoScroll = true
                };

                _lblAnnouncementBody = new Label
                {
                    AutoSize = true,
                    MaximumSize = Size.Empty,
                    Location = new Point(4, 4),
                    Padding = new Padding(0, 0, 14, 14),
                    BackColor = Color.Transparent,
                    ForeColor = Color.WhiteSmoke,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    Text = "Loading announcements..."
                };

                _pnlAnnouncementScroll.Controls.Add(_lblAnnouncementBody);
                annContainer.Controls.Add(_pnlAnnouncementScroll);
                _pnlAnnouncementScroll.BringToFront();

                var btnNextAnn = new Button
                {
                    Text = ">",
                    Width = 26,
                    Height = 22,
                    Top = headerBtnTop,
                    Left = boxLeft + boxWidth - 26,
                    BackColor = Color.FromArgb(165, 62, 26),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnNextAnn.Click += (s, e) => CycleAnnouncement(1);

                var btnPrevAnn = new Button
                {
                    Text = "<",
                    Width = 26,
                    Height = 22,
                    Top = headerBtnTop,
                    Left = btnNextAnn.Left - 29,
                    BackColor = Color.FromArgb(165, 62, 26),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnPrevAnn.Click += (s, e) => CycleAnnouncement(-1);

                _lblAnnouncementCounter = new Label
                {
                    Text = "",
                    Width = 48,
                    Height = 20,
                    Top = headerBtnTop + 1,
                    Left = btnPrevAnn.Left - 52,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.Khaki,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    BackColor = Color.Transparent
                };

                annContainer.Controls.Add(btnNextAnn);
                annContainer.Controls.Add(btnPrevAnn);
                annContainer.Controls.Add(_lblAnnouncementCounter);
                btnNextAnn.BringToFront();
                btnPrevAnn.BringToFront();
                _lblAnnouncementCounter.BringToFront();
            }

            // Scrollable (Down + Sideways) Submission Updates Box in the bottom-right maroon container
            var origSubLabel = allLabels.FirstOrDefault(l =>
                l.Text.Contains("THIS SHOWS HOW MANY USERS", StringComparison.OrdinalIgnoreCase));

            if (origSubLabel != null && origSubLabel.Parent is Control subContainer)
            {
                Rectangle orig = origSubLabel.Bounds;
                bool hasDedicatedPanel = subContainer != this && subContainer.Width < (this.ClientSize.Width * 0.6);

                int boxLeft = hasDedicatedPanel ? 16 : Math.Max(36, orig.Left + (orig.Width / 2) - 210);
                int boxWidth = hasDedicatedPanel ? Math.Max(220, subContainer.Width - 32) : 420;
                int bodyTop = hasDedicatedPanel ? 52 : orig.Top;
                int bodyHeight = hasDedicatedPanel ? Math.Max(80, subContainer.Height - 64) : Math.Max(110, orig.Height + 45);
                int headerBtnTop = hasDedicatedPanel ? 14 : Math.Max(14, orig.Top - 34);

                origSubLabel.Visible = false;

                _pnlSubmissionScroll = new Panel
                {
                    Left = boxLeft,
                    Top = bodyTop,
                    Width = boxWidth,
                    Height = bodyHeight,
                    BackColor = Color.FromArgb(62, 12, 12),
                    AutoScroll = true
                };

                _lblSubmissionUpdates = new Label
                {
                    AutoSize = true,
                    MaximumSize = Size.Empty,
                    Location = new Point(4, 4),
                    Padding = new Padding(0, 0, 14, 14),
                    BackColor = Color.Transparent,
                    ForeColor = Color.WhiteSmoke,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    Text = "Loading submission updates..."
                };

                _pnlSubmissionScroll.Controls.Add(_lblSubmissionUpdates);
                subContainer.Controls.Add(_pnlSubmissionScroll);
                _pnlSubmissionScroll.BringToFront();

                var btnRefreshSubs = new Button
                {
                    Text = "REFRESH",
                    Width = 68,
                    Height = 22,
                    Top = headerBtnTop,
                    Left = boxLeft + boxWidth - 68,
                    BackColor = Color.FromArgb(165, 62, 26),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnRefreshSubs.Click += async (s, e) => await RefreshDashboardDataAsync();

                subContainer.Controls.Add(btnRefreshSubs);
                btnRefreshSubs.BringToFront();
            }

            // Bottom Dark-Red Bar Dispatcher Status Label
            string welcomeText = string.IsNullOrWhiteSpace(DispatcherSession.FullName)
                ? "LGU DISPATCHER COMMAND DESK • ACTIVE"
                : $"WELCOME, {DispatcherSession.FullName.ToUpperInvariant()} • {DispatcherSession.DepartmentRole.ToUpperInvariant()} ({DispatcherSession.AssignedZone.ToUpperInvariant()})";

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
                Text = welcomeText
            };
            this.Controls.Add(_lblBottomTicker);
            _lblBottomTicker.BringToFront();
        }

        private async Task RefreshDashboardDataAsync()
        {
            await LoadAnnouncementsAsync();
            await LoadSubmissionUpdatesAsync();
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

            if (_pnlAnnouncementScroll != null)
            {
                _pnlAnnouncementScroll.AutoScrollPosition = Point.Empty;
            }

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

        private async Task LoadSubmissionUpdatesAsync()
        {
            if (_lblSubmissionUpdates == null) return;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string summarySql = @"
                    SELECT
                        COUNT(ps.SubmissionID) AS TotalSubmissions,
                        COUNT(DISTINCT qm.WorkerID) AS UniqueWorkers,
                        COALESCE(SUM(CASE WHEN ps.Status = 'Pending' THEN 1 ELSE 0 END), 0) AS PendingCount,
                        COALESCE(SUM(CASE WHEN ps.Status = 'Approved' THEN 1 ELSE 0 END), 0) AS ApprovedCount,
                        COALESCE(SUM(CASE WHEN ps.Status = 'Rejected' THEN 1 ELSE 0 END), 0) AS RejectedCount
                    FROM Proof_Submissions ps
                    INNER JOIN Quest_Memberships qm ON ps.MembershipID = qm.MembershipID;";

                int totalSubs = 0, uniqueWorkers = 0, pendingCount = 0, approvedCount = 0, rejectedCount = 0;

                using (var sumCmd = new MySqlCommand(summarySql, conn))
                using (var reader = await sumCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        totalSubs = Convert.ToInt32(reader["TotalSubmissions"]);
                        uniqueWorkers = Convert.ToInt32(reader["UniqueWorkers"]);
                        pendingCount = Convert.ToInt32(reader["PendingCount"]);
                        approvedCount = Convert.ToInt32(reader["ApprovedCount"]);
                        rejectedCount = Convert.ToInt32(reader["RejectedCount"]);
                    }
                }

                var recentLines = new List<string>();
                string recentSql = @"
                    SELECT u.FullName, q.Title AS QuestTitle, ps.Status, ps.SubmittedAt
                    FROM Proof_Submissions ps
                    INNER JOIN Quest_Memberships qm ON ps.MembershipID = qm.MembershipID
                    INNER JOIN Citizen_Worker cw ON qm.WorkerID = cw.WorkerID
                    INNER JOIN Users u ON cw.UserID = u.UserID
                    INNER JOIN Quests q ON qm.QuestID = q.QuestID
                    ORDER BY FIELD(ps.Status, 'Pending', 'Approved', 'Rejected'), ps.SubmittedAt DESC
                    LIMIT 25;";

                using (var recCmd = new MySqlCommand(recentSql, conn))
                using (var reader = await recCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string worker = reader["FullName"]?.ToString() ?? "Citizen Worker";
                        string quest = reader["QuestTitle"]?.ToString() ?? "Quest";
                        string status = reader["Status"]?.ToString()?.ToUpperInvariant() ?? "PENDING";
                        DateTime submittedAt = Convert.ToDateTime(reader["SubmittedAt"]);

                        recentLines.Add($"• [{status}] {worker} — {quest} ({submittedAt:MMM dd, hh:mm tt})");
                    }
                }

                if (_pnlSubmissionScroll != null)
                {
                    _pnlSubmissionScroll.AutoScrollPosition = Point.Empty;
                }

                if (totalSubs == 0)
                {
                    _lblSubmissionUpdates.ForeColor = Color.Silver;
                    _lblSubmissionUpdates.Text =
                        "PENDING REVIEW: 0   |   APPROVED: 0   |   REJECTED: 0\n" +
                        "Citizens Who Submitted Work: 0\n\n" +
                        "No proof of work submissions recorded yet.\n" +
                        "Open WORK SUBMISSIONS once citizens upload their task proofs.";
                }
                else
                {
                    _lblSubmissionUpdates.ForeColor = pendingCount > 0 ? Color.Khaki : Color.WhiteSmoke;
                    _lblSubmissionUpdates.Text =
                        $"PENDING REVIEW: {pendingCount}   |   APPROVED: {approvedCount}   |   REJECTED: {rejectedCount}\n" +
                        $"Citizens Who Submitted Work: {uniqueWorkers} ({totalSubs} total)\n\n" +
                        "RECENT SUBMISSIONS:\n" +
                        string.Join("\n", recentLines);
                }
            }
            catch (Exception ex)
            {
                _lblSubmissionUpdates.Text = $"Unable to load submission updates: {ex.Message}";
            }
        }

        private void NavigateTo(Form nextForm)
        {
            nextForm.FormClosed += (s, e) => this.Close();
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

        private void dispatcherprofile_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form19());
        }

        private void worksubmissions_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form22());
        }

        private void statusofusers_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form18());
        }
    }
}