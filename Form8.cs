using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form8 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly List<ActiveQuestItem> activeQuests = new();

        private class ActiveQuestItem
        {
            public int MembershipID { get; set; }
            public int QuestID { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public string Category { get; set; } = "";
            public string Difficulty { get; set; } = "";
            public decimal BasePay { get; set; }
            public int PointsReward { get; set; }
            public string RewardSupply { get; set; } = "";
            public int MaxCapacity { get; set; }
            public int CurrentJoinedCount { get; set; }
            public DateTime EndDate { get; set; }
            public string RedeemedTools { get; set; } = "None";
            public string ProofStatus { get; set; } = ""; // "", "Pending", "Approved", "Rejected"
            public string ExistingProofPaths { get; set; } = "";
            public int PartyJoinedCount { get; set; }
            public int PartySubmittedCount { get; set; }
            public bool IsGroupQuest => MaxCapacity > 1;
        }

        public Form8()
        {
            InitializeComponent();
            this.Load += Form8_Load;
            WireEvents();
        }

        private void WireEvents()
        {
            btnSubmitProof1.Click += async (s, e) => await OpenProofDialogForSlotAsync(0);
            btnAbandonQuest1.Click += async (s, e) => await AbandonQuestForSlotAsync(0);

            btnSubmitProof2.Click += async (s, e) => await OpenProofDialogForSlotAsync(1);
            btnAbandonQuest2.Click += async (s, e) => await AbandonQuestForSlotAsync(1);

            btnRedeem.Click += (s, e) =>
            {
                // Navigate back to Dashboard (Form5) or your Points Shop form
                var dashboard = new Form5();
                dashboard.FormClosed += (snd, args) => this.Close();
                dashboard.Show();
                this.Hide();
            };

            WireOptionalNav("picNavProfile", () =>
            {
                var profile = new Form6();
                profile.FormClosed += (s, a) => this.Close();
                profile.Show();
                this.Hide();
            });

            WireOptionalNav("picNavMap", () =>
            {
                var map = new Form23();
                map.FormClosed += (s, a) => this.Close();
                map.Show();
                this.Hide();
            });

            WireOptionalNav("picNavQuests", () =>
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

        private async void Form8_Load(object? sender, EventArgs e)
        {
            await RefreshQuestListAsync();
        }

        private async Task RefreshQuestListAsync()
        {
            activeQuests.Clear();
            ClearCardSlot(1);
            ClearCardSlot(2);

            if (UserSession.UserID <= 0 || UserSession.WorkerID <= 0)
            {
                lblTotalPoints.Text = "0";
                return;
            }

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                //  Load Worker Points
                using (var ptsCmd = new MySqlCommand("SELECT COALESCE(Points, 0) FROM Users WHERE UserID = @uid LIMIT 1;", conn))
                {
                    ptsCmd.Parameters.AddWithValue("@uid", UserSession.UserID);
                    object? ptsObj = await ptsCmd.ExecuteScalarAsync();
                    int pts = ptsObj != null && ptsObj != DBNull.Value ? Convert.ToInt32(ptsObj) : 0;
                    UserSession.Points = pts;
                    lblTotalPoints.Text = pts.ToString();
                }

                // Load Accepted Quests (Status = 'Joined') with Proof & Group Party Counts
                string sql = @"
                    SELECT 
                        qm.MembershipID,
                        q.*,
                        ps.Status AS ProofStatus,
                        ps.ImagePath AS ProofPaths,
                        (SELECT GROUP_CONCAT(DISTINCT t.ToolName SEPARATOR ', ')
                         FROM Tool_Items t
                         WHERE t.WorkerCheckedOutID = @workerId OR t.AssignedQuestID = q.QuestID) AS ToolsList,
                        (SELECT COUNT(*)
                         FROM Quest_Memberships qm2
                         WHERE qm2.QuestID = q.QuestID AND qm2.Status = 'Joined') AS PartyJoined,
                        (SELECT COUNT(*)
                         FROM Quest_Memberships qm3
                         INNER JOIN Proof_Submissions ps3 ON ps3.MembershipID = qm3.MembershipID
                         WHERE qm3.QuestID = q.QuestID AND qm3.Status = 'Joined') AS PartySubmitted
                    FROM Quest_Memberships qm
                    INNER JOIN Quests q ON qm.QuestID = q.QuestID
                    LEFT JOIN Proof_Submissions ps ON ps.MembershipID = qm.MembershipID
                    WHERE qm.WorkerID = @workerId
                      AND qm.Status = 'Joined'
                      AND q.Status IN ('Upcoming', 'Active')
                    ORDER BY qm.JoinedAt DESC
                    LIMIT 2;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string rawDesc = HasColumn(reader, "Description") && reader["Description"] != DBNull.Value
                            ? reader["Description"].ToString() ?? ""
                            : "";
                        string category = reader["Category"]?.ToString() ?? "";
                        string difficulty = reader["Difficulty"]?.ToString() ?? "";

                        string supply = HasColumn(reader, "RewardSupply") && reader["RewardSupply"] != DBNull.Value
                            ? reader["RewardSupply"].ToString() ?? ""
                            : "";
                        decimal basePay = reader["BasePay"] != DBNull.Value ? Convert.ToDecimal(reader["BasePay"]) : 0m;

                        if (string.IsNullOrWhiteSpace(supply))
                            supply = basePay > 0 ? $"PHP {basePay:N2}" : "None";

                        string tools = reader["ToolsList"] != DBNull.Value ? reader["ToolsList"].ToString() ?? "" : "";

                        activeQuests.Add(new ActiveQuestItem
                        {
                            MembershipID = Convert.ToInt32(reader["MembershipID"]),
                            QuestID = Convert.ToInt32(reader["QuestID"]),
                            Title = reader["Title"]?.ToString() ?? "Untitled Quest",
                            Description = string.IsNullOrWhiteSpace(rawDesc) ? $"Category: {category}" : rawDesc,
                            Category = category,
                            Difficulty = difficulty,
                            BasePay = basePay,
                            PointsReward = reader["PointsReward"] != DBNull.Value ? Convert.ToInt32(reader["PointsReward"]) : 0,
                            RewardSupply = supply,
                            MaxCapacity = Convert.ToInt32(reader["MaxCapacity"]),
                            CurrentJoinedCount = Convert.ToInt32(reader["CurrentJoinedCount"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            RedeemedTools = string.IsNullOrWhiteSpace(tools) ? "None" : tools,
                            ProofStatus = reader["ProofStatus"] != DBNull.Value ? reader["ProofStatus"].ToString() ?? "" : "",
                            ExistingProofPaths = reader["ProofPaths"] != DBNull.Value ? reader["ProofPaths"].ToString() ?? "" : "",
                            PartyJoinedCount = Convert.ToInt32(reader["PartyJoined"]),
                            PartySubmittedCount = Convert.ToInt32(reader["PartySubmitted"])
                        });
                    }
                }

                if (activeQuests.Count >= 1) BindQuestToCard(1, activeQuests[0]);
                if (activeQuests.Count >= 2) BindQuestToCard(2, activeQuests[1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading active quests: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearCardSlot(int slotNumber)
        {
            if (slotNumber == 1)
            {
                lblQuestTitle1.Text = "NO ACTIVE QUEST";
                lblQuestDifficulty1.Text = "";
                lblQuestType1.Text = "Accept a quest from the Quest Map to begin.";
                lblQuestDesc1.Text = "No active quest assigned to this slot.";
                lblQuestDeadline1.Text = "DEADLINE: NONE";
                lblRewardPoints1.Text = "0 PTS";
                lblRewardSupply1.Text = "None";
                lblRedeemedTools1.Text = "None";
                btnSubmitProof1.Text = "";
                btnSubmitProof1.Enabled = false;
                btnAbandonQuest1.Enabled = false;
            }
            else
            {
                lblQuestTitle2.Text = "NO ACTIVE QUEST";
                lblQuestDifficulty2.Text = "";
                lblQuestType2.Text = "Accept a quest from the Quest Map to begin.";
                lblQuestDesc2.Text = "No active quest assigned to this slot.";
                lblQuestDeadline2.Text = "DEADLINE: NONE";
                lblRewardPoints2.Text = "0 PTS";
                lblRewardSupply2.Text = "None";
                lblRedeemedTools2.Text = "None";
                btnSubmitProof2.Text = "";
                btnSubmitProof2.Enabled = false;
                btnAbandonQuest2.Enabled = false;
            }
        }

        private void BindQuestToCard(int slotNumber, ActiveQuestItem q)
        {
            string typeText = q.IsGroupQuest
                ? $"GROUP QUEST ({q.CurrentJoinedCount}/{q.MaxCapacity} JOINED  |  PROOFS: {q.PartySubmittedCount}/{q.PartyJoinedCount})"
                : "SOLO QUEST";

            string deadlineText = $"DEADLINE: {q.EndDate:MMM dd, yyyy hh:mm tt}";
            string pointsText = $"+{q.PointsReward} PTS";

            string submitBtnText = "SUBMIT PROOF OF WORK";
            bool canAbandon = true;

            if (q.ProofStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                submitBtnText = "APPROVED BY LGU";
                canAbandon = false;
            }
            else if (q.ProofStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                submitBtnText = q.IsGroupQuest && q.PartySubmittedCount < q.PartyJoinedCount
                    ? $"WAITING PARTY ({q.PartySubmittedCount}/{q.PartyJoinedCount})"
                    : "SENT TO LGU (EDIT)";
            }
            else if (q.ProofStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                submitBtnText = "RESUBMIT PROOF";
            }

            if (slotNumber == 1)
            {
                lblQuestTitle1.Text = q.Title.ToUpper();
                lblQuestDifficulty1.Text = q.Difficulty.ToUpper();
                lblQuestType1.Text = typeText;
                lblQuestDesc1.Text = q.Description;
                lblQuestDeadline1.Text = deadlineText;
                lblRewardPoints1.Text = pointsText;
                lblRewardSupply1.Text = q.RewardSupply;
                lblRedeemedTools1.Text = q.RedeemedTools;
                btnSubmitProof1.Text = submitBtnText;
                btnSubmitProof1.Enabled = !q.ProofStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                btnAbandonQuest1.Enabled = canAbandon;
            }
            else
            {
                lblQuestTitle2.Text = q.Title.ToUpper();
                lblQuestDifficulty2.Text = q.Difficulty.ToUpper();
                lblQuestType2.Text = typeText;
                lblQuestDesc2.Text = q.Description;
                lblQuestDeadline2.Text = deadlineText;
                lblRewardPoints2.Text = pointsText;
                lblRewardSupply2.Text = q.RewardSupply;
                lblRedeemedTools2.Text = q.RedeemedTools;
                btnSubmitProof2.Text = submitBtnText;
                btnSubmitProof2.Enabled = !q.ProofStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                btnAbandonQuest2.Enabled = canAbandon;
            }
        }

        private async Task OpenProofDialogForSlotAsync(int index)
        {
            if (index < 0 || index >= activeQuests.Count) return;
            var quest = activeQuests[index];

            using var dialog = new ProofSubmissionDialog(
                connString,
                quest.MembershipID,
                quest.QuestID,
                quest.Title,
                quest.IsGroupQuest,
                quest.PartyJoinedCount,
                quest.PartySubmittedCount,
                quest.ExistingProofPaths
            );

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                await RefreshQuestListAsync();
            }
        }

        private async Task AbandonQuestForSlotAsync(int index)
        {
            if (index < 0 || index >= activeQuests.Count) return;
            var quest = activeQuests[index];

            string warningMsg = quest.IsGroupQuest
                ? $"Abandon Group Quest '{quest.Title}'?\n\nYour spot will open up ({Math.Max(0, quest.CurrentJoinedCount - 1)}/{quest.MaxCapacity}) for other Citizen Workers and any unapproved proof will be removed."
                : $"Abandon Solo Quest '{quest.Title}'?\n\nThis will remove the quest from your active list.";

            var confirm = MessageBox.Show(warningMsg, "Confirm Abandon Quest", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                try
                {
                    // Delete any unapproved proof submission for this membership
                    using (var delProofCmd = new MySqlCommand(
                        "DELETE FROM Proof_Submissions WHERE MembershipID = @mid AND Status <> 'Approved';", conn, tx))
                    {
                        delProofCmd.Parameters.AddWithValue("@mid", quest.MembershipID);
                        await delProofCmd.ExecuteNonQueryAsync();
                    }

                    // Mark membership as 'Left'
                    using (var leaveCmd = new MySqlCommand(
                        "UPDATE Quest_Memberships SET Status = 'Left' WHERE MembershipID = @mid AND Status = 'Joined';", conn, tx))
                    {
                        leaveCmd.Parameters.AddWithValue("@mid", quest.MembershipID);
                        int affected = await leaveCmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            throw new InvalidOperationException("This quest membership is no longer active.");
                    }

                    // Free up 1 slot on the Quest (e.g., 5/5 -> 4/5)
                    using (var slotCmd = new MySqlCommand(
                        "UPDATE Quests SET CurrentJoinedCount = GREATEST(0, CurrentJoinedCount - 1) WHERE QuestID = @qid;", conn, tx))
                    {
                        slotCmd.Parameters.AddWithValue("@qid", quest.QuestID);
                        await slotCmd.ExecuteNonQueryAsync();
                    }

                    // Release any tools checked out by this worker for this quest
                    using (var toolCmd = new MySqlCommand(
                        "UPDATE Tool_Items SET WorkerCheckedOutID = NULL WHERE WorkerCheckedOutID = @wid AND AssignedQuestID = @qid;", conn, tx))
                    {
                        toolCmd.Parameters.AddWithValue("@wid", UserSession.WorkerID);
                        toolCmd.Parameters.AddWithValue("@qid", quest.QuestID);
                        await toolCmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    MessageBox.Show($"You have abandoned '{quest.Title}'.", "Quest Abandoned", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await RefreshQuestListAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to abandon quest: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void btnRedeem_Click(object sender, EventArgs e)
        {

        }
    }

    // Built-in Proof of Work Attachment & Group Verification Modal
    public class ProofSubmissionDialog : Form
    {
        private readonly string connString;
        private readonly int membershipId;
        private readonly int questId;
        private readonly string questTitle;
        private readonly bool isGroupQuest;
        private readonly int partyJoinedCount;
        private readonly int partySubmittedCount;

        private readonly List<string> attachedFilePaths = new();
        private readonly ListBox lstFiles;
        private readonly Label lblPartyRule;
        private readonly Button btnAddFiles;
        private readonly Button btnRemoveFile;
        private readonly Button btnConfirmSubmit;
        private readonly Button btnCancel;

        public ProofSubmissionDialog(
            string connString,
            int membershipId,
            int questId,
            string questTitle,
            bool isGroupQuest,
            int partyJoinedCount,
            int partySubmittedCount,
            string existingPaths)
        {
            this.connString = connString;
            this.membershipId = membershipId;
            this.questId = questId;
            this.questTitle = questTitle;
            this.isGroupQuest = isGroupQuest;
            this.partyJoinedCount = partyJoinedCount;
            this.partySubmittedCount = partySubmittedCount;

            this.Text = "Submit Proof of Work";
            this.Size = new Size(540, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(46, 24, 10);
            this.ForeColor = Color.White;

            var lblHeader = new Label
            {
                Text = $"PROOF OF WORK: {questTitle.ToUpper()}",
                Location = new Point(18, 15),
                Size = new Size(490, 26),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.Gold
            };

            lblPartyRule = new Label
            {
                Location = new Point(18, 44),
                Size = new Size(490, 42),
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = Color.Khaki,
                Text = isGroupQuest
                    ? $"GROUP QUEST RULE: Every joined member must submit individual proof ({partySubmittedCount}/{partyJoinedCount} submitted so far) before forwarding to the LGU Dispatcher for approval."
                    : "SOLO QUEST: Attach your proof-of-work images (.png, .jpg) or documents (.pdf) below to send directly to the LGU Dispatcher."
            };

            var lblListTitle = new Label
            {
                Text = "ATTACHED FILES (Double-click a file to preview):",
                Location = new Point(18, 92),
                Size = new Size(490, 20),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.Gainsboro
            };

            lstFiles = new ListBox
            {
                Location = new Point(18, 115),
                Size = new Size(490, 175),
                BackColor = Color.FromArgb(30, 15, 6),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };
            lstFiles.DoubleClick += (s, e) => PreviewSelectedFile();

            btnAddFiles = new Button
            {
                Text = "+ ATTACH FILES (.PNG, .JPG, .PDF)",
                Location = new Point(18, 300),
                Size = new Size(255, 34),
                BackColor = Color.FromArgb(92, 58, 24),
                ForeColor = Color.Gold,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddFiles.Click += (s, e) => BrowseFiles();

            btnRemoveFile = new Button
            {
                Text = "REMOVE SELECTED",
                Location = new Point(283, 300),
                Size = new Size(225, 34),
                BackColor = Color.FromArgb(110, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRemoveFile.Click += (s, e) => RemoveSelectedFile();

            btnConfirmSubmit = new Button
            {
                Text = "SUBMIT PROOF OF WORK",
                Location = new Point(18, 346),
                Size = new Size(340, 38),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirmSubmit.Click += async (s, e) => await SubmitProofAsync();

            btnCancel = new Button
            {
                Text = "CANCEL",
                Location = new Point(368, 346),
                Size = new Size(140, 38),
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[]
            {
                lblHeader, lblPartyRule, lblListTitle, lstFiles,
                btnAddFiles, btnRemoveFile, btnConfirmSubmit, btnCancel
            });

            LoadExistingAttachments(existingPaths);
        }

        private void LoadExistingAttachments(string existingPaths)
        {
            if (string.IsNullOrWhiteSpace(existingPaths)) return;

            foreach (string relPath in existingPaths.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string fullPath = Path.IsPathRooted(relPath)
                    ? relPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relPath);

                attachedFilePaths.Add(fullPath);
                lstFiles.Items.Add($"{Path.GetFileName(fullPath)}  ({relPath})");
            }
        }

        private void BrowseFiles()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Proof of Work Files",
                Filter = "Proof Files (*.png;*.jpg;*.jpeg;*.pdf;*.doc;*.docx)|*.png;*.jpg;*.jpeg;*.pdf;*.doc;*.docx|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            foreach (string file in ofd.FileNames)
            {
                if (!attachedFilePaths.Contains(file, StringComparer.OrdinalIgnoreCase))
                {
                    attachedFilePaths.Add(file);
                    long kb = Math.Max(1, new FileInfo(file).Length / 1024);
                    lstFiles.Items.Add($"{Path.GetFileName(file)}  ({kb} KB)");
                }
            }
        }

        private void RemoveSelectedFile()
        {
            int idx = lstFiles.SelectedIndex;
            if (idx < 0 || idx >= attachedFilePaths.Count) return;

            attachedFilePaths.RemoveAt(idx);
            lstFiles.Items.RemoveAt(idx);
        }

        private void PreviewSelectedFile()
        {
            int idx = lstFiles.SelectedIndex;
            if (idx < 0 || idx >= attachedFilePaths.Count) return;

            string path = attachedFilePaths[idx];
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("File not found on disk.", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task SubmitProofAsync()
        {
            if (attachedFilePaths.Count == 0)
            {
                MessageBox.Show("Please attach at least one file (.png, .jpg, or .pdf) before submitting.", "Attachment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConfirmSubmit.Enabled = false;

            try
            {
                // Copy files into local ProofUploads directory so paths stay valid
                string uploadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProofUploads");
                Directory.CreateDirectory(uploadDir);

                var storedRelativePaths = new List<string>();
                for (int i = 0; i < attachedFilePaths.Count; i++)
                {
                    string src = attachedFilePaths[i];
                    if (src.StartsWith(uploadDir, StringComparison.OrdinalIgnoreCase) && File.Exists(src))
                    {
                        storedRelativePaths.Add(Path.Combine("ProofUploads", Path.GetFileName(src)));
                        continue;
                    }

                    string safeName = $"m{membershipId}_{DateTime.Now:yyyyMMddHHmmss}_{i + 1}_{Path.GetFileName(src)}";
                    string destFull = Path.Combine(uploadDir, safeName);
                    File.Copy(src, destFull, overwrite: true);
                    storedRelativePaths.Add(Path.Combine("ProofUploads", safeName));
                }

                string combinedPaths = string.Join(";", storedRelativePaths);
                if (combinedPaths.Length > 250)
                    combinedPaths = combinedPaths.Substring(0, 250);

                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                try
                {
                    // Register primary file in Image_Registry
                    long imageId;
                    using (var imgCmd = new MySqlCommand(
                        "INSERT INTO Image_Registry (FilePath, EntityType) VALUES (@path, 'ProofOfWork'); SELECT LAST_INSERT_ID();", conn, tx))
                    {
                        imgCmd.Parameters.AddWithValue("@path", storedRelativePaths[0]);
                        imageId = Convert.ToInt64(await imgCmd.ExecuteScalarAsync());
                    }

                    // Insert or update Proof_Submissions for this worker's MembershipID
                    string upsertProofSql = @"
                        INSERT INTO Proof_Submissions (MembershipID, ProofImageID, ImagePath, Status, SubmittedAt)
                        VALUES (@mid, @imgId, @paths, 'Pending', NOW())
                        ON DUPLICATE KEY UPDATE
                            ProofImageID = VALUES(ProofImageID),
                            ImagePath = VALUES(ImagePath),
                            Status = 'Pending',
                            SubmittedAt = NOW();";

                    using (var proofCmd = new MySqlCommand(upsertProofSql, conn, tx))
                    {
                        proofCmd.Parameters.AddWithValue("@mid", membershipId);
                        proofCmd.Parameters.AddWithValue("@imgId", imageId);
                        proofCmd.Parameters.AddWithValue("@paths", combinedPaths);
                        await proofCmd.ExecuteNonQueryAsync();
                    }

                    //  Check Group Quest Party Submission Status
                    int totalJoined = 1;
                    int totalSubmitted = 1;

                    if (isGroupQuest)
                    {
                        using var countCmd = new MySqlCommand(@"
                            SELECT 
                                (SELECT COUNT(*) FROM Quest_Memberships WHERE QuestID = @qid AND Status = 'Joined') AS JoinedCnt,
                                (SELECT COUNT(*) FROM Quest_Memberships qm
                                 INNER JOIN Proof_Submissions ps ON ps.MembershipID = qm.MembershipID
                                 WHERE qm.QuestID = @qid AND qm.Status = 'Joined') AS SubCnt;", conn, tx);
                        countCmd.Parameters.AddWithValue("@qid", questId);

                        using var r = await countCmd.ExecuteReaderAsync();
                        if (await r.ReadAsync())
                        {
                            totalJoined = Convert.ToInt32(r["JoinedCnt"]);
                            totalSubmitted = Convert.ToInt32(r["SubCnt"]);
                        }
                    }

                    await tx.CommitAsync();

                    if (isGroupQuest && totalSubmitted < totalJoined)
                    {
                        MessageBox.Show(
                            $"Proof of work saved ({storedRelativePaths.Count} file(s))!\n\nGroup Quest Status: {totalSubmitted} of {totalJoined} party members have submitted.\nOnce all members submit their proof, the quest will be forwarded to the LGU Dispatcher for verification and point/code distribution.",
                            "Saved — Waiting for Party Members",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Proof of work submitted ({storedRelativePaths.Count} file(s))!\n\nAll required proofs are complete and forwarded to the LGU Dispatcher for verification and approval.",
                            "Forwarded to LGU Dispatcher",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                    this.DialogResult = DialogResult.OK;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to submit proof of work: {ex.Message}", "Submission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConfirmSubmit.Enabled = true;
            }
        }
    }
}