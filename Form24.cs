using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form24 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        // Map & Overlay Layers (matching Form23)
        private GMapControl gmap = null!;
        private GMapOverlay targetOverlay = null!;
        private GMapOverlay markersOverlay = null!;
        private PointLatLng targetQuestLocation = new(14.6590, 120.9768);
        private bool isAuthorizedAdmin = false;

        // Right-side Admin Quest Creator HUD
        private Panel pnlCreatorHud = null!;
        private Label lblAdminBanner = null!;
        private Label lblMapInstruction = null!;
        private TextBox txtTitle = null!;
        private ComboBox cmbCategory = null!;
        private ComboBox cmbDifficulty = null!;
        private ComboBox cmbQuestMode = null!;
        private NumericUpDown numMaxCapacity = null!;
        private TextBox txtBaseStipend = null!;
        private NumericUpDown numPointsReward = null!;
        private DateTimePicker dtpStartDate = null!;
        private DateTimePicker dtpEndDate = null!;
        private ComboBox cmbStatus = null!;
        private TextBox txtLatitude = null!;
        private TextBox txtLongitude = null!;
        private ComboBox cmbAssignTool = null!;

        private Button btnCreateQuest = null!;
        private Button btnResetPin = null!;
        private Button btnBackDashboard = null!;

        private class QuestRecord
        {
            public int QuestID { get; set; }
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Difficulty { get; set; } = "";
            public string BasePay { get; set; } = "";
            public int PointsReward { get; set; }
            public double Longitude { get; set; }
            public double Latitude { get; set; }
            public int MaxCapacity { get; set; }
            public int CurrentJoinedCount { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Status { get; set; } = "";
            public string AssignedTools { get; set; } = "";
            public string QuestMode => MaxCapacity <= 1 ? "SOLO QUEST" : "GROUP QUEST";
        }

        private class ToolOption
        {
            public int? ToolID { get; set; }
            public string DisplayName { get; set; } = "";
            public override string ToString() => DisplayName;
        }

        public Form24()
        {
            InitializeComponent();
            Control hostContainer = ResolveInnerHostPanel();
            BuildAdminCreatorHud(hostContainer);
            InitializeAdminMap(hostContainer);
            this.Load += Form24_Load;
        }

        private async void Form24_Load(object? sender, EventArgs e)
        {
            if (!await VerifySuperAdminAccessAsync()) return;

            await EnsureStipendColumnSupportsTextAsync();
            await LoadAvailableToolsAsync();
            await LoadActiveQuestsOnMapAsync();
            UpdateTargetPin(targetQuestLocation, panToPin: false);
        }

        private async Task EnsureStipendColumnSupportsTextAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string checkSql = @"
                    SELECT DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Quests'
                      AND COLUMN_NAME = 'BasePay'
                    LIMIT 1;";

                using var checkCmd = new MySqlCommand(checkSql, conn);
                string dataType = (await checkCmd.ExecuteScalarAsync())?.ToString() ?? "";

                if (!dataType.Equals("varchar", StringComparison.OrdinalIgnoreCase) &&
                    !dataType.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    using var alterCmd = new MySqlCommand(
                        "ALTER TABLE Quests MODIFY COLUMN BasePay VARCHAR(150) NOT NULL DEFAULT 'None';", conn);
                    await alterCmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Ignore if already altered or permissions restrict ALTER
            }
        }

        private Control ResolveInnerHostPanel()
        {
            var namedPanel = this.Controls.Find("panel1", true).FirstOrDefault();
            if (namedPanel != null) return namedPanel;

            var panelsBySize = GetAllControls(this)
                .OfType<Panel>()
                .OrderByDescending(p => p.Width * p.Height)
                .ToList();

            if (panelsBySize.Count >= 2) return panelsBySize[1];
            if (panelsBySize.Count == 1) return panelsBySize[0];
            return this;
        }

        private async Task<bool> VerifySuperAdminAccessAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sessionName = UserSession.FullName?.Trim() ?? "";

                if (!string.IsNullOrEmpty(sessionName))
                {
                    string sql = "SELECT FullName, Role FROM Users WHERE FullName = @name LIMIT 1;";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", sessionName);
                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync() || !string.Equals(reader["Role"]?.ToString(), "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                    {
                        DenyNonAdminAccess();
                        return false;
                    }

                    lblAdminBanner.Text = $"ADMIN: {(reader["FullName"]?.ToString() ?? sessionName).ToUpperInvariant()}";
                }
                else
                {
                    string sql = "SELECT FullName FROM Users WHERE Role = 'SuperAdmin' ORDER BY UserID ASC LIMIT 1;";
                    using var cmd = new MySqlCommand(sql, conn);
                    object? adminObj = await cmd.ExecuteScalarAsync();

                    if (adminObj == null || adminObj == DBNull.Value)
                    {
                        DenyNonAdminAccess();
                        return false;
                    }

                    lblAdminBanner.Text = $"ADMIN: {adminObj.ToString()!.ToUpperInvariant()}";
                }

                isAuthorizedAdmin = true;
                btnCreateQuest.Enabled = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Security verification error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void DenyNonAdminAccess()
        {
            isAuthorizedAdmin = false;
            btnCreateQuest.Enabled = false;
            MessageBox.Show(
                "Access Denied: Only Super Admins can create or dispatch quests.",
                "Super Admin Only",
                MessageBoxButtons.OK,
                MessageBoxIcon.Stop);

            Form2 roleSelect = new Form2();
            roleSelect.Show();
            this.Hide();
        }

        // Builds the right-side Admin Quest Creator HUD matching Form23's style
        private void BuildAdminCreatorHud(Control hostContainer)
        {
            pnlCreatorHud = new Panel
            {
                Dock = DockStyle.Right,
                Width = 330,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(46, 24, 10)
            };

            btnBackDashboard = new Button
            {
                Text = "RETURN TO DASHBOARD",
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBackDashboard.Click += (s, e) =>
            {
                X dashboard = new X();
                dashboard.Show();
                this.Hide();
            };

            btnResetPin = new Button
            {
                Text = "RESET FORM & MAP PIN",
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Color.FromArgb(92, 58, 24),
                ForeColor = Color.Gold,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetPin.Click += (s, e) => ResetCreatorForm();

            btnCreateQuest = new Button
            {
                Text = "CREATE & DISPATCH QUEST",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnCreateQuest.Click += async (s, e) => await CreateQuestAsync();

            var formStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            lblAdminBanner = new Label
            {
                Text = "SUPER ADMIN QUEST CREATOR",
                Width = 285,
                Height = 22,
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            lblMapInstruction = new Label
            {
                Text = "Right-click anywhere on the map to pin the quest location. Left-click green pins to inspect existing quests.",
                Width = 285,
                Height = 38,
                ForeColor = Color.Khaki,
                Font = new Font("Segoe UI", 8.2F, FontStyle.Regular)
            };

            Label AddFieldLabel(string text)
            {
                var lbl = new Label
                {
                    Text = text,
                    Width = 285,
                    Height = 18,
                    ForeColor = Color.Gold,
                    Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
                    Margin = new Padding(0, 5, 0, 1)
                };
                formStack.Controls.Add(lbl);
                return lbl;
            }

            formStack.Controls.Add(lblAdminBanner);
            formStack.Controls.Add(lblMapInstruction);

            // Quest Title
            AddFieldLabel("QUEST TITLE *");
            txtTitle = new TextBox
            {
                Width = 285,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "e.g., Barangay Drainage Clearing"
            };
            formStack.Controls.Add(txtTitle);

            // Category & Difficulty Row
            AddFieldLabel("CATEGORY & DIFFICULTY");
            var rowCatDiff = new Panel { Width = 285, Height = 26, Margin = new Padding(0) };
            cmbCategory = new ComboBox
            {
                Left = 0,
                Top = 0,
                Width = 175,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbCategory.Items.AddRange(new object[]
            {
                "Sanitation",
                "Infrastructure",
                "Disaster Response",
                "Community Aid",
                "Environment",
                "Peace & Order"
            });
            cmbCategory.SelectedIndex = 0;

            cmbDifficulty = new ComboBox
            {
                Left = 181,
                Top = 0,
                Width = 104,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbDifficulty.Items.AddRange(new object[] { "Easy", "Medium", "Hard" });
            cmbDifficulty.SelectedIndex = 0;

            rowCatDiff.Controls.Add(cmbCategory);
            rowCatDiff.Controls.Add(cmbDifficulty);
            formStack.Controls.Add(rowCatDiff);

            // Quest Mode & Max Capacity Row
            AddFieldLabel("QUEST MODE & MAX CAPACITY");
            var rowModeCap = new Panel { Width = 285, Height = 26, Margin = new Padding(0) };
            cmbQuestMode = new ComboBox
            {
                Left = 0,
                Top = 0,
                Width = 195,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbQuestMode.Items.AddRange(new object[] { "SOLO QUEST (1 Worker)", "GROUP QUEST (Multi-Worker)" });
            cmbQuestMode.SelectedIndex = 0;

            numMaxCapacity = new NumericUpDown
            {
                Left = 201,
                Top = 1,
                Width = 84,
                Minimum = 1,
                Maximum = 100,
                Value = 1,
                Enabled = false,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White
            };

            cmbQuestMode.SelectedIndexChanged += (s, e) =>
            {
                bool isGroup = cmbQuestMode.SelectedIndex == 1;
                numMaxCapacity.Enabled = isGroup;
                numMaxCapacity.Value = isGroup ? Math.Max(2, numMaxCapacity.Value) : 1;
            };

            rowModeCap.Controls.Add(cmbQuestMode);
            rowModeCap.Controls.Add(numMaxCapacity);
            formStack.Controls.Add(rowModeCap);

            // Stipend (Money or Object) & Points Reward Row
            AddFieldLabel("STIPEND (CASH / ITEM) & POINTS");
            var rowRewards = new Panel { Width = 285, Height = 26, Margin = new Padding(0) };
            txtBaseStipend = new TextBox
            {
                Left = 0,
                Top = 0,
                Width = 175,
                Text = "PHP 150.00",
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "e.g., PHP 150 or 5kg Rice"
            };

            numPointsReward = new NumericUpDown
            {
                Left = 181,
                Top = 0,
                Width = 104,
                Minimum = 5,
                Maximum = 100000,
                Increment = 25,
                Value = 100,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White
            };

            rowRewards.Controls.Add(txtBaseStipend);
            rowRewards.Controls.Add(numPointsReward);
            formStack.Controls.Add(rowRewards);

            // Start Date & End Date
            AddFieldLabel("START DATE & DEADLINE");
            dtpStartDate = new DateTimePicker
            {
                Width = 285,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMM dd, yyyy hh:mm tt",
                Value = DateTime.Now
            };
            formStack.Controls.Add(dtpStartDate);

            dtpEndDate = new DateTimePicker
            {
                Width = 285,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMM dd, yyyy hh:mm tt",
                Value = DateTime.Now.AddDays(2),
                Margin = new Padding(0, 3, 0, 0)
            };
            formStack.Controls.Add(dtpEndDate);

            // Coordinates (Latitude = Coordinates_Y, Longitude = Coordinates_X)
            AddFieldLabel("PIN COORDINATES (LATITUDE / LONGITUDE)");
            var rowCoords = new Panel { Width = 285, Height = 26, Margin = new Padding(0) };
            txtLatitude = new TextBox
            {
                Left = 0,
                Top = 0,
                Width = 140,
                Text = targetQuestLocation.Lat.ToString("F6"),
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.LightSkyBlue,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtLongitude = new TextBox
            {
                Left = 146,
                Top = 0,
                Width = 139,
                Text = targetQuestLocation.Lng.ToString("F6"),
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.LightSkyBlue,
                BorderStyle = BorderStyle.FixedSingle
            };

            rowCoords.Controls.Add(txtLatitude);
            rowCoords.Controls.Add(txtLongitude);
            formStack.Controls.Add(rowCoords);

            // Initial Status & Optional Tool Assignment
            AddFieldLabel("STATUS & OPTIONAL TOOL ITEM");
            var rowStatusTool = new Panel { Width = 285, Height = 26, Margin = new Padding(0) };
            cmbStatus = new ComboBox
            {
                Left = 0,
                Top = 0,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbStatus.Items.AddRange(new object[] { "Active", "Upcoming" });
            cmbStatus.SelectedIndex = 0;

            cmbAssignTool = new ComboBox
            {
                Left = 106,
                Top = 0,
                Width = 179,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbAssignTool.Items.Add(new ToolOption { ToolID = null, DisplayName = "No Tool Assigned" });
            cmbAssignTool.SelectedIndex = 0;

            rowStatusTool.Controls.Add(cmbStatus);
            rowStatusTool.Controls.Add(cmbAssignTool);
            formStack.Controls.Add(rowStatusTool);

            pnlCreatorHud.Controls.Add(formStack);
            pnlCreatorHud.Controls.Add(btnCreateQuest);
            pnlCreatorHud.Controls.Add(btnResetPin);
            pnlCreatorHud.Controls.Add(btnBackDashboard);

            hostContainer.Controls.Add(pnlCreatorHud);
        }

        private void InitializeAdminMap(Control hostContainer)
        {
            GMapProvider.UserAgent = "BarangayQuest_AdminDispatcher";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.OpenStreetMap,
                Position = targetQuestLocation,
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = 18,
                DragButton = MouseButtons.Left
            };

            targetOverlay = new GMapOverlay("target_pin");
            markersOverlay = new GMapOverlay("active_quests");
            gmap.Overlays.Add(markersOverlay);
            gmap.Overlays.Add(targetOverlay);

            gmap.MouseClick += Gmap_MouseClick;
            gmap.OnMarkerClick += Gmap_OnMarkerClick;

            hostContainer.Controls.Add(gmap);
            gmap.BringToFront();
        }

        private void Gmap_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || gmap.IsMouseOverMarker) return;

            PointLatLng clickedCoords = gmap.FromLocalToLatLng(e.X, e.Y);
            UpdateTargetPin(clickedCoords, panToPin: false);
        }

        private void UpdateTargetPin(PointLatLng coords, bool panToPin)
        {
            targetQuestLocation = coords;
            txtLatitude.Text = coords.Lat.ToString("F6");
            txtLongitude.Text = coords.Lng.ToString("F6");

            targetOverlay.Markers.Clear();
            targetOverlay.Markers.Add(new GMarkerGoogle(coords, GMarkerGoogleType.red_pushpin)
            {
                ToolTipText = $"New Quest Drop Pin\nLat: {coords.Lat:F6}, Lng: {coords.Lng:F6}",
                ToolTipMode = MarkerTooltipMode.OnMouseOver
            });

            if (panToPin)
            {
                gmap.Position = coords;
            }
        }

        private async void Gmap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || item.Tag is not QuestRecord q) return;

            string toolText = string.IsNullOrWhiteSpace(q.AssignedTools) ? "None" : q.AssignedTools;
            string details =
                $"Quest #{q.QuestID}: {q.Title}\n" +
                $"Mode: {q.QuestMode} ({q.CurrentJoinedCount}/{q.MaxCapacity} joined)\n" +
                $"Category: {q.Category} | Difficulty: {q.Difficulty}\n" +
                $"Stipend / Reward: {q.BasePay} | Points: +{q.PointsReward} PTS\n" +
                $"Assigned Tool: {toolText}\n" +
                $"Schedule: {q.StartDate:MMM dd, yyyy} - {q.EndDate:MMM dd, yyyy hh:mm tt}\n" +
                $"Status: {q.Status}\n\n" +
                $"Do you want to CANCEL this quest?";

            var result = MessageBox.Show(details, "Inspect Dispatched Quest", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                await CancelQuestAsync(q.QuestID, q.Title);
            }
        }

        private async Task LoadAvailableToolsAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                cmbAssignTool.Items.Clear();
                cmbAssignTool.Items.Add(new ToolOption { ToolID = null, DisplayName = "No Tool Assigned" });

                string sql = "SELECT ToolID, ToolName FROM Tool_Items WHERE AssignedQuestID IS NULL ORDER BY ToolName ASC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    cmbAssignTool.Items.Add(new ToolOption
                    {
                        ToolID = reader.GetInt32("ToolID"),
                        DisplayName = reader.GetString("ToolName")
                    });
                }

                cmbAssignTool.SelectedIndex = 0;
            }
            catch
            {
                // Keep default "No Tool Assigned" if Tool_Items has no unassigned rows
            }
        }

        private async Task LoadActiveQuestsOnMapAsync()
        {
            markersOverlay.Markers.Clear();

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT q.*,
                           (SELECT GROUP_CONCAT(t.ToolName SEPARATOR ', ')
                            FROM Tool_Items t
                            WHERE t.AssignedQuestID = q.QuestID) AS AssignedTools
                    FROM Quests q
                    WHERE q.Status IN ('Upcoming', 'Active');";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                int count = 0;
                while (await reader.ReadAsync())
                {
                    var q = ReadQuest(reader);
                    var marker = new GMarkerGoogle(new PointLatLng(q.Latitude, q.Longitude), GMarkerGoogleType.green_dot)
                    {
                        ToolTipText = $"[{q.Status.ToUpper()}] {q.Title} ({q.QuestMode}) • {q.BasePay} • +{q.PointsReward} PTS",
                        ToolTipMode = MarkerTooltipMode.OnMouseOver,
                        Tag = q
                    };
                    markersOverlay.Markers.Add(marker);
                    count++;
                }

                lblMapInstruction.Text = $"Showing {count} active/upcoming quest pin(s).\nRight-click map to set new quest coordinates.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load map quests: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CreateQuestAsync()
        {
            if (!isAuthorizedAdmin)
            {
                DenyNonAdminAccess();
                return;
            }

            string title = txtTitle.Text.Trim();
            string category = cmbCategory.Text.Trim();
            string difficulty = cmbDifficulty.SelectedItem?.ToString() ?? "Easy";
            int maxCapacity = cmbQuestMode.SelectedIndex == 1 ? (int)numMaxCapacity.Value : 1;
            string stipendInput = NormalizeStipendInput(txtBaseStipend.Text);
            int pointsReward = (int)numPointsReward.Value;
            DateTime startDate = dtpStartDate.Value;
            DateTime endDate = dtpEndDate.Value;
            string status = cmbStatus.SelectedItem?.ToString() ?? "Active";

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a Quest Title.", "Missing Title", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                category = "Community Aid";
            }

            if (endDate <= startDate)
            {
                MessageBox.Show("Deadline (End Date) must be later than the Start Date.", "Invalid Schedule", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(txtLatitude.Text.Trim(), out double lat) ||
                !double.TryParse(txtLongitude.Text.Trim(), out double lng))
            {
                MessageBox.Show("Invalid Latitude or Longitude coordinates. Right-click the map to place a valid pin.", "Invalid Coordinates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnCreateQuest.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                try
                {
                    string insertSql = @"
                        INSERT INTO Quests
                            (Title, Category, Difficulty, BasePay, PointsReward, Coordinates_X, Coordinates_Y, MaxCapacity, CurrentJoinedCount, StartDate, EndDate, Status)
                        VALUES
                            (@title, @cat, @diff, @pay, @pts, @lng, @lat, @cap, 0, @start, @end, @status);
                        SELECT LAST_INSERT_ID();";

                    int newQuestId;
                    using (var cmd = new MySqlCommand(insertSql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@cat", category);
                        cmd.Parameters.AddWithValue("@diff", difficulty);
                        cmd.Parameters.AddWithValue("@pay", stipendInput);
                        cmd.Parameters.AddWithValue("@pts", pointsReward);
                        cmd.Parameters.AddWithValue("@lng", lng);
                        cmd.Parameters.AddWithValue("@lat", lat);
                        cmd.Parameters.AddWithValue("@cap", maxCapacity);
                        cmd.Parameters.AddWithValue("@start", startDate);
                        cmd.Parameters.AddWithValue("@end", endDate);
                        cmd.Parameters.AddWithValue("@status", status);

                        newQuestId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    if (cmbAssignTool.SelectedItem is ToolOption selectedTool && selectedTool.ToolID.HasValue)
                    {
                        string toolSql = "UPDATE Tool_Items SET AssignedQuestID = @qid WHERE ToolID = @tid;";
                        using var toolCmd = new MySqlCommand(toolSql, conn, tx);
                        toolCmd.Parameters.AddWithValue("@qid", newQuestId);
                        toolCmd.Parameters.AddWithValue("@tid", selectedTool.ToolID.Value);
                        await toolCmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();

                    MessageBox.Show($"Quest '{title}' has been created and pinned on the map!", "Quest Dispatched", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    txtTitle.Clear();
                    await LoadAvailableToolsAsync();
                    await LoadActiveQuestsOnMapAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create quest: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCreateQuest.Enabled = isAuthorizedAdmin;
            }
        }

        private async Task CancelQuestAsync(int questId, string title)
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand("UPDATE Quests SET Status = 'Cancelled' WHERE QuestID = @qid;", conn))
                {
                    cmd.Parameters.AddWithValue("@qid", questId);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var freeToolCmd = new MySqlCommand("UPDATE Tool_Items SET AssignedQuestID = NULL WHERE AssignedQuestID = @qid;", conn))
                {
                    freeToolCmd.Parameters.AddWithValue("@qid", questId);
                    await freeToolCmd.ExecuteNonQueryAsync();
                }

                MessageBox.Show($"Quest '{title}' has been cancelled.", "Quest Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadAvailableToolsAsync();
                await LoadActiveQuestsOnMapAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to cancel quest: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetCreatorForm()
        {
            txtTitle.Clear();
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            cmbDifficulty.SelectedIndex = 0;
            cmbQuestMode.SelectedIndex = 0;
            numMaxCapacity.Value = 1;
            numMaxCapacity.Enabled = false;
            txtBaseStipend.Text = "PHP 150.00";
            numPointsReward.Value = 100;
            dtpStartDate.Value = DateTime.Now;
            dtpEndDate.Value = DateTime.Now.AddDays(2);
            cmbStatus.SelectedIndex = 0;
            if (cmbAssignTool.Items.Count > 0) cmbAssignTool.SelectedIndex = 0;

            UpdateTargetPin(new PointLatLng(14.6590, 120.9768), panToPin: true);
        }

        private static string NormalizeStipendInput(string rawInput)
        {
            string trimmed = rawInput.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return "None";

            // If the admin types a plain number like "200", format it as PHP 200.00;
            // otherwise keep the exact item/reward description (e.g., "5kg Sack of Rice").
            if (decimal.TryParse(trimmed, out decimal cashAmount))
                return $"PHP {cashAmount:N2}";

            return trimmed;
        }

        private static QuestRecord ReadQuest(MySqlDataReader r)
        {
            string rawPay = r["BasePay"] == DBNull.Value ? "None" : r["BasePay"].ToString() ?? "None";
            return new QuestRecord
            {
                QuestID = r.GetInt32("QuestID"),
                Title = r.GetString("Title"),
                Category = r.GetString("Category"),
                Difficulty = r.GetString("Difficulty"),
                BasePay = NormalizeStipendInput(rawPay),
                PointsReward = r.GetInt32("PointsReward"),
                Longitude = r.GetDouble("Coordinates_X"),
                Latitude = r.GetDouble("Coordinates_Y"),
                MaxCapacity = r.GetInt32("MaxCapacity"),
                CurrentJoinedCount = r.GetInt32("CurrentJoinedCount"),
                StartDate = r.GetDateTime("StartDate"),
                EndDate = r.GetDateTime("EndDate"),
                Status = r.GetString("Status"),
                AssignedTools = r["AssignedTools"] == DBNull.Value ? "" : r["AssignedTools"].ToString() ?? ""
            };
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