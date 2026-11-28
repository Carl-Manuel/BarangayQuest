using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form27 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private sealed class ToolCardSlot
        {
            public int SlotIndex { get; set; }
            public string DefaultTitle { get; set; } = "";
            public int ToolID { get; set; }
            public string ToolName { get; set; } = "";
            public string ConditionStatus { get; set; } = "Available";
            public int? AssignedQuestID { get; set; }
            public string AssignedQuestTitle { get; set; } = "";

            public Control? CardContainer { get; set; }
            public Label? TitleLabel { get; set; }
            public Label? AvailabilityLabel { get; set; }
            public bool IsSelected { get; set; }
        }

        private static readonly string[] DefaultToolCatalog =
        {
            "BROOMSTICK AND DUSTPAN",
            "UMBRELLA / RAINCOAT",
            "HAMMER, PLIERS AND SCREWDRIVERS SET",
            "SHOVEL / RAKE",
            "REFLECTORIZED VEST",
            "BOLO / GRASS CUTTER AND PRUNING SHEARS",
            "WHEELBARROW",
            "WHISTLE",
            "PAINTBRUSH, ROLLER AND BUCKET"
        };

        private readonly List<ToolCardSlot> _slots = new();
        private bool _editMode = false;
        private bool _isBusy = false;

        private Control? _btnEditInventory;
        private Control? _btnUpdate;
        private Label? _lblStatusBanner;

        public Form27()
        {
            InitializeComponent();
            BindInventoryControls();
            _resizer = new FormResizer(this);

            this.Load += Form27_Load;
        }

        private async void Form27_Load(object? sender, EventArgs e)
        {
            await LoadToolInventoryAsync();
        }

        private void BindInventoryControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();

            // Match each of the 9 tool cards by its title label and pair it with its AVAILABILITY label
            var availLabels = allLabels
                .Where(l => l.Text.Trim().Equals("AVAILABILITY", StringComparison.OrdinalIgnoreCase))
                .ToList();

            for (int i = 0; i < DefaultToolCatalog.Length; i++)
            {
                string expectedTitle = DefaultToolCatalog[i];
                string normalizedExpected = NormalizeKey(expectedTitle);

                var titleLbl = allLabels.FirstOrDefault(l =>
                    NormalizeKey(l.Text).Contains(normalizedExpected) ||
                    normalizedExpected.Contains(NormalizeKey(l.Text)) && NormalizeKey(l.Text).Length >= 6);

                Label? pairedAvail = null;
                Control? cardContainer = null;

                if (titleLbl != null)
                {
                    Point titleScreen = titleLbl.PointToScreen(Point.Empty);

                    // Find the closest AVAILABILITY label below/right of this title label
                    pairedAvail = availLabels
                        .Select(a => new { Lbl = a, Pos = a.PointToScreen(Point.Empty) })
                        .Where(x => x.Pos.Y >= titleScreen.Y - 10 &&
                                    x.Pos.Y <= titleScreen.Y + 110 &&
                                    Math.Abs(x.Pos.X - titleScreen.X) <= 180)
                        .OrderBy(x => Math.Pow(x.Pos.X - titleScreen.X, 2) + Math.Pow(x.Pos.Y - titleScreen.Y, 2))
                        .Select(x => x.Lbl)
                        .FirstOrDefault();

                    if (pairedAvail != null)
                        availLabels.Remove(pairedAvail);

                    cardContainer = titleLbl.Parent != this ? titleLbl.Parent : titleLbl;
                }

                var slot = new ToolCardSlot
                {
                    SlotIndex = i,
                    DefaultTitle = expectedTitle,
                    ToolName = expectedTitle,
                    CardContainer = cardContainer,
                    TitleLabel = titleLbl,
                    AvailabilityLabel = pairedAvail
                };

                _slots.Add(slot);

                // Make the entire ticket card clickable to select/toggle availability
                if (cardContainer != null)
                {
                    WireCardClick(cardContainer, slot);
                }
                else
                {
                    if (titleLbl != null) WireCardClick(titleLbl, slot);
                    if (pairedAvail != null) WireCardClick(pairedAvail, slot);
                }
            }

            // Bind EDIT INVENTORY and UPDATE buttons (works for Button, Label, or PictureBox)
            _btnEditInventory = FindActionControl(allControls, "EDIT INVENTORY");
            _btnUpdate = FindActionControl(allControls, "UPDATE");

            if (_btnEditInventory != null)
            {
                _btnEditInventory.Cursor = Cursors.Hand;
                _btnEditInventory.Click += (s, e) => ToggleEditMode();
            }

            if (_btnUpdate != null)
            {
                _btnUpdate.Cursor = Cursors.Hand;
                _btnUpdate.Click += async (s, e) => await SaveInventoryUpdatesAsync();
            }

            // Wire Left Scrollbar Navigation Icons if present (Shield = SuperAdmin Dash, Wrench = Current, Gear = Users, Map = Quests)
            WireSidebarIcons(allControls);

            // Status bar at the bottom of the screen
            _lblStatusBanner = new Label
            {
                Left = 140,
                Top = Math.Max(10, this.ClientSize.Height - 32),
                Width = Math.Max(320, this.ClientSize.Width - 200),
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "TOOL INVENTORY • CLICK ANY TOOL CARD TO INSPECT OR CLICK 'EDIT INVENTORY' TO TOGGLE AVAILABILITY"
            };
            this.Controls.Add(_lblStatusBanner);
            _lblStatusBanner.BringToFront();
        }

        private void WireCardClick(Control root, ToolCardSlot slot)
        {
            root.Cursor = Cursors.Hand;
            root.Click += (s, e) => OnToolCardClicked(slot);

            if (root != this)
            {
                foreach (Control child in GetAllControls(root))
                {
                    child.Cursor = Cursors.Hand;
                    child.Click += (s, e) => OnToolCardClicked(slot);
                }
            }
        }

        private void OnToolCardClicked(ToolCardSlot slot)
        {
            foreach (var s in _slots)
                s.IsSelected = (s == slot);

            if (_editMode)
            {
                if (slot.AssignedQuestID.HasValue)
                {
                    MessageBox.Show(
                        $"'{slot.ToolName}' is currently assigned to active Quest #{slot.AssignedQuestID} ({slot.AssignedQuestTitle}).\n\n" +
                        "Complete or cancel that quest in Quest Management before marking this tool Available.",
                        "Tool In Use",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    slot.ConditionStatus = slot.ConditionStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)
                        ? "Unavailable"
                        : "Available";
                }
            }

            RenderAllSlots();

            if (_lblStatusBanner != null)
            {
                string usage = slot.AssignedQuestID.HasValue
                    ? $"ASSIGNED TO QUEST #{slot.AssignedQuestID} ({slot.AssignedQuestTitle})"
                    : slot.ConditionStatus.ToUpperInvariant();

                _lblStatusBanner.Text = _editMode
                    ? $"[EDIT MODE] {slot.ToolName} → {usage} • CLICK 'UPDATE' TO SAVE CHANGES"
                    : $"SELECTED: {slot.ToolName} • STATUS: {usage}";
            }
        }

        private void ToggleEditMode()
        {
            _editMode = !_editMode;

            if (_btnEditInventory != null)
            {
                _btnEditInventory.Text = _editMode ? "DONE EDITING" : "EDIT INVENTORY";
            }

            if (_lblStatusBanner != null)
            {
                _lblStatusBanner.ForeColor = _editMode ? Color.Gold : Color.Bisque;
                _lblStatusBanner.Text = _editMode
                    ? "EDIT MODE ACTIVE • CLICK ANY TOOL CARD TO TOGGLE AVAILABLE / UNAVAILABLE, THEN CLICK 'UPDATE'"
                    : "TOOL INVENTORY • CLICK 'EDIT INVENTORY' TO MODIFY AVAILABILITY";
            }

            RenderAllSlots();
        }

        private async Task LoadToolInventoryAsync()
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureToolSchemaAndSeedAsync(conn);

                string sql = @"
                    SELECT 
                        t.ToolID,
                        t.ToolName,
                        COALESCE(t.ConditionStatus, 'Available') AS ConditionStatus,
                        t.AssignedQuestID,
                        COALESCE(q.Title, '') AS QuestTitle
                    FROM Tool_Items t
                    LEFT JOIN Quests q ON t.AssignedQuestID = q.QuestID
                    ORDER BY t.ToolID ASC;";

                var dbTools = new List<(int ToolID, string Name, string Status, int? QuestId, string QuestTitle)>();

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        dbTools.Add((
                            Convert.ToInt32(reader["ToolID"]),
                            reader["ToolName"]?.ToString() ?? "",
                            reader["ConditionStatus"]?.ToString() ?? "Available",
                            reader["AssignedQuestID"] == DBNull.Value ? null : Convert.ToInt32(reader["AssignedQuestID"]),
                            reader["QuestTitle"]?.ToString() ?? ""
                        ));
                    }
                }

                // Pair database rows with the 9 visual cards
                for (int i = 0; i < _slots.Count; i++)
                {
                    var slot = _slots[i];
                    string normSlot = NormalizeKey(slot.DefaultTitle);

                    var match = dbTools.FirstOrDefault(t => NormalizeKey(t.Name) == normSlot);
                    if (match.ToolID == 0 && i < dbTools.Count)
                    {
                        match = dbTools[i];
                    }

                    if (match.ToolID > 0)
                    {
                        slot.ToolID = match.ToolID;
                        slot.ToolName = match.Name;
                        slot.ConditionStatus = match.Status;
                        slot.AssignedQuestID = match.QuestId;
                        slot.AssignedQuestTitle = match.QuestTitle;
                    }
                }

                RenderAllSlots();

                int availCount = _slots.Count(s =>
                    !s.AssignedQuestID.HasValue &&
                    s.ConditionStatus.Equals("Available", StringComparison.OrdinalIgnoreCase));

                if (_lblStatusBanner != null)
                {
                    _lblStatusBanner.Text = $"INVENTORY SYNCED • {availCount} OF {_slots.Count} TOOLS AVAILABLE FOR DISPATCH";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tool inventory:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task EnsureToolSchemaAndSeedAsync(MySqlConnection conn)
        {
            string createSql = @"
                CREATE TABLE IF NOT EXISTS Tool_Items (
                    ToolID INT AUTO_INCREMENT PRIMARY KEY,
                    ToolName VARCHAR(120) NOT NULL,
                    ConditionStatus VARCHAR(30) NOT NULL DEFAULT 'Available',
                    AssignedQuestID INT NULL
                );";
            using (var createCmd = new MySqlCommand(createSql, conn))
            {
                await createCmd.ExecuteNonQueryAsync();
            }

            // Ensure ConditionStatus column exists if Tool_Items was created earlier without it
            string colCheckSql = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'Tool_Items'
                  AND COLUMN_NAME = 'ConditionStatus';";
            using (var colCmd = new MySqlCommand(colCheckSql, conn))
            {
                long hasCol = Convert.ToInt64(await colCmd.ExecuteScalarAsync());
                if (hasCol == 0)
                {
                    using var alterCmd = new MySqlCommand(
                        "ALTER TABLE Tool_Items ADD COLUMN ConditionStatus VARCHAR(30) NOT NULL DEFAULT 'Available';", conn);
                    await alterCmd.ExecuteNonQueryAsync();
                }
            }

            // Seed the 9 barangay tools if Tool_Items is currently empty
            using var countCmd = new MySqlCommand("SELECT COUNT(*) FROM Tool_Items;", conn);
            long existingCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
            if (existingCount == 0)
            {
                foreach (string toolName in DefaultToolCatalog)
                {
                    using var insCmd = new MySqlCommand(
                        "INSERT INTO Tool_Items (ToolName, ConditionStatus, AssignedQuestID) VALUES (@name, 'Available', NULL);", conn);
                    insCmd.Parameters.AddWithValue("@name", toolName);
                    await insCmd.ExecuteNonQueryAsync();
                }
            }
        }

        private void RenderAllSlots()
        {
            foreach (var slot in _slots)
            {
                bool inUse = slot.AssignedQuestID.HasValue;
                bool isAvailable = !inUse && slot.ConditionStatus.Equals("Available", StringComparison.OrdinalIgnoreCase);

                if (slot.AvailabilityLabel != null)
                {
                    slot.AvailabilityLabel.AutoSize = false;
                    slot.AvailabilityLabel.TextAlign = ContentAlignment.MiddleCenter;
                    slot.AvailabilityLabel.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);

                    if (inUse)
                    {
                        slot.AvailabilityLabel.Text = $"IN USE (Q#{slot.AssignedQuestID})";
                        slot.AvailabilityLabel.BackColor = Color.DarkGoldenrod;
                        slot.AvailabilityLabel.ForeColor = Color.White;
                    }
                    else if (isAvailable)
                    {
                        slot.AvailabilityLabel.Text = "AVAILABLE";
                        slot.AvailabilityLabel.BackColor = Color.SeaGreen;
                        slot.AvailabilityLabel.ForeColor = Color.White;
                    }
                    else
                    {
                        slot.AvailabilityLabel.Text = "UNAVAILABLE";
                        slot.AvailabilityLabel.BackColor = Color.Firebrick;
                        slot.AvailabilityLabel.ForeColor = Color.White;
                    }
                }

                if (slot.CardContainer != null && slot.CardContainer is Panel pnl)
                {
                    pnl.BorderStyle = slot.IsSelected ? BorderStyle.FixedSingle : BorderStyle.None;
                }
            }
        }

        private async Task SaveInventoryUpdatesAsync()
        {
            if (_isBusy) return;
            _isBusy = true;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureToolSchemaAndSeedAsync(conn);

                using var tx = await conn.BeginTransactionAsync();

                foreach (var slot in _slots)
                {
                    if (slot.ToolID > 0)
                    {
                        using var updCmd = new MySqlCommand(
                            "UPDATE Tool_Items SET ConditionStatus = @status WHERE ToolID = @tid;", conn, tx);
                        updCmd.Parameters.AddWithValue("@status", slot.ConditionStatus);
                        updCmd.Parameters.AddWithValue("@tid", slot.ToolID);
                        await updCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using var insCmd = new MySqlCommand(
                            "INSERT INTO Tool_Items (ToolName, ConditionStatus, AssignedQuestID) VALUES (@name, @status, NULL);", conn, tx);
                        insCmd.Parameters.AddWithValue("@name", slot.DefaultTitle);
                        insCmd.Parameters.AddWithValue("@status", slot.ConditionStatus);
                        await insCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                _editMode = false;
                if (_btnEditInventory != null)
                {
                    _btnEditInventory.Text = "EDIT INVENTORY";
                }

                await LoadToolInventoryAsync();

                MessageBox.Show(
                    "Tool inventory availability has been updated in the database!",
                    "Inventory Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update tool inventory:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void WireSidebarIcons(List<Control> allControls)
        {
            // Find the 4 icon PictureBoxes on the left scroll panel sorted top-to-bottom
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
                // Icon 0 (Shield): Super Admin Dashboard (Form16)
                leftIcons[0].Cursor = Cursors.Hand;
                leftIcons[0].Click += (s, e) => NavigateTo(new Form16());

                // Icon 2 (Gear): User Management (Form14)
                leftIcons[2].Cursor = Cursors.Hand;
                leftIcons[2].Click += (s, e) => NavigateTo(new Form14());

                // Icon 3 (Map): Super Admin Quest Creator (Form24)
                leftIcons[3].Cursor = Cursors.Hand;
                leftIcons[3].Click += (s, e) => NavigateTo(new Form24());
            }
        }

        private static Control? FindActionControl(List<Control> allControls, string text)
        {
            return allControls.FirstOrDefault(c =>
                !(c is Form) &&
                c.Text.Trim().Equals(text, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeKey(string input)
        {
            return new string(input
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
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

        private void goodsinventory_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form15());
        }
    }
}