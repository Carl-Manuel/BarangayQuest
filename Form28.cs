using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form28 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private sealed class ShopCatalogItem
        {
            public int ItemID { get; set; }
            public string Category { get; set; } = "Food Staples";
            public string ItemName { get; set; } = "";
            public string Description { get; set; } = "";
            public int CostPoints { get; set; }
            public int Stock { get; set; }
        }

        private sealed class LeftCardSlot
        {
            public int SlotIndex { get; set; }
            public Control? CardContainer { get; set; }
            public PictureBox? IconBox { get; set; }
            public Label? TitleLabel { get; set; }
            public Label? PointsLabel { get; set; }
            public Label? AvailabilityLabel { get; set; }
            public ShopCatalogItem? BoundItem { get; set; }
        }

        private static readonly List<ShopCatalogItem> DefaultCatalog = new()
        {
            // FOOD STAPLES
            new ShopCatalogItem { Category = "Food Staples", ItemName = "5KG SACK OF RICE", Description = "Well-milled local rice (5 kilograms)", CostPoints = 2000, Stock = 15 },
            new ShopCatalogItem { Category = "Food Staples", ItemName = "1 TRAY OF EGGS", Description = "30 pieces fresh farm chicken eggs", CostPoints = 2800, Stock = 12 },
            new ShopCatalogItem { Category = "Food Staples", ItemName = "COOKING ESSENTIALS PACK", Description = "Cooking oil (1L), soy sauce, vinegar & salt", CostPoints = 2500, Stock = 10 },
            new ShopCatalogItem { Category = "Food Staples", ItemName = "INSTANT NOODLES & SARDINES PACK", Description = "6 packs instant noodles + 4 cans sardines", CostPoints = 1500, Stock = 20 },

            // CASH STIPEND
            new ShopCatalogItem { Category = "Cash Stipend", ItemName = "PHP 100 CASH VOUCHER", Description = "Claimable at Barangay Treasury Window 1", CostPoints = 1000, Stock = 25 },
            new ShopCatalogItem { Category = "Cash Stipend", ItemName = "PHP 250 CASH VOUCHER", Description = "Claimable at Barangay Treasury Window 1", CostPoints = 2400, Stock = 15 },
            new ShopCatalogItem { Category = "Cash Stipend", ItemName = "PHP 500 CASH VOUCHER", Description = "Claimable at Barangay Treasury Window 1", CostPoints = 4800, Stock = 10 },
            new ShopCatalogItem { Category = "Cash Stipend", ItemName = "PHP 1,000 EMERGENCY STIPEND", Description = "Priority barangay assistance cash voucher", CostPoints = 9500, Stock = 5 },

            // HOUSEHOLD ESSENTIALS
            new ShopCatalogItem { Category = "Household Essentials", ItemName = "LAUNDRY & BATH HYGIENE KIT", Description = "Detergent powder, bar soap, shampoo & toothpaste", CostPoints = 1400, Stock = 18 },
            new ShopCatalogItem { Category = "Household Essentials", ItemName = "DRINKING WATER GALLON REFILL (5X)", Description = "5 purified 5-gallon water station vouchers", CostPoints = 900, Stock = 30 },
            new ShopCatalogItem { Category = "Household Essentials", ItemName = "FIRST AID & MEDICINE KIT", Description = "Alcohol, bandages, gauze, paracetamol & betadine", CostPoints = 1800, Stock = 12 },
            new ShopCatalogItem { Category = "Household Essentials", ItemName = "SOLAR EMERGENCY FLASHLIGHT", Description = "Rechargeable LED lantern for power outages", CostPoints = 3200, Stock = 8 }
        };

        private readonly List<ShopCatalogItem> _allItems = new();
        private readonly List<LeftCardSlot> _leftSlots = new();
        private readonly Dictionary<string, Image?> _categorySampleIcons = new(StringComparer.OrdinalIgnoreCase);

        private string _activeCategory = "Food Staples";
        private ShopCatalogItem? _selectedItem;
        private int _currentUserId;
        private int _currentUserPoints;
        private bool _isRedeeming = false;

        // Top Points & Bottom Bar
        private Label? _lblRewardPointsValue;
        private Label? _lblBottomStatus;

        // Category Tabs
        private Control? _tabFoodStaples;
        private Control? _tabCashStipend;
        private Control? _tabHouseholdEssentials;

        // Right-hand Enlarged Coupon Preview Controls
        private Panel? _pnlRightHost;
        private Panel? _pnlBigCoupon;
        private PictureBox? _pbBigIcon;
        private Label? _lblBigTitle;
        private Label? _lblBigDesc;
        private Label? _lblBigPoints;
        private Button? _btnRedeemCoupon;

        public Form28()
        {
            InitializeComponent();
            BindShopControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await InitializeShopAsync();
        }

        private void BindShopControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();

            // Bind top-right "REWARD POINTS" value label
            _lblRewardPointsValue = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("REWARD POINTS", StringComparison.OrdinalIgnoreCase) &&
                l.Font.Size >= 11f)
                ?? allLabels.Where(l => l.Text.Trim().Equals("REWARD POINTS", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(l => l.Top)
                            .FirstOrDefault();

            // Bind the 4 Left-Hand Coupon Cards sorted top-to-bottom
            var availLabels = allLabels
                .Where(l => l.Text.Trim().Equals("AVAILABILITY", StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.PointToScreen(Point.Empty).Y)
                .ToList();

            for (int i = 0; i < availLabels.Count && i < 4; i++)
            {
                var availLbl = availLabels[i];
                Control? cardContainer = availLbl.Parent != this ? availLbl.Parent : null;

                var cardLabels = cardContainer != null
                    ? GetAllControls(cardContainer).OfType<Label>().ToList()
                    : allLabels.Where(l => Math.Abs(l.PointToScreen(Point.Empty).Y - availLbl.PointToScreen(Point.Empty).Y) <= 55 &&
                                           Math.Abs(l.PointToScreen(Point.Empty).X - availLbl.PointToScreen(Point.Empty).X) <= 180).ToList();

                var pointsLbl = cardLabels.FirstOrDefault(l =>
                    l != availLbl && l.Text.Trim().Equals("POINTS", StringComparison.OrdinalIgnoreCase));

                var titleLbl = cardLabels
                    .Where(l => l != availLbl && l != pointsLbl)
                    .OrderBy(l => l.Top)
                    .FirstOrDefault();

                var iconPb = cardContainer != null
                    ? GetAllControls(cardContainer).OfType<PictureBox>().FirstOrDefault()
                    : null;

                var slot = new LeftCardSlot
                {
                    SlotIndex = i,
                    CardContainer = cardContainer,
                    IconBox = iconPb,
                    TitleLabel = titleLbl,
                    PointsLabel = pointsLbl,
                    AvailabilityLabel = availLbl
                };

                _leftSlots.Add(slot);

                if (iconPb?.Image != null && i < 4)
                {
                    _categorySampleIcons[$"Food Staples_{i}"] = iconPb.Image;
                }

                if (cardContainer != null)
                {
                    WireCardSelection(cardContainer, slot);
                }
                else
                {
                    if (titleLbl != null) WireCardSelection(titleLbl, slot);
                    if (pointsLbl != null) WireCardSelection(pointsLbl, slot);
                    if (availLbl != null) WireCardSelection(availLbl, slot);
                }
            }

            // Bind the 3 Category Buttons
            _tabFoodStaples = FindControlByText(allControls, "FOOD STAPLES");
            _tabCashStipend = FindControlByText(allControls, "CASH STIPEND");
            _tabHouseholdEssentials = FindControlByText(allControls, "HOUSEHOLD ESSENTIALS");

            if (_tabFoodStaples != null)
            {
                _tabFoodStaples.Cursor = Cursors.Hand;
                _tabFoodStaples.Click += (s, e) => SwitchCategory("Food Staples");
            }
            if (_tabCashStipend != null)
            {
                _tabCashStipend.Cursor = Cursors.Hand;
                _tabCashStipend.Click += (s, e) => SwitchCategory("Cash Stipend");
            }
            if (_tabHouseholdEssentials != null)
            {
                _tabHouseholdEssentials.Cursor = Cursors.Hand;
                _tabHouseholdEssentials.Click += (s, e) => SwitchCategory("Household Essentials");
            }

            // Build the Right-Hand Enlarged Coupon Preview & "REDEEM CODE FOR COUPON" Button
            BuildRightPreviewPanel(allControls);

            //  Wire Left Scrollbar Navigation Icons
            WireSidebarIcons(allControls);

            // Bottom Status Banner
            var bottomMaroonBar = allControls
                .OfType<Panel>()
                .Where(p => p.Width > this.ClientSize.Width * 0.6 && p.Height >= 25 && p.Height <= 70 && p.Top > this.ClientSize.Height * 0.75)
                .OrderByDescending(p => p.Top)
                .FirstOrDefault();

            _lblBottomStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "CLICK ANY COUPON ON THE LEFT TO PREVIEW AND GENERATE YOUR REDEMPTION CODE"
            };

            if (bottomMaroonBar != null)
            {
                bottomMaroonBar.Controls.Add(_lblBottomStatus);
            }
        }

        private void BuildRightPreviewPanel(List<Control> allControls)
        {
            _pnlRightHost = allControls
                .OfType<Panel>()
                .Where(p => p.Left > this.ClientSize.Width * 0.30 &&
                            p.Width > this.ClientSize.Width * 0.40 &&
                            p.Height > this.ClientSize.Height * 0.40)
                .OrderByDescending(p => p.Width * p.Height)
                .FirstOrDefault();

            if (_pnlRightHost == null) return;

            int ticketW = Math.Min(470, _pnlRightHost.Width - 48);
            int ticketH = 185;
            int ticketX = (_pnlRightHost.Width - ticketW) / 2;
            int ticketY = Math.Max(45, (_pnlRightHost.Height / 2) - 115);

            _pnlBigCoupon = new Panel
            {
                Left = ticketX,
                Top = ticketY,
                Width = ticketW,
                Height = ticketH,
                BackColor = Color.FromArgb(245, 166, 35),
                Visible = false
            };

            int stubWidth = 128;
            _pbBigIcon = new PictureBox
            {
                Left = 18,
                Top = 28,
                Width = stubWidth - 36,
                Height = ticketH - 56,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            var dashedDivider = new Panel
            {
                Left = stubWidth,
                Top = 10,
                Width = 2,
                Height = ticketH - 20,
                BackColor = Color.Transparent
            };
            dashedDivider.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(45, 25, 10), 2.5f)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                };
                e.Graphics.DrawLine(pen, 1, 0, 1, dashedDivider.Height);
            };

            _pnlBigCoupon.Paint += (s, e) =>
            {
                using var notchBrush = new SolidBrush(_pnlRightHost.BackColor);
                int r = 12;
                e.Graphics.FillEllipse(notchBrush, -r, (ticketH / 2) - r, r * 2, r * 2);
                e.Graphics.FillEllipse(notchBrush, ticketW - r, (ticketH / 2) - r, r * 2, r * 2);
            };

            int contentX = stubWidth + 14;
            int contentW = ticketW - contentX - 18;

            _lblBigTitle = new Label
            {
                Left = contentX,
                Top = 22,
                Width = contentW,
                Height = 46,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Georgia", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 18, 8),
                BackColor = Color.Transparent,
                Text = "5KG SACK OF RICE"
            };

            _lblBigDesc = new Label
            {
                Left = contentX,
                Top = 72,
                Width = contentW,
                Height = 44,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Georgia", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(55, 34, 14),
                BackColor = Color.Transparent,
                Text = "(desc)"
            };

            _lblBigPoints = new Label
            {
                Left = contentX,
                Top = 122,
                Width = contentW,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 18, 8),
                BackColor = Color.Transparent,
                Text = "2000 PTS"
            };

            _pnlBigCoupon.Controls.Add(_pbBigIcon);
            _pnlBigCoupon.Controls.Add(dashedDivider);
            _pnlBigCoupon.Controls.Add(_lblBigTitle);
            _pnlBigCoupon.Controls.Add(_lblBigDesc);
            _pnlBigCoupon.Controls.Add(_lblBigPoints);

            int btnW = Math.Min(310, ticketW - 40);
            _btnRedeemCoupon = new Button
            {
                Left = (_pnlRightHost.Width - btnW) / 2,
                Top = _pnlBigCoupon.Bottom + 24,
                Width = btnW,
                Height = 42,
                Text = "REDEEM CODE FOR COUPON",
                BackColor = Color.FromArgb(62, 168, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnRedeemCoupon.FlatAppearance.BorderColor = Color.FromArgb(140, 230, 80);
            _btnRedeemCoupon.FlatAppearance.BorderSize = 1;
            _btnRedeemCoupon.Click += async (s, e) => await RedeemSelectedCouponAsync();

            _pnlRightHost.Controls.Add(_pnlBigCoupon);
            _pnlRightHost.Controls.Add(_btnRedeemCoupon);
        }

        private void WireCardSelection(Control root, LeftCardSlot slot)
        {
            root.Cursor = Cursors.Hand;
            root.Click += (s, e) => SelectSlot(slot);

            if (root != this)
            {
                foreach (Control child in GetAllControls(root))
                {
                    child.Cursor = Cursors.Hand;
                    child.Click += (s, e) => SelectSlot(slot);
                }
            }
        }

        private async Task InitializeShopAsync()
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureShopSchemaAndSeedAsync(conn);
                await LoadCurrentCitizenPointsAsync(conn);
                await LoadShopItemsFromDbAsync(conn);

                SwitchCategory("Food Staples");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Redemption Shop:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task EnsureShopSchemaAndSeedAsync(MySqlConnection conn)
        {
            string[] alterColumns =
            {
                "ALTER TABLE Shop_Items ADD COLUMN Category VARCHAR(50) NOT NULL DEFAULT 'Food Staples';",
                "ALTER TABLE Shop_Items ADD COLUMN Description VARCHAR(255) NOT NULL DEFAULT '';"
            };

            foreach (string alterSql in alterColumns)
            {
                try
                {
                    using var cmd = new MySqlCommand(alterSql, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Column already exists
                }
            }

            string createRedemptionsSql = @"
                CREATE TABLE IF NOT EXISTS Shop_Redemptions (
                    RedemptionID INT AUTO_INCREMENT PRIMARY KEY,
                    UserID INT NOT NULL,
                    ItemID INT NOT NULL,
                    ItemName VARCHAR(100) NOT NULL,
                    VoucherCode VARCHAR(25) NOT NULL UNIQUE,
                    PointsSpent INT NOT NULL,
                    Status ENUM('Active', 'Claimed') NOT NULL DEFAULT 'Active',
                    RedeemedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using (var createCmd = new MySqlCommand(createRedemptionsSql, conn))
            {
                await createCmd.ExecuteNonQueryAsync();
            }

            using var countCmd = new MySqlCommand("SELECT COUNT(*) FROM Shop_Items;", conn);
            long existingCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
            if (existingCount == 0)
            {
                foreach (var item in DefaultCatalog)
                {
                    using var insCmd = new MySqlCommand(
                        "INSERT INTO Shop_Items (ItemName, Category, Description, CostPoints, Stock) VALUES (@name, @cat, @desc, @pts, @stock);", conn);
                    insCmd.Parameters.AddWithValue("@name", item.ItemName);
                    insCmd.Parameters.AddWithValue("@cat", item.Category);
                    insCmd.Parameters.AddWithValue("@desc", item.Description);
                    insCmd.Parameters.AddWithValue("@pts", item.CostPoints);
                    insCmd.Parameters.AddWithValue("@stock", item.Stock);
                    await insCmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task LoadCurrentCitizenPointsAsync(MySqlConnection conn)
        {
            _currentUserId = UserSession.UserID;

            if (_currentUserId > 0)
            {
                using var cmd = new MySqlCommand("SELECT Points FROM Users WHERE UserID = @uid LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("@uid", _currentUserId);
                object? res = await cmd.ExecuteScalarAsync();
                _currentUserPoints = res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
            }
            else if (!string.IsNullOrWhiteSpace(UserSession.FullName))
            {
                using var cmd = new MySqlCommand("SELECT UserID, Points FROM Users WHERE FullName = @name LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("@name", UserSession.FullName.Trim());
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    _currentUserId = Convert.ToInt32(r["UserID"]);
                    _currentUserPoints = Convert.ToInt32(r["Points"]);
                }
            }
            else
            {
                using var cmd = new MySqlCommand("SELECT UserID, Points FROM Users WHERE Role = 'CitizenWorker' ORDER BY UserID ASC LIMIT 1;", conn);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    _currentUserId = Convert.ToInt32(r["UserID"]);
                    _currentUserPoints = Convert.ToInt32(r["Points"]);
                }
            }

            UpdatePointsDisplay();
        }

        private void UpdatePointsDisplay()
        {
            if (_lblRewardPointsValue != null)
            {
                _lblRewardPointsValue.Text = $"{_currentUserPoints} PTS";
            }
        }

        private async Task LoadShopItemsFromDbAsync(MySqlConnection conn)
        {
            _allItems.Clear();

            string sql = @"
                SELECT ItemID, ItemName, COALESCE(Category, 'Food Staples') AS Category,
                       COALESCE(Description, '') AS Description, CostPoints, Stock
                FROM Shop_Items
                ORDER BY ItemID ASC;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                _allItems.Add(new ShopCatalogItem
                {
                    ItemID = Convert.ToInt32(reader["ItemID"]),
                    ItemName = reader["ItemName"]?.ToString() ?? "",
                    Category = reader["Category"]?.ToString() ?? "Food Staples",
                    Description = reader["Description"]?.ToString() ?? "",
                    CostPoints = Convert.ToInt32(reader["CostPoints"]),
                    Stock = Convert.ToInt32(reader["Stock"])
                });
            }
        }

        private void SwitchCategory(string categoryName)
        {
            _activeCategory = categoryName;

            var categoryItems = _allItems
                .Where(i => i.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .ToList();

            if (categoryItems.Count == 0 && categoryName.Equals("Food Staples", StringComparison.OrdinalIgnoreCase))
            {
                categoryItems = _allItems.Take(4).ToList();
            }

            for (int i = 0; i < _leftSlots.Count; i++)
            {
                var slot = _leftSlots[i];
                if (i < categoryItems.Count)
                {
                    var item = categoryItems[i];
                    slot.BoundItem = item;
                    if (slot.CardContainer != null) slot.CardContainer.Visible = true;
                    if (slot.TitleLabel != null) slot.TitleLabel.Text = item.ItemName;
                    if (slot.PointsLabel != null) slot.PointsLabel.Text = $"{item.CostPoints} PTS";

                    if (slot.IconBox != null && _categorySampleIcons.TryGetValue($"Food Staples_{i}", out var img))
                    {
                        slot.IconBox.Image = img;
                    }

                    if (slot.AvailabilityLabel != null)
                    {
                        bool inStock = item.Stock > 0;
                        slot.AvailabilityLabel.Text = inStock ? "AVAILABLE" : "UNAVAILABLE";
                        slot.AvailabilityLabel.ForeColor = inStock ? Color.DarkGreen : Color.DarkRed;
                    }
                }
                else
                {
                    slot.BoundItem = null;
                    if (slot.CardContainer != null) slot.CardContainer.Visible = false;
                }
            }

            if (_lblBottomStatus != null)
            {
                _lblBottomStatus.Text = $"CATEGORY: {categoryName.ToUpperInvariant()} • CLICK ANY COUPON CARD TO PREVIEW";
            }
        }

        private void SelectSlot(LeftCardSlot slot)
        {
            if (slot.BoundItem == null) return;
            _selectedItem = slot.BoundItem;

            if (_pnlBigCoupon == null || _btnRedeemCoupon == null) return;

            _pnlBigCoupon.Visible = true;
            _btnRedeemCoupon.Visible = true;

            if (_pbBigIcon != null)
            {
                _pbBigIcon.Image = slot.IconBox?.Image;
            }

            if (_lblBigTitle != null)
            {
                _lblBigTitle.Text = _selectedItem.ItemName;
            }

            if (_lblBigDesc != null)
            {
                _lblBigDesc.Text = string.IsNullOrWhiteSpace(_selectedItem.Description)
                    ? $"({_selectedItem.Category} • Stock: {_selectedItem.Stock})"
                    : $"({_selectedItem.Description})";
            }

            if (_lblBigPoints != null)
            {
                _lblBigPoints.Text = $"{_selectedItem.CostPoints} PTS";
            }

            bool canRedeem = _selectedItem.Stock > 0 && _currentUserPoints >= _selectedItem.CostPoints;
            _btnRedeemCoupon.Enabled = _selectedItem.Stock > 0;
            _btnRedeemCoupon.BackColor = _selectedItem.Stock <= 0
                ? Color.DimGray
                : (canRedeem ? Color.FromArgb(62, 168, 22) : Color.DarkGoldenrod);
            _btnRedeemCoupon.Text = _selectedItem.Stock <= 0
                ? "ITEM CURRENTLY UNAVAILABLE"
                : "REDEEM CODE FOR COUPON";

            if (_lblBottomStatus != null)
            {
                _lblBottomStatus.Text = _selectedItem.Stock <= 0
                    ? $"{_selectedItem.ItemName} IS OUT OF STOCK (UNAVAILABLE)"
                    : $"SELECTED: {_selectedItem.ItemName} ({_selectedItem.CostPoints} PTS) • AVAILABLE STOCK: {_selectedItem.Stock}";
            }
        }

        private async Task RedeemSelectedCouponAsync()
        {
            if (_isRedeeming || _selectedItem == null) return;

            if (_selectedItem.Stock <= 0)
            {
                MessageBox.Show("This item is currently unavailable (out of stock).", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentUserPoints < _selectedItem.CostPoints)
            {
                int needed = _selectedItem.CostPoints - _currentUserPoints;
                MessageBox.Show(
                    $"Insufficient Reward Points.\n\nYour Balance: {_currentUserPoints} PTS\nItem Cost: {_selectedItem.CostPoints} PTS\nNeeded: {needed} more PTS",
                    "Not Enough Points",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Redeem '{_selectedItem.ItemName}' for {_selectedItem.CostPoints} PTS?\n\n" +
                $"Remaining balance after redemption: {_currentUserPoints - _selectedItem.CostPoints} PTS",
                "Confirm Coupon Redemption",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _isRedeeming = true;
            if (_btnRedeemCoupon != null) _btnRedeemCoupon.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                int freshPoints;
                using (var ptsCmd = new MySqlCommand("SELECT Points FROM Users WHERE UserID = @uid FOR UPDATE;", conn, tx))
                {
                    ptsCmd.Parameters.AddWithValue("@uid", _currentUserId);
                    freshPoints = Convert.ToInt32(await ptsCmd.ExecuteScalarAsync());
                }

                int freshStock;
                using (var stkCmd = new MySqlCommand("SELECT Stock FROM Shop_Items WHERE ItemID = @iid FOR UPDATE;", conn, tx))
                {
                    stkCmd.Parameters.AddWithValue("@iid", _selectedItem.ItemID);
                    freshStock = Convert.ToInt32(await stkCmd.ExecuteScalarAsync());
                }

                if (freshStock <= 0 || freshPoints < _selectedItem.CostPoints)
                {
                    await tx.RollbackAsync();
                    MessageBox.Show("Points or item stock changed. Please refresh and try again.", "Redemption Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string voucherCode = GenerateUniqueVoucherCode(_selectedItem.ItemName);

                using (var deductCmd = new MySqlCommand(
                    "UPDATE Users SET Points = Points - @cost WHERE UserID = @uid;", conn, tx))
                {
                    deductCmd.Parameters.AddWithValue("@cost", _selectedItem.CostPoints);
                    deductCmd.Parameters.AddWithValue("@uid", _currentUserId);
                    await deductCmd.ExecuteNonQueryAsync();
                }

                using (var stockUpdCmd = new MySqlCommand(
                    "UPDATE Shop_Items SET Stock = Stock - 1 WHERE ItemID = @iid;", conn, tx))
                {
                    stockUpdCmd.Parameters.AddWithValue("@iid", _selectedItem.ItemID);
                    await stockUpdCmd.ExecuteNonQueryAsync();
                }

                using (var codeInsCmd = new MySqlCommand(
                    @"INSERT INTO Shop_Redemptions (UserID, ItemID, ItemName, VoucherCode, PointsSpent, Status)
                      VALUES (@uid, @iid, @iname, @code, @spent, 'Active');", conn, tx))
                {
                    codeInsCmd.Parameters.AddWithValue("@uid", _currentUserId);
                    codeInsCmd.Parameters.AddWithValue("@iid", _selectedItem.ItemID);
                    codeInsCmd.Parameters.AddWithValue("@iname", _selectedItem.ItemName);
                    codeInsCmd.Parameters.AddWithValue("@code", voucherCode);
                    codeInsCmd.Parameters.AddWithValue("@spent", _selectedItem.CostPoints);
                    await codeInsCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                _currentUserPoints = freshPoints - _selectedItem.CostPoints;
                _selectedItem.Stock = freshStock - 1;
                UpdatePointsDisplay();
                SwitchCategory(_activeCategory);

                if (_lblBottomStatus != null)
                {
                    _lblBottomStatus.Text = $"REDEEMED! VOUCHER CODE: {voucherCode} • REMAINING BALANCE: {_currentUserPoints} PTS";
                }

                ShowVoucherPopup(_selectedItem.ItemName, voucherCode, _selectedItem.CostPoints, _currentUserPoints);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to redeem coupon:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRedeeming = false;
                if (_btnRedeemCoupon != null && _selectedItem != null)
                {
                    _btnRedeemCoupon.Enabled = _selectedItem.Stock > 0;
                }
            }
        }

        private void ShowVoucherPopup(string itemName, string voucherCode, int pointsSpent, int remainingPoints)
        {
            using var popup = new Form
            {
                Text = "Coupon Voucher Generated",
                Size = new Size(440, 315),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(46, 26, 12)
            };

            var outerBorder = new Panel
            {
                Left = 14,
                Top = 14,
                Width = popup.ClientSize.Width - 28,
                Height = popup.ClientSize.Height - 28,
                BackColor = Color.FromArgb(64, 38, 18)
            };
            outerBorder.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.Goldenrod, 2f);
                e.Graphics.DrawRectangle(pen, 0, 0, outerBorder.Width - 1, outerBorder.Height - 1);
            };

            var lblHeader = new Label
            {
                Left = 10,
                Top = 14,
                Width = outerBorder.Width - 20,
                Height = 28,
                Text = "COUPON VOUCHER GENERATED",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                ForeColor = Color.Gold,
                BackColor = Color.Transparent
            };

            var lblItem = new Label
            {
                Left = 10,
                Top = 46,
                Width = outerBorder.Width - 20,
                Height = 24,
                Text = $"{itemName} (-{pointsSpent} PTS)",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.Bisque,
                BackColor = Color.Transparent
            };

            // Gold Coupon Ticket Box holding the generated code
            var ticketBox = new Panel
            {
                Left = 24,
                Top = 80,
                Width = outerBorder.Width - 48,
                Height = 76,
                BackColor = Color.FromArgb(245, 166, 35)
            };

            var lblCodeTitle = new Label
            {
                Left = 8,
                Top = 8,
                Width = ticketBox.Width - 16,
                Height = 18,
                Text = "PRESENT THIS CODE AT THE BARANGAY COUNTER",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(65, 35, 10),
                BackColor = Color.Transparent
            };

            var lblCodeValue = new Label
            {
                Left = 8,
                Top = 28,
                Width = ticketBox.Width - 16,
                Height = 38,
                Text = voucherCode,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Consolas", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 15, 5),
                BackColor = Color.Transparent
            };

            ticketBox.Controls.Add(lblCodeTitle);
            ticketBox.Controls.Add(lblCodeValue);

            var lblBalance = new Label
            {
                Left = 10,
                Top = 166,
                Width = outerBorder.Width - 20,
                Height = 22,
                Text = $"Remaining Balance: {remainingPoints} PTS",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.LightGreen,
                BackColor = Color.Transparent
            };

            var btnCopy = new Button
            {
                Left = 36,
                Top = 200,
                Width = 155,
                Height = 38,
                Text = "COPY CODE",
                BackColor = Color.FromArgb(215, 145, 25),
                ForeColor = Color.FromArgb(28, 15, 5),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderColor = Color.Gold;
            btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(voucherCode);
                btnCopy.Text = "COPIED!";
            };

            var btnClose = new Button
            {
                Left = outerBorder.Width - 191,
                Top = 200,
                Width = 155,
                Height = 38,
                Text = "DONE",
                BackColor = Color.FromArgb(62, 168, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.LightGreen;
            btnClose.Click += (s, e) => popup.Close();

            outerBorder.Controls.Add(lblHeader);
            outerBorder.Controls.Add(lblItem);
            outerBorder.Controls.Add(ticketBox);
            outerBorder.Controls.Add(lblBalance);
            outerBorder.Controls.Add(btnCopy);
            outerBorder.Controls.Add(btnClose);

            popup.Controls.Add(outerBorder);
            popup.ShowDialog(this);
        }

        private static string GenerateUniqueVoucherCode(string itemName)
        {
            string prefix = new string(itemName.Where(char.IsLetter).Take(4).ToArray()).ToUpperInvariant();
            if (prefix.Length < 2) prefix = "ITEM";

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<byte> randomBytes = stackalloc byte[6];
            RandomNumberGenerator.Fill(randomBytes);

            char[] suffix = new char[6];
            for (int i = 0; i < 6; i++)
            {
                suffix[i] = chars[randomBytes[i] % chars.Length];
            }

            return $"BQ-{prefix}-{new string(suffix)}";
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
                leftIcons[0].Cursor = Cursors.Hand;
                leftIcons[0].Click += (s, e) => NavigateTo(new Form5());

                leftIcons[1].Cursor = Cursors.Hand;
                leftIcons[1].Click += (s, e) => NavigateTo(new Form6());

                leftIcons[2].Cursor = Cursors.Hand;
                leftIcons[2].Click += (s, e) => NavigateTo(new Form23());
            }
        }

        private static Control? FindControlByText(List<Control> allControls, string text)
        {
            return allControls.FirstOrDefault(c =>
                !(c is Form) &&
                c.Text.Trim().Equals(text, StringComparison.OrdinalIgnoreCase));
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

        private void questmap3_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form23());
        }

        private void citizenquest3_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form5());
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form5());
        }

        private void foodstaples_Click(object sender, EventArgs e)
        {
            SwitchCategory("Food Staples");
        }
    }
}