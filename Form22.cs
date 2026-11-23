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
    public partial class Form22 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private sealed class PendingSubmissionItem
        {
            public int SubmissionID { get; set; }
            public int MembershipID { get; set; }
            public int QuestID { get; set; }
            public int WorkerID { get; set; }
            public int UserID { get; set; }
            public string BarangayIDNumber { get; set; } = "";
            public string WorkerName { get; set; } = "";
            public string QuestTitle { get; set; } = "";
            public int PointsReward { get; set; }
            public string ImagePath { get; set; } = "";
            public DateTime SubmittedAt { get; set; }
            public DateTime QuestDeadline { get; set; }
        }

        private readonly List<PendingSubmissionItem> _pendingSubmissions = new();
        private int _currentIndex = 0;
        private bool _isProcessing = false;

        private DataGridView? _dgvSubmissions;
        private Control? _displayCanvas;
        private PictureBox? _picProofViewer;
        private Label? _lblViewerPlaceholder;
        private Label? _lblBottomStatus;

        public Form22()
        {
            InitializeComponent();
            this.KeyPreview = true;
            BindControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await LoadPendingSubmissionsAsync();
        }

        private void BindControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allPictureBoxes = allControls.OfType<PictureBox>().ToList();
            var allLabels = allControls.OfType<Label>().ToList();

            // Bind the Submission Details DataGridView
            _dgvSubmissions = allControls.OfType<DataGridView>().FirstOrDefault();
            if (_dgvSubmissions != null)
            {
                _dgvSubmissions.AllowUserToAddRows = false;
                _dgvSubmissions.AllowUserToDeleteRows = false;
                _dgvSubmissions.ReadOnly = true;
                _dgvSubmissions.MultiSelect = false;
                _dgvSubmissions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _dgvSubmissions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                _dgvSubmissions.RowHeadersVisible = false;
                _dgvSubmissions.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                _dgvSubmissions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                _dgvSubmissions.CellClick += (s, e) =>
                {
                    if (e.RowIndex >= 0 && e.RowIndex < _pendingSubmissions.Count)
                    {
                        DisplaySubmissionAt(e.RowIndex);
                    }
                };
            }

            // Locate ONLY the inner White Box below the DataGridView (never an ancestor container)
            int gridScreenBottom = _dgvSubmissions != null
                ? _dgvSubmissions.PointToScreen(Point.Empty).Y + _dgvSubmissions.Height
                : this.PointToScreen(Point.Empty).Y + 120;

            _displayCanvas = allControls
                .Where(c => (c is Panel || c is PictureBox) &&
                            c != _dgvSubmissions &&
                            !IsAncestorOf(c, _dgvSubmissions) &&
                            c.Controls.Count == 0 &&
                            c.Width >= 200 && c.Width <= (int)(this.ClientSize.Width * 0.72) &&
                            c.Height >= 90 && c.Height <= (int)(this.ClientSize.Height * 0.55) &&
                            c.PointToScreen(Point.Empty).Y >= gridScreenBottom - 5)
                .OrderByDescending(c => c.BackColor.R + c.BackColor.G + c.BackColor.B)
                .ThenByDescending(c => c.Width * c.Height)
                .FirstOrDefault();

            if (_displayCanvas != null)
            {
                if (_displayCanvas is PictureBox existingPb)
                {
                    _picProofViewer = existingPb;
                    _picProofViewer.SizeMode = PictureBoxSizeMode.Zoom;
                }
                else
                {
                    _picProofViewer = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = _displayCanvas.BackColor,
                        SizeMode = PictureBoxSizeMode.Zoom
                    };
                    _displayCanvas.Controls.Add(_picProofViewer);
                }

                _lblViewerPlaceholder = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.DimGray,
                    BackColor = _displayCanvas.BackColor,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Text = "Loading proof of work submissions..."
                };

                _displayCanvas.Controls.Add(_lblViewerPlaceholder);
                _lblViewerPlaceholder.BringToFront();
            }

            // Wire the Left Scroll Navigation Icons (1st = Dashboard, 2nd = Profile, 3rd = Status of Users)
            var sidebarIcons = allPictureBoxes
                .Where(p => p != _picProofViewer && p.Left < 130 && p.Top > 120 && p.Width < 90)
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
                sidebarIcons[1].Click += (s, e) =>
                {
                    MessageBox.Show(
                        $"Dispatcher: {(string.IsNullOrWhiteSpace(DispatcherSession.FullName) ? "LGU Dispatcher" : DispatcherSession.FullName)}\n" +
                        $"Officer ID: {DispatcherSession.BarangayIDNumber}\n" +
                        $"Department: {DispatcherSession.DepartmentRole}\n" +
                        $"Assigned Zone: {DispatcherSession.AssignedZone}",
                        "Dispatcher Profile",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                };
            }

            if (sidebarIcons.Count >= 3)
            {
                sidebarIcons[2].Cursor = Cursors.Hand;
                sidebarIcons[2].Click += (s, e) => NavigateTo(new Form18());
            }

            // Wire the 4 Keycap Icons below the white image box: [0]=Prev, [1]=Next, [2]=Accept (K), [3]=Reject (L)
            int canvasBottom = _displayCanvas != null
                ? _displayCanvas.PointToScreen(Point.Empty).Y + _displayCanvas.Height
                : gridScreenBottom + 200;

            var keycapButtons = allPictureBoxes
                .Where(p => p != _picProofViewer && p.Width >= 24 && p.Width <= 85 && p.Height >= 24 && p.Height <= 85)
                .Select(p => new { Box = p, ScreenPos = p.PointToScreen(Point.Empty) })
                .Where(x => x.ScreenPos.Y >= canvasBottom - 5)
                .OrderBy(x => x.ScreenPos.X)
                .Select(x => x.Box)
                .ToList();

            if (keycapButtons.Count >= 4)
            {
                keycapButtons[0].Cursor = Cursors.Hand;
                keycapButtons[0].Click += (s, e) => NavigateSubmission(-1);

                keycapButtons[1].Cursor = Cursors.Hand;
                keycapButtons[1].Click += (s, e) => NavigateSubmission(1);

                keycapButtons[2].Cursor = Cursors.Hand;
                keycapButtons[2].Click += async (s, e) => await AcceptCurrentSubmissionAsync();

                keycapButtons[3].Cursor = Cursors.Hand;
                keycapButtons[3].Click += async (s, e) => await RejectCurrentSubmissionAsync();
            }

            // Wire UPDATE button if present
            var btnUpdate = allControls.FirstOrDefault(c =>
                c.Name.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                c.Text.Trim().Equals("UPDATE", StringComparison.OrdinalIgnoreCase));
            if (btnUpdate != null)
            {
                btnUpdate.Cursor = Cursors.Hand;
                btnUpdate.Click += async (s, e) => await LoadPendingSubmissionsAsync();
            }

            // Make "ACCEPT SUBMISSION" and "REJECT SUBMISSION" labels clickable too
            var lblAccept = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("ACCEPT SUBMISSION", StringComparison.OrdinalIgnoreCase));
            if (lblAccept != null)
            {
                lblAccept.Cursor = Cursors.Hand;
                lblAccept.Click += async (s, e) => await AcceptCurrentSubmissionAsync();
            }

            var lblReject = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("REJECT SUBMISSION", StringComparison.OrdinalIgnoreCase));
            if (lblReject != null)
            {
                lblReject.Cursor = Cursors.Hand;
                lblReject.Click += async (s, e) => await RejectCurrentSubmissionAsync();
            }

            // Status & Keybind Bar inside the bottom dark-red bar (below the UPDATE button)
            _lblBottomStatus = new Label
            {
                Left = 48,
                Top = Math.Max(10, this.ClientSize.Height - 44),
                Width = Math.Max(300, this.ClientSize.Width - 96),
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "KEYBINDS: [← / →] SWITCH PROOF   •   [K] ACCEPT & COMPLETE QUEST   •   [L] REJECT SUBMISSION"
            };
            this.Controls.Add(_lblBottomStatus);
            _lblBottomStatus.BringToFront();
        }

        private static bool IsAncestorOf(Control candidateAncestor, Control? target)
        {
            if (target == null) return false;
            Control? current = target.Parent;
            while (current != null)
            {
                if (current == candidateAncestor) return true;
                current = current.Parent;
            }
            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!_isProcessing)
            {
                if (keyData == Keys.Left)
                {
                    NavigateSubmission(-1);
                    return true;
                }
                if (keyData == Keys.Right)
                {
                    NavigateSubmission(1);
                    return true;
                }
                if (keyData == Keys.K)
                {
                    _ = AcceptCurrentSubmissionAsync();
                    return true;
                }
                if (keyData == Keys.L)
                {
                    _ = RejectCurrentSubmissionAsync();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async Task LoadPendingSubmissionsAsync()
        {
            _pendingSubmissions.Clear();
            _currentIndex = 0;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        ps.SubmissionID,
                        ps.MembershipID,
                        COALESCE(NULLIF(ps.ImagePath, ''), ir.FilePath) AS ResolvedImagePath,
                        ps.SubmittedAt,
                        qm.QuestID,
                        qm.WorkerID,
                        cw.UserID,
                        u.BarangayIDNumber,
                        u.FullName,
                        q.Title AS QuestTitle,
                        q.PointsReward,
                        q.EndDate
                    FROM Proof_Submissions ps
                    LEFT JOIN Image_Registry ir ON ps.ProofImageID = ir.ImageID
                    INNER JOIN Quest_Memberships qm ON ps.MembershipID = qm.MembershipID
                    INNER JOIN Citizen_Worker cw ON qm.WorkerID = cw.WorkerID
                    INNER JOIN Users u ON cw.UserID = u.UserID
                    INNER JOIN Quests q ON qm.QuestID = q.QuestID
                    WHERE ps.Status = 'Pending'
                    ORDER BY ps.SubmittedAt ASC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    _pendingSubmissions.Add(new PendingSubmissionItem
                    {
                        SubmissionID = Convert.ToInt32(reader["SubmissionID"]),
                        MembershipID = Convert.ToInt32(reader["MembershipID"]),
                        QuestID = Convert.ToInt32(reader["QuestID"]),
                        WorkerID = Convert.ToInt32(reader["WorkerID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        BarangayIDNumber = reader["BarangayIDNumber"]?.ToString() ?? "",
                        WorkerName = reader["FullName"]?.ToString() ?? "",
                        QuestTitle = reader["QuestTitle"]?.ToString() ?? "",
                        PointsReward = Convert.ToInt32(reader["PointsReward"]),
                        ImagePath = reader["ResolvedImagePath"]?.ToString() ?? "",
                        SubmittedAt = Convert.ToDateTime(reader["SubmittedAt"]),
                        QuestDeadline = Convert.ToDateTime(reader["EndDate"])
                    });
                }

                RefreshTableAndActiveSubmission();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading pending work submissions:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTableAndActiveSubmission()
        {
            if (_pendingSubmissions.Count == 0)
            {
                _dgvSubmissions?.Rows.Clear();
                ClearProofImage("NO PENDING WORK SUBMISSIONS\nAll submitted proofs have been reviewed.");

                if (_lblBottomStatus != null)
                {
                    _lblBottomStatus.Text = "QUEUE CLEAR • 0 PENDING SUBMISSIONS REMAINING";
                }
                return;
            }

            if (_currentIndex >= _pendingSubmissions.Count)
                _currentIndex = _pendingSubmissions.Count - 1;
            if (_currentIndex < 0)
                _currentIndex = 0;

            DisplaySubmissionAt(_currentIndex);
        }

        private void NavigateSubmission(int step)
        {
            if (_pendingSubmissions.Count <= 1) return;
            int nextIdx = (_currentIndex + step + _pendingSubmissions.Count) % _pendingSubmissions.Count;
            DisplaySubmissionAt(nextIdx);
        }

        private void DisplaySubmissionAt(int index)
        {
            if (index < 0 || index >= _pendingSubmissions.Count) return;
            _currentIndex = index;
            var item = _pendingSubmissions[_currentIndex];

            if (_dgvSubmissions != null)
            {
                _dgvSubmissions.Rows.Clear();
                int rowIdx = _dgvSubmissions.Rows.Add(
                    item.BarangayIDNumber,
                    item.WorkerName,
                    item.QuestTitle,
                    item.SubmittedAt.ToString("MMM dd, yyyy hh:mm tt"),
                    item.QuestDeadline.ToString("MMM dd, yyyy hh:mm tt")
                );
                _dgvSubmissions.Rows[rowIdx].Selected = true;
            }

            LoadProofImageIntoWhitePanel(item.ImagePath);

            if (_lblBottomStatus != null)
            {
                _lblBottomStatus.Text =
                    $"REVIEWING SUBMISSION {_currentIndex + 1} OF {_pendingSubmissions.Count}   •   REWARD: +{item.PointsReward} PTS   •   [← / →] SWITCH   |   [K] ACCEPT   |   [L] REJECT";
            }
        }

        private void LoadProofImageIntoWhitePanel(string storedPath)
        {
            if (_picProofViewer == null || _lblViewerPlaceholder == null) return;

            if (_picProofViewer.Image != null)
            {
                var old = _picProofViewer.Image;
                _picProofViewer.Image = null;
                old.Dispose();
            }

            if (string.IsNullOrWhiteSpace(storedPath))
            {
                ClearProofImage("No proof image path attached to this submission.");
                return;
            }

            string? resolvedPath = ResolveProofFilePath(storedPath);
            if (resolvedPath == null)
            {
                ClearProofImage($"Proof image file not found on disk:\n{storedPath}");
                return;
            }

            try
            {
                byte[] imageBytes = File.ReadAllBytes(resolvedPath);
                using var ms = new MemoryStream(imageBytes);
                using var tempImg = Image.FromStream(ms);
                _picProofViewer.Image = new Bitmap(tempImg);

                _lblViewerPlaceholder.Visible = false;
                _picProofViewer.Visible = true;
                _picProofViewer.BringToFront();
            }
            catch (Exception ex)
            {
                ClearProofImage($"Unable to render image file:\n{ex.Message}");
            }
        }

        private static string? ResolveProofFilePath(string storedPath)
        {
            string cleaned = storedPath.Trim().Trim('"');
            if (File.Exists(cleaned)) return cleaned;

            string relative = cleaned.TrimStart('\\', '/');
            string fileName = Path.GetFileName(relative);

            string[] baseDirs =
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Application.StartupPath,
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."))
            };

            string[] subFolders = { "", "Proofs", "ProofImages", "Images", "Uploads", "Resources" };

            foreach (string baseDir in baseDirs)
            {
                if (!Directory.Exists(baseDir)) continue;

                string direct = Path.Combine(baseDir, relative);
                if (File.Exists(direct)) return direct;

                if (!string.IsNullOrEmpty(fileName))
                {
                    foreach (string sub in subFolders)
                    {
                        string candidate = string.IsNullOrEmpty(sub)
                            ? Path.Combine(baseDir, fileName)
                            : Path.Combine(baseDir, sub, fileName);
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }

            return null;
        }

        private void ClearProofImage(string message)
        {
            if (_picProofViewer != null)
            {
                if (_picProofViewer.Image != null)
                {
                    var old = _picProofViewer.Image;
                    _picProofViewer.Image = null;
                    old.Dispose();
                }
                if (_picProofViewer != _displayCanvas)
                {
                    _picProofViewer.Visible = false;
                }
            }

            if (_lblViewerPlaceholder != null)
            {
                _lblViewerPlaceholder.Text = message;
                _lblViewerPlaceholder.Visible = true;
                _lblViewerPlaceholder.BringToFront();
            }
        }

        private async Task AcceptCurrentSubmissionAsync()
        {
            if (_isProcessing || _pendingSubmissions.Count == 0) return;

            var item = _pendingSubmissions[_currentIndex];
            var confirm = MessageBox.Show(
                $"Approve proof of work for '{item.WorkerName}' on quest '{item.QuestTitle}'?\n\n" +
                $"• Credits +{item.PointsReward} PTS to {item.WorkerName}\n" +
                $"• Marks the quest COMPLETED and removes it from available quests",
                "Confirm Quest Approval (K)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _isProcessing = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                string approverName = string.IsNullOrWhiteSpace(DispatcherSession.FullName)
                    ? "LGU Dispatcher"
                    : DispatcherSession.FullName;

                string updProofSql = @"
                    UPDATE Proof_Submissions
                    SET Status = 'Approved',
                        AdminComments = @comments
                    WHERE SubmissionID = @subId;";
                using (var cmd1 = new MySqlCommand(updProofSql, conn, tx))
                {
                    cmd1.Parameters.AddWithValue("@comments", $"Verified & Approved by {approverName}");
                    cmd1.Parameters.AddWithValue("@subId", item.SubmissionID);
                    await cmd1.ExecuteNonQueryAsync();
                }

                string updMemberSql = @"
                    UPDATE Quest_Memberships
                    SET Status = 'Completed'
                    WHERE MembershipID = @memId;";
                using (var cmd2 = new MySqlCommand(updMemberSql, conn, tx))
                {
                    cmd2.Parameters.AddWithValue("@memId", item.MembershipID);
                    await cmd2.ExecuteNonQueryAsync();
                }

                string updQuestSql = @"
                    UPDATE Quests
                    SET Status = 'Completed'
                    WHERE QuestID = @questId;";
                using (var cmd3 = new MySqlCommand(updQuestSql, conn, tx))
                {
                    cmd3.Parameters.AddWithValue("@questId", item.QuestID);
                    await cmd3.ExecuteNonQueryAsync();
                }

                string updPointsSql = @"
                    UPDATE Users
                    SET Points = Points + @pts
                    WHERE UserID = @uid;";
                using (var cmd4 = new MySqlCommand(updPointsSql, conn, tx))
                {
                    cmd4.Parameters.AddWithValue("@pts", item.PointsReward);
                    cmd4.Parameters.AddWithValue("@uid", item.UserID);
                    await cmd4.ExecuteNonQueryAsync();
                }

                string rewardCode = $"BQ-{item.QuestID}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                string insCodeSql = @"
                    INSERT IGNORE INTO Generated_Codes (SubmissionID, WorkerID, RewardCode, PointsEarned, IsRedeemed)
                    VALUES (@subId, @workerId, @code, @pts, FALSE);";
                using (var cmd5 = new MySqlCommand(insCodeSql, conn, tx))
                {
                    cmd5.Parameters.AddWithValue("@subId", item.SubmissionID);
                    cmd5.Parameters.AddWithValue("@workerId", item.WorkerID);
                    cmd5.Parameters.AddWithValue("@code", rewardCode);
                    cmd5.Parameters.AddWithValue("@pts", item.PointsReward);
                    await cmd5.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                MessageBox.Show(
                    $"Submission Approved!\n\n" +
                    $"• {item.WorkerName} earned +{item.PointsReward} PTS\n" +
                    $"• Quest '{item.QuestTitle}' is now Completed and removed from available quests\n" +
                    $"• Verification Code: {rewardCode}",
                    "Quest Approved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _pendingSubmissions.RemoveAt(_currentIndex);
                RefreshTableAndActiveSubmission();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to approve submission:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task RejectCurrentSubmissionAsync()
        {
            if (_isProcessing || _pendingSubmissions.Count == 0) return;

            var item = _pendingSubmissions[_currentIndex];
            string? reason = PromptRejectionReason(item.WorkerName, item.QuestTitle);
            if (reason == null) return;

            _isProcessing = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string approverName = string.IsNullOrWhiteSpace(DispatcherSession.FullName)
                    ? "LGU Dispatcher"
                    : DispatcherSession.FullName;

                string sql = @"
                    UPDATE Proof_Submissions
                    SET Status = 'Rejected',
                        AdminComments = @comments
                    WHERE SubmissionID = @subId;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@comments", $"Rejected by {approverName}: {reason}");
                cmd.Parameters.AddWithValue("@subId", item.SubmissionID);
                await cmd.ExecuteNonQueryAsync();

                MessageBox.Show(
                    $"Submission from '{item.WorkerName}' has been rejected.",
                    "Submission Rejected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _pendingSubmissions.RemoveAt(_currentIndex);
                RefreshTableAndActiveSubmission();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reject submission:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private string? PromptRejectionReason(string workerName, string questTitle)
        {
            using var dlg = new Form
            {
                Text = "Reject Work Submission (L)",
                Size = new Size(420, 210),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 25, 12),
                ForeColor = Color.White
            };

            var lbl = new Label
            {
                Text = $"Reason for rejecting {workerName}'s proof ({questTitle}):",
                Left = 18,
                Top = 18,
                Width = 370,
                ForeColor = Color.Gold
            };

            var txtReason = new TextBox
            {
                Left = 18,
                Top = 46,
                Width = 368,
                Text = "Image unclear or task requirements incomplete",
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnConfirm = new Button
            {
                Text = "REJECT",
                Left = 220,
                Top = 95,
                Width = 85,
                Height = 34,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnCancel = new Button
            {
                Text = "CANCEL",
                Left = 312,
                Top = 95,
                Width = 74,
                Height = 34,
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnConfirm.Click += (s, e) => dlg.DialogResult = DialogResult.OK;
            btnCancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;

            dlg.Controls.AddRange(new Control[] { lbl, txtReason, btnConfirm, btnCancel });
            dlg.AcceptButton = btnConfirm;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                return string.IsNullOrWhiteSpace(txtReason.Text)
                    ? "Did not meet verification criteria"
                    : txtReason.Text.Trim();
            }

            return null;
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