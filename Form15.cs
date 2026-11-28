using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form15 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private sealed class CouponCardBinding
        {
            public string ItemKey { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public int CouponId { get; set; }
            public int PointsCost { get; set; }
            public int StockQty { get; set; }
            public Label TitleLabel { get; set; } = null!;
            public Label? PointsLabel { get; set; }
            public Label? AvailabilityLabel { get; set; }
            public Control? CardBackground { get; set; }
            public Color OriginalTitleColor { get; set; }
        }

        private readonly List<CouponCardBinding> couponCards = new();
        private CouponCardBinding? selectedCoupon;

        private Control? btnEditCoupon;
        private Control? btnUpdate;

        // Default seed values matching the 10 Redemption Shop coupons on Form15
        private static readonly (string Key, string Name, int DefaultPoints, int DefaultStock)[] DefaultCoupons =
        {
            ("FAMILY HYGIENE KIT", "Family Hygiene Kit", 250, 20),
            ("LAUNDRY N CLEANING PACK", "Laundry & Cleaning Pack", 200, 25),
            ("5-GALLON PURIFIED WATER REFILL STUB", "5-Gallon Purified Water Refill Stub", 100, 50),
            ("5KG SACK OF RICE", "5kg Sack of Rice", 400, 30),
            ("1 TRAY OF EGGS", "1 Tray of Eggs", 300, 15),
            ("COOKING ESSENTIALS PACK", "Cooking Essentials Pack", 350, 18),
            ("INSTANT NOODLES AND SARDINES PACK", "Instant Noodles & Sardines Pack", 180, 40),
            ("P250 CASH ASSISTANCE", "₱250 Cash Assistance", 500, 10),
            ("P1000 CASH ASSISTANCE", "₱1000 Cash Assistance", 2000, 5),
            ("P500 CASH ASSISTANCE", "₱500 Cash Assistance", 1000, 8)
        };

        public Form15()
        {
            InitializeComponent();
            BindInventoryControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await InitializeAndLoadInventoryAsync();
        }

        private void BindInventoryControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();
            var allPictureBoxes = allControls.OfType<PictureBox>().ToList();

            foreach (var item in DefaultCoupons)
            {
                var titleLbl = allLabels.FirstOrDefault(l => NormalizeKey(l.Text) == item.Key);
                if (titleLbl == null) continue;

                Point titlePos = titleLbl.PointToScreen(Point.Empty);

                // Find the POINTS and AVAILABILITY labels beneath this coupon's title
                var pointsLbl = allLabels
                    .Where(l => l != titleLbl && l.Text.Trim().StartsWith("POINTS", StringComparison.OrdinalIgnoreCase))
                    .Select(l => new { Label = l, Pos = l.PointToScreen(Point.Empty) })
                    .Where(x => x.Pos.Y > titlePos.Y && x.Pos.Y - titlePos.Y < 110 && Math.Abs(x.Pos.X - titlePos.X) < 150)
                    .OrderBy(x => x.Pos.Y - titlePos.Y)
                    .Select(x => x.Label)
                    .FirstOrDefault();

                var availLbl = allLabels
                    .Where(l => l != titleLbl && l.Text.Trim().StartsWith("AVAILABILITY", StringComparison.OrdinalIgnoreCase))
                    .Select(l => new { Label = l, Pos = l.PointToScreen(Point.Empty) })
                    .Where(x => x.Pos.Y > titlePos.Y && x.Pos.Y - titlePos.Y < 110 && x.Pos.X >= titlePos.X - 60 && x.Pos.X - titlePos.X < 260)
                    .OrderBy(x => x.Pos.Y - titlePos.Y)
                    .Select(x => x.Label)
                    .FirstOrDefault();

                // Locate the underlying ticket PictureBox or Panel so clicking anywhere on the ticket selects it
                Control? bgControl = null;
                if (titleLbl.Parent is Panel pnl && pnl.Width < 380 && pnl.Height < 170)
                {
                    bgControl = pnl;
                }
                else
                {
                    Rectangle titleRect = new Rectangle(titlePos, titleLbl.Size);
                    bgControl = allPictureBoxes
                        .Where(pb => pb.Width >= 140 && pb.Width <= 380 && pb.Height >= 55 && pb.Height <= 160)
                        .FirstOrDefault(pb =>
                        {
                            Rectangle pbRect = new Rectangle(pb.PointToScreen(Point.Empty), pb.Size);
                            return pbRect.IntersectsWith(titleRect);
                        });
                }

                var binding = new CouponCardBinding
                {
                    ItemKey = item.Key,
                    DisplayName = item.Name,
                    PointsCost = item.DefaultPoints,
                    StockQty = item.DefaultStock,
                    TitleLabel = titleLbl,
                    PointsLabel = pointsLbl,
                    AvailabilityLabel = availLbl,
                    CardBackground = bgControl,
                    OriginalTitleColor = titleLbl.ForeColor
                };

                couponCards.Add(binding);

                AttachSelectionHandler(titleLbl, binding);
                if (pointsLbl != null) AttachSelectionHandler(pointsLbl, binding);
                if (availLbl != null) AttachSelectionHandler(availLbl, binding);
                if (bgControl != null) AttachSelectionHandler(bgControl, binding);
            }

            // Wire UPDATE control (already bound in Designer as 'update')
            btnUpdate = this.update;
            if (btnUpdate != null)
            {
                btnUpdate.Cursor = Cursors.Hand;
            }

            // Locate EDIT COUPON control (by Name, Text, or directly to the left of 'update')
            btnEditCoupon = allControls.FirstOrDefault(c =>
                c != btnUpdate &&
                (c.Name.Contains("edit", StringComparison.OrdinalIgnoreCase) ||
                 c.Text.Trim().Equals("EDIT COUPON", StringComparison.OrdinalIgnoreCase)));

            if (btnEditCoupon == null && btnUpdate != null)
            {
                Point updatePos = btnUpdate.PointToScreen(Point.Empty);
                btnEditCoupon = allControls
                    .Where(c => c != btnUpdate && (c is PictureBox || c is Button || c is Panel))
                    .Where(c => c.Width >= 110 && c.Width <= 320 && c.Height >= 28 && c.Height <= 90)
                    .Select(c => new { Ctrl = c, Pos = c.PointToScreen(Point.Empty) })
                    .Where(x => Math.Abs(x.Pos.Y - updatePos.Y) < 30 && x.Pos.X < updatePos.X)
                    .OrderByDescending(x => x.Pos.X)
                    .Select(x => x.Ctrl)
                    .FirstOrDefault();
            }

            if (btnEditCoupon != null)
            {
                btnEditCoupon.Cursor = Cursors.Hand;
                btnEditCoupon.Click += async (s, e) => await OpenEditCouponDialogAsync();
            }

            // Wire the left sidebar icons (Top = Admin Dashboard, 3rd = User Management)
            var sidebarIcons = allPictureBoxes
                .Where(p => p.Left < 100 && p.Top > 120 && p.Width < 90)
                .OrderBy(p => p.Top)
                .ToList();

            if (sidebarIcons.Count >= 1)
            {
                sidebarIcons[0].Cursor = Cursors.Hand;
                sidebarIcons[0].Click += (s, e) => NavigateTo(new X());
            }

            if (sidebarIcons.Count >= 3)
            {
                sidebarIcons[2].Cursor = Cursors.Hand;
                sidebarIcons[2].Click += (s, e) => NavigateTo(new Form14());
            }
        }

        private void AttachSelectionHandler(Control ctrl, CouponCardBinding binding)
        {
            ctrl.Cursor = Cursors.Hand;
            ctrl.Click += (s, e) => SelectCouponCard(binding);
            ctrl.DoubleClick += async (s, e) =>
            {
                SelectCouponCard(binding);
                await OpenEditCouponDialogAsync();
            };
        }

        private void SelectCouponCard(CouponCardBinding target)
        {
            selectedCoupon = target;

            foreach (var card in couponCards)
            {
                bool isSelected = card == target;
                card.TitleLabel.ForeColor = isSelected ? Color.White : card.OriginalTitleColor;
                card.TitleLabel.BackColor = isSelected ? Color.FromArgb(120, 45, 10) : Color.Transparent;
            }
        }

        private async Task InitializeAndLoadInventoryAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS Redemption_Inventory (
                        CouponID INT AUTO_INCREMENT PRIMARY KEY,
                        ItemKey VARCHAR(100) NOT NULL UNIQUE,
                        ItemName VARCHAR(120) NOT NULL,
                        PointsCost INT NOT NULL DEFAULT 100,
                        StockQty INT NOT NULL DEFAULT 10,
                        UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    );";

                using (var createCmd = new MySqlCommand(createTableSql, conn))
                {
                    await createCmd.ExecuteNonQueryAsync();
                }

                string seedSql = @"
                    INSERT IGNORE INTO Redemption_Inventory (ItemKey, ItemName, PointsCost, StockQty)
                    VALUES (@key, @name, @pts, @stock);";

                foreach (var item in DefaultCoupons)
                {
                    using var seedCmd = new MySqlCommand(seedSql, conn);
                    seedCmd.Parameters.AddWithValue("@key", item.Key);
                    seedCmd.Parameters.AddWithValue("@name", item.Name);
                    seedCmd.Parameters.AddWithValue("@pts", item.DefaultPoints);
                    seedCmd.Parameters.AddWithValue("@stock", item.DefaultStock);
                    await seedCmd.ExecuteNonQueryAsync();
                }

                await LoadInventoryFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize redemption inventory: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadInventoryFromDatabaseAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sql = "SELECT CouponID, ItemKey, ItemName, PointsCost, StockQty FROM Redemption_Inventory;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string key = reader["ItemKey"]?.ToString() ?? "";
                    var card = couponCards.FirstOrDefault(c => c.ItemKey.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (card == null) continue;

                    card.CouponId = Convert.ToInt32(reader["CouponID"]);
                    card.DisplayName = reader["ItemName"]?.ToString() ?? card.DisplayName;
                    card.PointsCost = Convert.ToInt32(reader["PointsCost"]);
                    card.StockQty = Convert.ToInt32(reader["StockQty"]);

                    RefreshCardLabels(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inventory items: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void RefreshCardLabels(CouponCardBinding card)
        {
            if (card.TitleLabel != null)
            {
                card.TitleLabel.Text = WrapTicketTitle(card.DisplayName.ToUpperInvariant());
            }

            if (card.PointsLabel != null)
            {
                card.PointsLabel.Text = $"{card.PointsCost} PTS";
            }

            if (card.AvailabilityLabel != null)
            {
                if (card.StockQty > 0)
                {
                    card.AvailabilityLabel.Text = $"STOCK: {card.StockQty}";
                    card.AvailabilityLabel.ForeColor = Color.DarkGreen;
                }
                else
                {
                    card.AvailabilityLabel.Text = "OUT OF STOCK";
                    card.AvailabilityLabel.ForeColor = Color.Firebrick;
                }
            }
        }

        private static string WrapTicketTitle(string title)
        {
            if (title.Length <= 22 || !title.Contains(' '))
                return title;

            int mid = title.Length / 2;
            int splitIndex = -1;
            int bestDist = int.MaxValue;

            for (int i = 0; i < title.Length; i++)
            {
                if (title[i] == ' ')
                {
                    int dist = Math.Abs(i - mid);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        splitIndex = i;
                    }
                }
            }

            return splitIndex > 0
                ? title.Substring(0, splitIndex) + Environment.NewLine + title.Substring(splitIndex + 1)
                : title;
        }

        private async Task OpenEditCouponDialogAsync()
        {
            if (selectedCoupon == null)
            {
                MessageBox.Show("Click on a coupon ticket first before clicking EDIT COUPON.", "No Coupon Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new Form
            {
                Text = $"Edit Coupon — {selectedCoupon.DisplayName}",
                Size = new Size(420, 270),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 25, 12),
                ForeColor = Color.White
            };

            var lblNameTitle = new Label { Text = "Reward Item:", Left = 20, Top = 24, Width = 130, ForeColor = Color.Gold };
            var txtItemName = new TextBox
            {
                Text = selectedCoupon.DisplayName,
                Left = 155,
                Top = 21,
                Width = 220,
                MaxLength = 100,
                BackColor = Color.FromArgb(30, 16, 8),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(this.Font, FontStyle.Bold)
            };

            var lblPts = new Label { Text = "Points Required:", Left = 20, Top = 68, Width = 130, ForeColor = Color.Gold };
            var numPts = new NumericUpDown
            {
                Left = 155,
                Top = 65,
                Width = 220,
                Minimum = 1,
                Maximum = 100000,
                Increment = 10,
                Value = Math.Max(1, selectedCoupon.PointsCost)
            };

            var lblStock = new Label { Text = "Available Stock:", Left = 20, Top = 112, Width = 130, ForeColor = Color.Gold };
            var numStock = new NumericUpDown
            {
                Left = 155,
                Top = 109,
                Width = 220,
                Minimum = 0,
                Maximum = 10000,
                Value = Math.Max(0, selectedCoupon.StockQty)
            };

            var btnSave = new Button
            {
                Text = "SAVE COUPON",
                Left = 155,
                Top = 165,
                Width = 130,
                Height = 36,
                BackColor = Color.ForestGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnCancel = new Button
            {
                Text = "CANCEL",
                Left = 295,
                Top = 165,
                Width = 80,
                Height = 36,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnCancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;
            btnSave.Click += async (s, e) =>
            {
                string updatedName = txtItemName.Text.Trim();
                if (string.IsNullOrWhiteSpace(updatedName))
                {
                    MessageBox.Show("Please enter a reward item name.", "Invalid Item Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtItemName.Focus();
                    return;
                }

                try
                {
                    using var conn = new MySqlConnection(connString);
                    await conn.OpenAsync();

                    string sql = @"
                        UPDATE Redemption_Inventory
                        SET ItemName = @name, PointsCost = @pts, StockQty = @stock
                        WHERE ItemKey = @key;";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", updatedName);
                    cmd.Parameters.AddWithValue("@pts", (int)numPts.Value);
                    cmd.Parameters.AddWithValue("@stock", (int)numStock.Value);
                    cmd.Parameters.AddWithValue("@key", selectedCoupon.ItemKey);
                    await cmd.ExecuteNonQueryAsync();

                    selectedCoupon.DisplayName = updatedName;
                    selectedCoupon.PointsCost = (int)numPts.Value;
                    selectedCoupon.StockQty = (int)numStock.Value;
                    RefreshCardLabels(selectedCoupon);

                    dlg.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating coupon: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblNameTitle, txtItemName, lblPts, numPts, lblStock, numStock, btnSave, btnCancel });
            dlg.ShowDialog(this);
        }

        private void NavigateTo(Form targetForm)
        {
            targetForm.FormClosed += (s, e) => this.Close();
            targetForm.Show();
            this.Hide();
        }

        private static string NormalizeKey(string text)
        {
            return string.Join(" ", text
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("₱", "P")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
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

        private async void update_Click(object sender, EventArgs e)
        {
            if (update != null) update.Enabled = false;
            try
            {
                await LoadInventoryFromDatabaseAsync();
                MessageBox.Show("Redemption shop inventory has been updated and synced.", "Inventory Synced", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                if (update != null) update.Enabled = true;
            }
        }

        private void toolinventory_Click(object sender, EventArgs e)
        {
            Form27 nextForm = new Form27();
            nextForm.FormClosed += (s, args) => Application.Exit();
            nextForm.Show();
            this.Hide();
        }
    }
}