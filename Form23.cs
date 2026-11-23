using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form23 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private const double SearchRadiusMeters = 2500; // SET SEARCH RADIUS

        // Map & Overlay Layers
        private GMapControl gmap = null!;
        private GMapOverlay radiusOverlay = null!;
        private GMapOverlay markersOverlay = null!;
        private PointLatLng currentScanCenter = new(14.6590, 120.9768);
        private QuestRecord? selectedQuest;

        // Citizen Quest HUD Controls (docked inside panel1)
        private Panel pnlQuestHud = null!;
        private Label lblWorkerBanner = null!;
        private Label lblScanStatus = null!;
        private Label lblQuestTitle = null!;
        private Label lblQuestMeta = null!;
        private Label lblQuestRewards = null!;
        private Label lblQuestSlots = null!;
        private Label lblQuestSchedule = null!;
        private Button btnAcceptQuest = null!;
        private Button btnShowAllQuests = null!;
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

        public Form23()
        {
            InitializeComponent();
            BuildCitizenHud();
            InitializeCitizenMap();
        }

        private async void Form23_Load(object sender, EventArgs e)
        {
            await LoadNearbyQuestsAsync(currentScanCenter.Lat, currentScanCenter.Lng, SearchRadiusMeters);
        }

        // Builds the Citizen-only Quest Inspector HUD inside the right edge of the panel
        private void BuildCitizenHud()
        {
            pnlQuestHud = new Panel
            {
                Dock = DockStyle.Right,
                Width = 310,
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
                var dashboard = new Form5();
                dashboard.FormClosed += (snd, args) => this.Close();
                this.Hide();
                dashboard.Show();
            };

            btnShowAllQuests = new Button
            {
                Text = "SHOW ALL ACTIVE QUESTS",
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Color.FromArgb(92, 58, 24),
                ForeColor = Color.Gold,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnShowAllQuests.Click += async (s, e) => await LoadAllActiveQuestsAsync();

            btnAcceptQuest = new Button
            {
                Text = "ACCEPT QUEST",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnAcceptQuest.Click += async (s, e) => await AcceptSelectedQuestAsync();

            var infoStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            lblWorkerBanner = new Label
            {
                Text = string.IsNullOrWhiteSpace(UserSession.FullName)
                    ? $"CITIZEN QUEST RADAR ({SearchRadiusMeters / 1000.0:0.#} KM)"
                    : $"WORKER: {UserSession.FullName.ToUpper()}",
                Width = 280,
                Height = 24,
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            lblScanStatus = new Label
            {
                Text = $"Right-click anywhere on the map to set your location and scan a {SearchRadiusMeters / 1000.0:0.#} km radius.",
                Width = 280,
                Height = 42,
                ForeColor = Color.Khaki,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };

            lblQuestTitle = new Label
            {
                Text = "No Quest Selected",
                Width = 280,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(0, 8, 0, 4)
            };

            lblQuestMeta = new Label
            {
                Text = "Left-click any green pin on the map to inspect quest details.",
                Width = 280,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9F)
            };

            lblQuestRewards = new Label
            {
                Text = "",
                Width = 280,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(0, 8, 0, 4)
            };

            lblQuestSlots = new Label
            {
                Text = "",
                Width = 280,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 9F)
            };

            lblQuestSchedule = new Label
            {
                Text = "",
                Width = 280,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 8.5F),
                Padding = new Padding(0, 4, 0, 8)
            };

            infoStack.Controls.Add(lblWorkerBanner);
            infoStack.Controls.Add(lblScanStatus);
            infoStack.Controls.Add(lblQuestTitle);
            infoStack.Controls.Add(lblQuestMeta);
            infoStack.Controls.Add(lblQuestRewards);
            infoStack.Controls.Add(lblQuestSlots);
            infoStack.Controls.Add(lblQuestSchedule);

            pnlQuestHud.Controls.Add(infoStack);
            pnlQuestHud.Controls.Add(btnAcceptQuest);
            pnlQuestHud.Controls.Add(btnShowAllQuests);
            pnlQuestHud.Controls.Add(btnBackDashboard);

            panel1.Controls.Add(pnlQuestHud);
        }

        private void InitializeCitizenMap()
        {
            GMapProvider.UserAgent = "BarangayQuest_CitizenClient";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.OpenStreetMap,
                Position = currentScanCenter,
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = 18, // FOR DEFAULT ZOOM
                DragButton = MouseButtons.Left
            };

            radiusOverlay = new GMapOverlay("radius");
            markersOverlay = new GMapOverlay("pins");
            gmap.Overlays.Add(radiusOverlay);
            gmap.Overlays.Add(markersOverlay);

            gmap.MouseClick += Gmap_MouseClick;
            gmap.OnMarkerClick += Gmap_OnMarkerClick;

            panel1.Controls.Add(gmap);
            gmap.BringToFront();
        }

        private async void Gmap_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || gmap.IsMouseOverMarker) return;

            currentScanCenter = gmap.FromLocalToLatLng(e.X, e.Y);
            ClearQuestSelection();
            await LoadNearbyQuestsAsync(currentScanCenter.Lat, currentScanCenter.Lng, SearchRadiusMeters);
        }

        private void Gmap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || item.Tag is not QuestRecord q) return;

            selectedQuest = q;
            string itemText = string.IsNullOrWhiteSpace(q.AssignedTools) ? "None" : q.AssignedTools;

            lblQuestTitle.Text = $"[{q.Status.ToUpper()}] {q.Title}";
            lblQuestMeta.Text = $"Type: {q.QuestMode}\nCategory: {q.Category}  |  Difficulty: {q.Difficulty}";
            lblQuestRewards.Text = $"Base Stipend: {q.BasePay}\nPoints Reward: +{q.PointsReward} PTS\nItem / Tool: {itemText}";
            lblQuestSlots.Text = $"Available Slots: {q.MaxCapacity - q.CurrentJoinedCount} left ({q.CurrentJoinedCount}/{q.MaxCapacity} joined)";
            lblQuestSchedule.Text = $"Start: {q.StartDate:MMM dd, yyyy}\nDeadline: {q.EndDate:MMM dd, yyyy hh:mm tt}";

            bool hasSlots = q.CurrentJoinedCount < q.MaxCapacity;
            btnAcceptQuest.Enabled = hasSlots;
            btnAcceptQuest.Text = hasSlots ? "ACCEPT QUEST" : "QUEST FULL";
            btnAcceptQuest.BackColor = hasSlots ? Color.SeaGreen : Color.DimGray;
        }

        private async Task LoadNearbyQuestsAsync(double userLat, double userLng, double radiusMeters)
        {
            markersOverlay.Markers.Clear();
            DrawRadiusCircle(userLat, userLng, radiusMeters);

            markersOverlay.Markers.Add(new GMarkerGoogle(new PointLatLng(userLat, userLng), GMarkerGoogleType.blue_pushpin)
            {
                ToolTipText = "Your Current Location",
                ToolTipMode = MarkerTooltipMode.OnMouseOver
            });

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
                    WHERE q.Status IN ('Upcoming', 'Active')
                      AND ST_Distance_Sphere(POINT(@lng, @lat), POINT(q.Coordinates_X, q.Coordinates_Y)) <= @radius;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@lng", userLng);
                cmd.Parameters.AddWithValue("@lat", userLat);
                cmd.Parameters.AddWithValue("@radius", radiusMeters);

                using var reader = await cmd.ExecuteReaderAsync();
                int count = 0;
                while (await reader.ReadAsync())
                {
                    AddQuestPinToMap(ReadQuest(reader));
                    count++;
                }

                lblScanStatus.Text = $"Found {count} quest(s) within {radiusMeters / 1000.0:0.#} km.\nRight-click map to move your pin.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load nearby quests: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadAllActiveQuestsAsync()
        {
            markersOverlay.Markers.Clear();
            radiusOverlay.Polygons.Clear();
            ClearQuestSelection();

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
                    AddQuestPinToMap(ReadQuest(reader));
                    count++;
                }

                lblScanStatus.Text = $"Showing all {count} active/upcoming quest(s).\nRight-click map to filter by {SearchRadiusMeters / 1000.0:0.#} km.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load quests: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AcceptSelectedQuestAsync()
        {
            if (selectedQuest == null) return;

            if (UserSession.WorkerID <= 0)
            {
                MessageBox.Show("Please log in as a Citizen Worker first.", "Session Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAcceptQuest.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                try
                {
                    // Prevent duplicate joins on the same quest
                    string checkSql = @"
                        SELECT Status
                        FROM Quest_Memberships
                        WHERE QuestID = @questId AND WorkerID = @workerId
                        LIMIT 1;";

                    using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
                    {
                        checkCmd.Parameters.AddWithValue("@questId", selectedQuest.QuestID);
                        checkCmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);

                        object? existingStatus = await checkCmd.ExecuteScalarAsync();
                        if (existingStatus != null && existingStatus != DBNull.Value)
                        {
                            string statusStr = existingStatus.ToString() ?? "";
                            if (statusStr.Equals("Joined", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("You have already joined this quest.");
                            if (statusStr.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("You have already completed this quest.");
                        }
                    }

                    // Insert/re-activate membership in Quest_Memberships
                    string joinSql = @"
                        INSERT INTO Quest_Memberships (QuestID, WorkerID, JoinedAt, Status)
                        VALUES (@questId, @workerId, NOW(), 'Joined')
                        ON DUPLICATE KEY UPDATE Status = 'Joined', JoinedAt = NOW();";

                    using (var joinCmd = new MySqlCommand(joinSql, conn, tx))
                    {
                        joinCmd.Parameters.AddWithValue("@questId", selectedQuest.QuestID);
                        joinCmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);
                        await joinCmd.ExecuteNonQueryAsync();
                    }

                    // Increment joined count in Quests
                    string updateSql = @"
                        UPDATE Quests
                        SET CurrentJoinedCount = CurrentJoinedCount + 1
                        WHERE QuestID = @questId AND CurrentJoinedCount < MaxCapacity;";

                    using (var updateCmd = new MySqlCommand(updateSql, conn, tx))
                    {
                        updateCmd.Parameters.AddWithValue("@questId", selectedQuest.QuestID);
                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            throw new InvalidOperationException("This quest has just reached maximum capacity.");
                    }

                    await tx.CommitAsync();
                    MessageBox.Show($"You accepted '{selectedQuest.Title}'! Check your Quest List for details.", "Quest Accepted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearQuestSelection();
                    await LoadNearbyQuestsAsync(currentScanCenter.Lat, currentScanCenter.Lng, SearchRadiusMeters);
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not accept quest: {ex.Message}", "Quest Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnAcceptQuest.Enabled = true;
            }
        }

        private void ClearQuestSelection()
        {
            selectedQuest = null;
            lblQuestTitle.Text = "No Quest Selected";
            lblQuestMeta.Text = "Left-click any green pin on the map to inspect quest details.";
            lblQuestRewards.Text = "";
            lblQuestSlots.Text = "";
            lblQuestSchedule.Text = "";
            btnAcceptQuest.Enabled = false;
            btnAcceptQuest.Text = "ACCEPT QUEST";
            btnAcceptQuest.BackColor = Color.SeaGreen;
        }

        private static string FormatStipend(object? rawValue)
        {
            if (rawValue == null || rawValue == DBNull.Value)
                return "None";

            string text = rawValue.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return "None";

            if (decimal.TryParse(text, out decimal cashAmount))
                return $"PHP {cashAmount:N2}";

            return text;
        }

        private QuestRecord ReadQuest(MySqlDataReader r) => new()
        {
            QuestID = r.GetInt32("QuestID"),
            Title = r.GetString("Title"),
            Category = r.GetString("Category"),
            Difficulty = r.GetString("Difficulty"),
            BasePay = FormatStipend(r["BasePay"]),
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

        private void AddQuestPinToMap(QuestRecord q)
        {
            var marker = new GMarkerGoogle(new PointLatLng(q.Latitude, q.Longitude), GMarkerGoogleType.green_dot)
            {
                ToolTipText = $"{q.Title} [{q.QuestMode}] • {q.BasePay} (+{q.PointsReward} pts)",
                ToolTipMode = MarkerTooltipMode.OnMouseOver,
                Tag = q
            };
            markersOverlay.Markers.Add(marker);
        }

        private void DrawRadiusCircle(double centerLat, double centerLng, double radiusMeters)
        {
            radiusOverlay.Polygons.Clear();
            var points = new List<PointLatLng>();
            double earthRadius = 6378137.0;

            for (int i = 0; i < 72; i++)
            {
                double angle = i * 2 * Math.PI / 72;
                double dLat = (radiusMeters / earthRadius) * (180.0 / Math.PI);
                double dLng = dLat / Math.Cos(centerLat * Math.PI / 180.0);

                points.Add(new PointLatLng(
                    centerLat + dLat * Math.Sin(angle),
                    centerLng + dLng * Math.Cos(angle)
                ));
            }

            radiusOverlay.Polygons.Add(new GMapPolygon(points, "SearchRadius")
            {
                Fill = new SolidBrush(Color.FromArgb(40, Color.DodgerBlue)),
                Stroke = new Pen(Color.DodgerBlue, 2)
            });
        }
    }
}   