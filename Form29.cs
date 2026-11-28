using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form29 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private TextBox? _txtGoodsCode;
        private TextBox? _txtToolCode;
        private Label? _lblStatusBanner;
        private bool _isBusy = false;

        public Form29()
        {
            InitializeComponent();
            BindPortalControls();
            _resizer = new FormResizer(this);
        }

        private void BindPortalControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();

            // Bind TextBoxes inside the dark-maroon slots for CODE OF GOODS: and CODE OF TOOL:
            var lblGoodsHeader = allLabels.FirstOrDefault(l =>
                l.Text.Trim().StartsWith("CODE OF GOODS", StringComparison.OrdinalIgnoreCase));
            var lblToolHeader = allLabels.FirstOrDefault(l =>
                l.Text.Trim().StartsWith("CODE OF TOOL", StringComparison.OrdinalIgnoreCase));

            _txtGoodsCode = FindOrCreateSlotTextBox(allControls, lblGoodsHeader, "Enter Shop Voucher or Quest Reward Code...");
            _txtToolCode = FindOrCreateSlotTextBox(allControls, lblToolHeader, "Enter Tool Asset Tag, Tool ID, or Checkout Code...");

            if (_txtGoodsCode != null)
            {
                _txtGoodsCode.CharacterCasing = CharacterCasing.Upper;
                _txtGoodsCode.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        goodscheckcode_Click(s!, EventArgs.Empty);
                    }
                };
            }

            if (_txtToolCode != null)
            {
                _txtToolCode.CharacterCasing = CharacterCasing.Upper;
                _txtToolCode.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        toolcheckcode_Click(s!, EventArgs.Empty);
                    }
                };
            }

            //  Embed status label inside the bottom dark-maroon bar
            var bottomMaroonBar = allControls
                .OfType<Panel>()
                .Where(p => p.Width > this.ClientSize.Width * 0.6 &&
                            p.Height >= 24 && p.Height <= 70 &&
                            p.Top > this.ClientSize.Height * 0.72)
                .OrderByDescending(p => p.Top)
                .FirstOrDefault();

            _lblStatusBanner = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "CODE VERIFIER PORTAL • ENTER A GOODS VOUCHER CODE OR BORROWED TOOL CODE TO VERIFY"
            };

            if (bottomMaroonBar != null)
            {
                bottomMaroonBar.Controls.Add(_lblStatusBanner);
                _lblStatusBanner.BringToFront();
            }

            // Wire Left Scrollbar Navigation Icons (Shield = Admin Dash, Wrench = Inventory, Gear = Users, Map = Quests)
            WireSidebarIcons(allControls);
        }

        private TextBox? FindOrCreateSlotTextBox(List<Control> allControls, Label? headerLabel, string placeholder)
        {
            if (headerLabel == null) return null;

            // Header label may be inside an orange badge panel inside the card panel
            Control cardContainer = headerLabel.Parent != null && headerLabel.Parent.Parent != null && headerLabel.Parent != this
                ? headerLabel.Parent.Parent
                : (headerLabel.Parent ?? this);

            Point headerScreen = headerLabel.PointToScreen(Point.Empty);

            // Check if a TextBox already exists below the header inside the card
            var existingTxt = allControls
                .OfType<TextBox>()
                .Select(t => new { Box = t, Pos = t.PointToScreen(Point.Empty) })
                .Where(x => Math.Abs(x.Pos.X - headerScreen.X) <= 60 &&
                            x.Pos.Y >= headerScreen.Y + 15 &&
                            x.Pos.Y <= headerScreen.Y + 110)
                .OrderBy(x => x.Pos.Y)
                .Select(x => x.Box)
                .FirstOrDefault();

            if (existingTxt != null)
            {
                existingTxt.ForeColor = Color.White;
                existingTxt.Font = new Font("Consolas", 12f, FontStyle.Bold);
                existingTxt.PlaceholderText = placeholder;
                return existingTxt;
            }

            // Locate the dark-maroon input strip panel below the header inside the card
            var slotPanel = cardContainer.Controls
                .OfType<Panel>()
                .Where(p => p != headerLabel.Parent &&
                            p.Height >= 22 && p.Height <= 58 &&
                            p.Width >= 180 &&
                            p.PointToScreen(Point.Empty).Y > headerScreen.Y)
                .OrderBy(p => p.Top)
                .FirstOrDefault();

            if (slotPanel != null)
            {
                var txtInSlot = new TextBox
                {
                    Left = 10,
                    Top = Math.Max(4, (slotPanel.Height - 24) / 2),
                    Width = Math.Max(140, slotPanel.Width - 20),
                    BackColor = slotPanel.BackColor,
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Consolas", 12f, FontStyle.Bold),
                    TextAlign = HorizontalAlignment.Center,
                    PlaceholderText = placeholder
                };
                slotPanel.Controls.Add(txtInSlot);
                txtInSlot.BringToFront();
                return txtInSlot;
            }

            // Fallback TextBox placed directly inside the card container
            var fallback = new TextBox
            {
                Left = 24,
                Top = 74,
                Width = Math.Max(180, cardContainer.Width - 48),
                BackColor = Color.FromArgb(62, 12, 12),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                PlaceholderText = placeholder
            };
            cardContainer.Controls.Add(fallback);
            fallback.BringToFront();
            return fallback;
        }

        private void SetStatus(string message, Color color)
        {
            if (_lblStatusBanner == null) return;
            _lblStatusBanner.Text = message;
            _lblStatusBanner.ForeColor = color;
        }

        // =========================================================================
        //  CHECK CODE OF GOODS (Redemption Shop Vouchers OR Quest Reward Codes)
        // =========================================================================
        private async void goodscheckcode_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            string code = _txtGoodsCode?.Text.Trim().ToUpperInvariant() ?? "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("PLEASE ENTER A GOODS VOUCHER CODE OR QUEST REWARD CODE FIRST.", Color.Gold);
                _txtGoodsCode?.Focus();
                return;
            }

            _isBusy = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                //  Check Shop_Redemptions (Redemption Shop Voucher Codes from Form28)
                bool hasShopRedemptions = await TableExistsAsync(conn, "Shop_Redemptions");
                if (hasShopRedemptions)
                {
                    string shopSql = @"
                        SELECT sr.RedemptionID, sr.ItemName, sr.PointsSpent, sr.Status, sr.RedeemedAt,
                               u.FullName, u.BarangayIDNumber
                        FROM Shop_Redemptions sr
                        INNER JOIN Users u ON sr.UserID = u.UserID
                        WHERE UPPER(sr.VoucherCode) = @code
                        LIMIT 1;";

                    using var cmd = new MySqlCommand(shopSql, conn);
                    cmd.Parameters.AddWithValue("@code", code);
                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        int redemptionId = Convert.ToInt32(r["RedemptionID"]);
                        string itemName = r["ItemName"]?.ToString() ?? "";
                        int ptsSpent = Convert.ToInt32(r["PointsSpent"]);
                        string status = r["Status"]?.ToString() ?? "Active";
                        string citizenName = r["FullName"]?.ToString() ?? "";
                        string brgyId = r["BarangayIDNumber"]?.ToString() ?? "";
                        DateTime redeemedAt = Convert.ToDateTime(r["RedeemedAt"]);
                        await r.CloseAsync();

                        if (status.Equals("Claimed", StringComparison.OrdinalIgnoreCase))
                        {
                            SetStatus($"ALREADY CLAIMED: '{itemName}' FOR {citizenName.ToUpperInvariant()} ({brgyId})", Color.OrangeRed);
                            MessageBox.Show(
                                $"This shop voucher code ({code}) has ALREADY BEEN CLAIMED.\n\n" +
                                $"Item: {itemName}\nCitizen: {citizenName} ({brgyId})\nRedeemed On: {redeemedAt:MMM dd, yyyy hh:mm tt}",
                                "Voucher Already Claimed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        SetStatus($"VALID SHOP VOUCHER: {itemName.ToUpperInvariant()} • CITIZEN: {citizenName.ToUpperInvariant()}", Color.LightGreen);

                        var confirm = MessageBox.Show(
                            $"VALID REDEMPTION SHOP VOUCHER!\n\n" +
                            $"Voucher Code: {code}\n" +
                            $"Item to Release: {itemName}\n" +
                            $"Points Paid: {ptsSpent} PTS\n" +
                            $"Citizen: {citizenName} ({brgyId})\n\n" +
                            "Mark this voucher as CLAIMED and release goods now?",
                            "Verify & Claim Goods Voucher",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (confirm == DialogResult.Yes)
                        {
                            using var upd = new MySqlCommand(
                                "UPDATE Shop_Redemptions SET Status = 'Claimed' WHERE RedemptionID = @id;", conn);
                            upd.Parameters.AddWithValue("@id", redemptionId);
                            await upd.ExecuteNonQueryAsync();

                            SetStatus($"CLAIMED & RELEASED: {itemName.ToUpperInvariant()} TO {citizenName.ToUpperInvariant()} ({code})", Color.LightGreen);
                            _txtGoodsCode?.Clear();
                            MessageBox.Show($"Voucher {code} marked as CLAIMED. Goods released to {citizenName}.", "Goods Released", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        return;
                    }
                }

                //  Check Generated_Codes (Reward Codes earned from Completing Quests)
                string questCodeSql = @"
                    SELECT gc.CodeID, gc.RewardCode, gc.PointsEarned, gc.IsRedeemed, gc.GeneratedAt,
                           u.FullName, u.BarangayIDNumber, q.Title AS QuestTitle
                    FROM Generated_Codes gc
                    INNER JOIN Citizen_Worker cw ON gc.WorkerID = cw.WorkerID
                    INNER JOIN Users u ON cw.UserID = u.UserID
                    INNER JOIN Proof_Submissions ps ON gc.SubmissionID = ps.SubmissionID
                    INNER JOIN Quest_Memberships qm ON ps.MembershipID = qm.MembershipID
                    INNER JOIN Quests q ON qm.QuestID = q.QuestID
                    WHERE UPPER(gc.RewardCode) = @code
                    LIMIT 1;";

                using (var qCmd = new MySqlCommand(questCodeSql, conn))
                {
                    qCmd.Parameters.AddWithValue("@code", code);
                    using var qr = await qCmd.ExecuteReaderAsync();
                    if (await qr.ReadAsync())
                    {
                        int codeId = Convert.ToInt32(qr["CodeID"]);
                        int ptsEarned = Convert.ToInt32(qr["PointsEarned"]);
                        bool isRedeemed = Convert.ToBoolean(qr["IsRedeemed"]);
                        string citizenName = qr["FullName"]?.ToString() ?? "";
                        string brgyId = qr["BarangayIDNumber"]?.ToString() ?? "";
                        string questTitle = qr["QuestTitle"]?.ToString() ?? "";
                        await qr.CloseAsync();

                        if (isRedeemed)
                        {
                            SetStatus($"ALREADY VERIFIED: QUEST REWARD CODE '{code}' ({citizenName.ToUpperInvariant()})", Color.OrangeRed);
                            MessageBox.Show(
                                $"Quest Reward Code '{code}' was already verified and claimed.\n\n" +
                                $"Quest: {questTitle}\nCitizen: {citizenName} ({brgyId})\nPoints Earned: {ptsEarned} PTS",
                                "Code Already Redeemed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        SetStatus($"VALID QUEST REWARD CODE: {ptsEarned} PTS • QUEST: {questTitle.ToUpperInvariant()}", Color.LightGreen);

                        var confirm = MessageBox.Show(
                            $"VALID QUEST COMPLETION CODE!\n\n" +
                            $"Reward Code: {code}\n" +
                            $"Completed Quest: {questTitle}\n" +
                            $"Citizen: {citizenName} ({brgyId})\n" +
                            $"Points Earned: {ptsEarned} PTS\n\n" +
                            "Mark this reward code as REDEEMED / VERIFIED?",
                            "Verify Quest Reward Code",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (confirm == DialogResult.Yes)
                        {
                            using var upd = new MySqlCommand(
                                "UPDATE Generated_Codes SET IsRedeemed = TRUE WHERE CodeID = @cid;", conn);
                            upd.Parameters.AddWithValue("@cid", codeId);
                            await upd.ExecuteNonQueryAsync();

                            SetStatus($"VERIFIED QUEST REWARD: {code} ({citizenName.ToUpperInvariant()} • {ptsEarned} PTS)", Color.LightGreen);
                            _txtGoodsCode?.Clear();
                            MessageBox.Show($"Quest Reward Code {code} verified and marked as redeemed!", "Code Verified", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        return;
                    }
                }

                //  Not found in either table
                SetStatus($"INVALID CODE: '{code}' NOT FOUND IN SHOP VOUCHERS OR QUEST REWARDS", Color.Salmon);
                MessageBox.Show(
                    $"Code '{code}' was not found in Redemption Shop vouchers or Quest Reward codes.",
                    "Invalid Code",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying goods code:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        // =========================================================================
        //  CHECK CODE OF TOOL (Checkout for Active Quest OR Return on Completion)
        // =========================================================================
        private async void toolcheckcode_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            string toolInput = _txtToolCode?.Text.Trim().ToUpperInvariant() ?? "";
            if (string.IsNullOrWhiteSpace(toolInput))
            {
                SetStatus("PLEASE ENTER A TOOL ASSET TAG, TOOL ID, OR CHECKOUT CODE FIRST.", Color.Gold);
                _txtToolCode?.Focus();
                return;
            }

            _isBusy = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureToolCheckoutColumnsAsync(conn);

                // Match by AssetTagID, ToolID (numeric or TOOL-X), or CheckoutCode
                int numericId = 0;
                string cleanedDigits = new string(toolInput.Where(char.IsDigit).ToArray());
                if (toolInput.All(char.IsDigit) || toolInput.StartsWith("TOOL", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(cleanedDigits, out numericId);
                }

                string findToolSql = @"
                    SELECT 
                        t.ToolID,
                        COALESCE(t.AssetTagID, CONCAT('TOOL-', t.ToolID)) AS AssetTagID,
                        t.ToolName,
                        COALESCE(t.ConditionStatus, 'Available') AS ConditionStatus,
                        t.AssignedQuestID,
                        t.WorkerCheckedOutID,
                        q.Title AS QuestTitle,
                        q.Status AS QuestStatus,
                        u.FullName AS BorrowerName,
                        u.BarangayIDNumber AS BorrowerBrgyId
                    FROM Tool_Items t
                    LEFT JOIN Quests q ON t.AssignedQuestID = q.QuestID
                    LEFT JOIN Citizen_Worker cw ON t.WorkerCheckedOutID = cw.WorkerID
                    LEFT JOIN Users u ON cw.UserID = u.UserID
                    WHERE UPPER(COALESCE(t.AssetTagID, '')) = @code
                       OR UPPER(COALESCE(t.CheckoutCode, '')) = @code
                       OR (@tid > 0 AND t.ToolID = @tid)
                    LIMIT 1;";

                using var cmd = new MySqlCommand(findToolSql, conn);
                cmd.Parameters.AddWithValue("@code", toolInput);
                cmd.Parameters.AddWithValue("@tid", numericId);

                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                {
                    SetStatus($"TOOL NOT FOUND: NO TOOL MATCHES '{toolInput}'", Color.Salmon);
                    MessageBox.Show(
                        $"No tool item found matching '{toolInput}'.\n\nEnter a valid Tool Asset Tag (e.g., TOOL-001), Tool ID, or Checkout Code.",
                        "Tool Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                int toolId = Convert.ToInt32(r["ToolID"]);
                string assetTag = r["AssetTagID"]?.ToString() ?? $"TOOL-{toolId}";
                string toolName = r["ToolName"]?.ToString() ?? "";
                string condition = r["ConditionStatus"]?.ToString() ?? "Available";
                int? assignedQuestId = r["AssignedQuestID"] == DBNull.Value ? null : Convert.ToInt32(r["AssignedQuestID"]);
                int? workerCheckedOutId = r["WorkerCheckedOutID"] == DBNull.Value ? null : Convert.ToInt32(r["WorkerCheckedOutID"]);
                string questTitle = r["QuestTitle"]?.ToString() ?? "";
                string questStatus = r["QuestStatus"]?.ToString() ?? "";
                string borrowerName = r["BorrowerName"]?.ToString() ?? "Assigned Worker";
                string borrowerBrgyId = r["BorrowerBrgyId"]?.ToString() ?? "";
                await r.CloseAsync();

                // CASE A: Tool is currently checked out / assigned to a quest -> Verify & Return to Inventory
                if (assignedQuestId.HasValue || workerCheckedOutId.HasValue)
                {
                    bool questFinished = questStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                         questStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);

                    string statusHeader = questFinished
                        ? $"QUEST '{questTitle}' IS {questStatus.ToUpperInvariant()} — TOOL MUST BE RETURNED TO INVENTORY!"
                        : $"TOOL CURRENTLY CHECKED OUT TO QUEST #{assignedQuestId} ({questTitle})";

                    SetStatus(statusHeader, questFinished ? Color.Gold : Color.DeepSkyBlue);

                    string promptMsg = questFinished
                        ? $"BORROWED TOOL RETURN VERIFICATION\n\n" +
                          $"Tool: {toolName} ({assetTag})\n" +
                          $"Quest: #{assignedQuestId} - {questTitle} [Status: {questStatus}]\n" +
                          $"Borrower: {borrowerName} ({borrowerBrgyId})\n\n" +
                          "Since the quest is completed/ended, this tool must be turned back in.\n" +
                          "Confirm tool return and mark AVAILABLE in inventory?"
                        : $"BORROWED TOOL RECORD MATCHED\n\n" +
                          $"Tool: {toolName} ({assetTag})\n" +
                          $"Assigned Quest: #{assignedQuestId} - {questTitle} [Status: {questStatus}]\n" +
                          $"Checked Out By: {borrowerName} ({borrowerBrgyId})\n\n" +
                          "Is the citizen returning this borrowed tool to inventory right now?";

                    var returnConfirm = MessageBox.Show(
                        promptMsg,
                        "Verify Borrowed Tool",
                        MessageBoxButtons.YesNo,
                        questFinished ? MessageBoxIcon.Warning : MessageBoxIcon.Question);

                    if (returnConfirm == DialogResult.Yes)
                    {
                        using var retCmd = new MySqlCommand(
                            @"UPDATE Tool_Items
                              SET AssignedQuestID = NULL,
                                  WorkerCheckedOutID = NULL,
                                  CheckoutCode = NULL,
                                  ConditionStatus = 'Available'
                              WHERE ToolID = @tid;", conn);
                        retCmd.Parameters.AddWithValue("@tid", toolId);
                        await retCmd.ExecuteNonQueryAsync();

                        SetStatus($"RETURNED TO INVENTORY: {toolName.ToUpperInvariant()} ({assetTag}) IS NOW AVAILABLE", Color.LightGreen);
                        _txtToolCode?.Clear();
                        MessageBox.Show(
                            $"Tool '{toolName}' ({assetTag}) has been checked back in and marked AVAILABLE in inventory!",
                            "Tool Returned",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    return;
                }

                // CASE B: Tool is currently Available in inventory -> Offer Checkout to an Active/Upcoming Quest Member
                if (condition.Equals("Damaged", StringComparison.OrdinalIgnoreCase) ||
                    condition.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus($"TOOL UNAVAILABLE: {toolName.ToUpperInvariant()} ({assetTag}) IS MARKED '{condition.ToUpperInvariant()}'", Color.OrangeRed);
                    MessageBox.Show(
                        $"Tool '{toolName}' ({assetTag}) is currently marked as '{condition}' and cannot be checked out.",
                        "Tool Unavailable",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                SetStatus($"TOOL AVAILABLE: {toolName.ToUpperInvariant()} ({assetTag}) • READY FOR QUEST CHECKOUT", Color.LightGreen);

                var checkoutConfirm = MessageBox.Show(
                    $"TOOL IS AVAILABLE IN INVENTORY\n\n" +
                    $"Tool: {toolName} ({assetTag})\n" +
                    $"Condition: {condition}\n\n" +
                    "Do you want to CHECK OUT this tool to a citizen currently assigned to an Active/Upcoming quest?",
                    "Checkout Tool for Quest",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (checkoutConfirm == DialogResult.Yes)
                {
                    await PromptAndCheckoutToolAsync(conn, toolId, assetTag, toolName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying tool code:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private sealed class ActiveMembershipOption
        {
            public int QuestID { get; set; }
            public int WorkerID { get; set; }
            public string QuestTitle { get; set; } = "";
            public string WorkerName { get; set; } = "";
            public string BarangayID { get; set; } = "";
            public override string ToString() => $"[Quest #{QuestID}] {QuestTitle} — {WorkerName} ({BarangayID})";
        }

        private async Task PromptAndCheckoutToolAsync(MySqlConnection conn, int toolId, string assetTag, string toolName)
        {
            var options = new List<ActiveMembershipOption>();

            string sql = @"
                SELECT qm.QuestID, qm.WorkerID, q.Title AS QuestTitle, u.FullName, u.BarangayIDNumber
                FROM Quest_Memberships qm
                INNER JOIN Quests q ON qm.QuestID = q.QuestID
                INNER JOIN Citizen_Worker cw ON qm.WorkerID = cw.WorkerID
                INNER JOIN Users u ON cw.UserID = u.UserID
                WHERE qm.Status = 'Joined'
                  AND q.Status IN ('Active', 'Upcoming')
                ORDER BY q.QuestID DESC, u.FullName ASC;";

            using (var cmd = new MySqlCommand(sql, conn))
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    options.Add(new ActiveMembershipOption
                    {
                        QuestID = Convert.ToInt32(r["QuestID"]),
                        WorkerID = Convert.ToInt32(r["WorkerID"]),
                        QuestTitle = r["QuestTitle"]?.ToString() ?? "",
                        WorkerName = r["FullName"]?.ToString() ?? "",
                        BarangayID = r["BarangayIDNumber"]?.ToString() ?? ""
                    });
                }
            }

            if (options.Count == 0)
            {
                MessageBox.Show(
                    "There are currently no citizens joined in an Active or Upcoming quest to check this tool out to.",
                    "No Active Quest Borrowers",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dlg = new Form
            {
                Text = $"Checkout {toolName} ({assetTag})",
                Size = new Size(470, 225),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(46, 26, 12)
            };

            var lblPrompt = new Label
            {
                Left = 18,
                Top = 18,
                Width = 420,
                Height = 24,
                Text = "Select the Quest & Citizen Borrower checking out this tool:",
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var cmb = new ComboBox
            {
                Left = 18,
                Top = 50,
                Width = 418,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            foreach (var opt in options) cmb.Items.Add(opt);
            cmb.SelectedIndex = 0;

            var btnConfirm = new Button
            {
                Left = 18,
                Top = 105,
                Width = 200,
                Height = 38,
                Text = "CONFIRM CHECKOUT",
                BackColor = Color.FromArgb(62, 168, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Left = 236,
                Top = 105,
                Width = 200,
                Height = 38,
                Text = "CANCEL",
                BackColor = Color.FromArgb(145, 42, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };

            dlg.Controls.Add(lblPrompt);
            dlg.Controls.Add(cmb);
            dlg.Controls.Add(btnConfirm);
            dlg.Controls.Add(btnCancel);

            if (dlg.ShowDialog(this) != DialogResult.OK || cmb.SelectedItem is not ActiveMembershipOption chosen)
                return;

            string checkoutRef = $"CHK-Q{chosen.QuestID}-T{toolId}";

            using var updCmd = new MySqlCommand(
                @"UPDATE Tool_Items
                  SET AssignedQuestID = @qid,
                      WorkerCheckedOutID = @wid,
                      CheckoutCode = @chk
                  WHERE ToolID = @tid;", conn);
            updCmd.Parameters.AddWithValue("@qid", chosen.QuestID);
            updCmd.Parameters.AddWithValue("@wid", chosen.WorkerID);
            updCmd.Parameters.AddWithValue("@chk", checkoutRef);
            updCmd.Parameters.AddWithValue("@tid", toolId);
            await updCmd.ExecuteNonQueryAsync();

            SetStatus($"CHECKED OUT: {toolName.ToUpperInvariant()} ({assetTag}) → {chosen.WorkerName.ToUpperInvariant()} (QUEST #{chosen.QuestID})", Color.DeepSkyBlue);
            _txtToolCode?.Clear();

            MessageBox.Show(
                $"Tool Checked Out Successfully!\n\n" +
                $"Tool: {toolName} ({assetTag})\n" +
                $"Checkout Code: {checkoutRef}\n" +
                $"Quest: #{chosen.QuestID} - {chosen.QuestTitle}\n" +
                $"Borrower: {chosen.WorkerName} ({chosen.BarangayID})\n\n" +
                "Reminder: This tool must be returned to inventory once the quest is completed.",
                "Tool Checked Out",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static async Task EnsureToolCheckoutColumnsAsync(MySqlConnection conn)
        {
            string[] alterQueries =
            {
                "ALTER TABLE Tool_Items ADD COLUMN AssetTagID VARCHAR(50) NULL;",
                "ALTER TABLE Tool_Items ADD COLUMN AssignedQuestID INT NULL;",
                "ALTER TABLE Tool_Items ADD COLUMN WorkerCheckedOutID INT NULL;",
                "ALTER TABLE Tool_Items ADD COLUMN CheckoutCode VARCHAR(40) NULL;"
            };

            foreach (string sql in alterQueries)
            {
                try
                {
                    using var cmd = new MySqlCommand(sql, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Column already exists
                }
            }
        }

        private static async Task<bool> TableExistsAsync(MySqlConnection conn, string tableName)
        {
            using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl;", conn);
            cmd.Parameters.AddWithValue("@tbl", tableName);
            return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
        }

        private void WireSidebarIcons(List<Control> allControls)
        {
            var leftIcons = allControls
                .OfType<PictureBox>()
                .Select(p => new { Box = p, Pos = p.PointToScreen(Point.Empty) })
                .Where(x => x.Pos.X - this.PointToScreen(Point.Empty).X < 140 &&
                            x.Box.Width >= 28 && x.Box.Width <= 80 &&
                            x.Box.Height >= 28 && x.Box.Height <= 80)
                .OrderBy(x => x.Pos.Y)
                .Select(x => x.Box)
                .ToList();

            if (leftIcons.Count >= 4)
            {
                // Icon 0 (Shield): Super Admin / Dispatcher Dashboard
                leftIcons[0].Cursor = Cursors.Hand;
                leftIcons[0].Click += (s, e) => NavigateTo(new Form16());

                // Icon 1 (Wrench): Inventory Management (Form15)
                leftIcons[1].Cursor = Cursors.Hand;
                leftIcons[1].Click += (s, e) => NavigateTo(new Form15());

                // Icon 2 (Gear): User Management (Form14)
                leftIcons[2].Cursor = Cursors.Hand;
                leftIcons[2].Click += (s, e) => NavigateTo(new Form14());

                // Icon 3 (Map): Quest Creator / Map (Form24)
                leftIcons[3].Cursor = Cursors.Hand;
                leftIcons[3].Click += (s, e) => NavigateTo(new Form24());
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

        private void toolcheckcode_Click_Wrapper(object sender, EventArgs e) => toolcheckcode_Click(sender, e);
        private void goodscheckcode_Click_Wrapper(object sender, EventArgs e) => goodscheckcode_Click(sender, e);
    }
}